using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SmartNPC : MonoBehaviour
{
    [Header("NPC Settings")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float playerDetectionRadius = 5f;
    [SerializeField] private float obstacleAvoidanceRadius = 1.5f;
    [SerializeField] private float waitTime = 2f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Behavior Settings")]
    [SerializeField] private bool runAwayFromPlayer = true;
    [SerializeField] private float runAwayDistance = 8f;

    [Header("Light Reaction")]
    [SerializeField] private float stopMovingInDarknessThreshold = 0.3f;
    [SerializeField] private float panicSpeedMultiplier = 2f;

    [Header("Corner Avoidance Settings")]
    [SerializeField] private float cornerDetectionTime = 0.5f; // How quickly to detect stuck state
    [SerializeField] private float cornerEscapeForce = 5f; // Force applied to escape corners
    [SerializeField] private int maxEscapeAttempts = 5; // Maximum number of attempts to escape
    [SerializeField] private float safetyTeleportDistance = 2f; // Distance to teleport if all else fails

    [Header("Scene Transition")]
    [SerializeField] private string dialogueSceneName = "Dialogue";
    [SerializeField] private float sceneTransitionDelay = 0.5f;

    private Rigidbody2D rb;
    private Vector2 targetPosition;
    private Vector2 moveDirection;
    private bool isWandering = true;
    private bool isWaiting = false;
    private bool isRescued = false;
    private bool isPlayerNearby = false;
    private Transform playerTransform;
    private Animator animator;
    private UnityEngine.Rendering.Universal.Light2D playerLight;
    private bool isPanicking = false;
    private bool isFrozenInDark = false;

    // Corner avoidance variables
    private Vector2 lastPosition;
    private float stuckTimer = 0f;
    private bool isEscapingCorner = false;
    private int currentEscapeAttempt = 0;
    private bool debugMode = true; // Set to true to see debug messages

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Recommended Rigidbody2D settings
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0f;
        rb.linearDamping = 1f; // Add some drag to prevent excessive sliding

        animator = GetComponent<Animator>(); // Optional

        // Find player if not assigned
        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("LightSource")?.transform;

        // Find player light
        if (playerTransform != null)
        {
            playerLight = playerTransform.GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();
        }

        // Set first wander target
        lastPosition = transform.position;
        SetNewWanderTarget();

        if (debugMode)
            Debug.Log("NPC initialized with physics settings: Drag=" + rb.linearDamping + ", DetectionMode=" + rb.collisionDetectionMode);
    }

    void Update()
    {
        // Skip all behavior if rescued
        if (isRescued)
            return;

        // Check if player is nearby
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            isPlayerNearby = distanceToPlayer <= playerDetectionRadius;
        }

        // Check light intensity and react
        CheckLightConditions();

        // Check if stuck in corner but don't check if already escaping
        if (!isEscapingCorner)
        {
            CheckIfStuck();
        }
    }

    void CheckIfStuck()
    {
        // Calculate how much we've moved
        float distanceMoved = Vector2.Distance(transform.position, lastPosition);

        // If we're trying to move but not actually moving much
        if (rb.linearVelocity.magnitude > 0.5f && distanceMoved < 0.02f)
        {
            stuckTimer += Time.deltaTime;

            if (debugMode && stuckTimer > 0.1f)
                Debug.Log($"Possible stuck state detected. Timer: {stuckTimer:F2}, Movement: {distanceMoved:F4}");

            // If stuck for too long, trigger escape behavior
            if (stuckTimer >= cornerDetectionTime && !isEscapingCorner)
            {
                if (debugMode)
                    Debug.Log("STUCK DETECTED! Initiating escape sequence.");

                StartCoroutine(EscapeCornerImproved());
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        // Update last position
        lastPosition = transform.position;
    }

    IEnumerator EscapeCornerImproved()
    {
        isEscapingCorner = true;
        currentEscapeAttempt = 0;

        while (currentEscapeAttempt < maxEscapeAttempts)
        {
            currentEscapeAttempt++;

            if (debugMode)
                Debug.Log($"Corner escape attempt {currentEscapeAttempt} of {maxEscapeAttempts}");

            // Stop current movement
            rb.linearVelocity = Vector2.zero;

            // Multi-step approach to escape
            yield return AttemptRadialEscape(); // Fixed: removed StartCoroutine, just yield return the coroutine

            // Check if we've successfully moved
            float escapeDistance = Vector2.Distance(transform.position, lastPosition);
            if (escapeDistance > 0.2f)
            {
                if (debugMode)
                    Debug.Log($"Successfully escaped! Moved {escapeDistance:F2} units");

                break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        // If all escape attempts failed, teleport as a last resort
        if (currentEscapeAttempt >= maxEscapeAttempts)
        {
            EmergencyTeleport();
        }

        // Reset stuck state and find a new target
        stuckTimer = 0f;
        isEscapingCorner = false;
        SetNewWanderTarget();
    }

    IEnumerator AttemptRadialEscape()
    {
        // Try 16 directions (full 360 degrees) to find escape route
        int directions = 16;
        float angleStep = 360f / directions;

        // Start with a random offset to avoid always trying the same directions first
        float startAngle = Random.Range(0f, 360f);

        for (int i = 0; i < directions; i++)
        {
            float angle = startAngle + (i * angleStep);
            Vector2 escapeDir = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ).normalized;

            // Check if this direction is clear
            RaycastHit2D hit = Physics2D.Raycast(transform.position, escapeDir, 1.0f, obstacleLayer);

            // Draw rays in scene view for debugging
            if (debugMode)
            {
                Debug.DrawRay(transform.position, escapeDir * 1.0f,
                    hit.collider == null ? Color.green : Color.red, 0.5f);
            }

            if (hit.collider == null)
            {
                // Found clear direction - apply strong force in that direction
                rb.linearVelocity = Vector2.zero; // Clear any existing velocity

                // Apply force to escape
                Vector2 escapePosition = (Vector2)transform.position + (escapeDir * cornerEscapeForce * 0.2f);

                // Move directly to this position over a short time
                float escapeTime = 0.2f;
                float elapsed = 0f;
                Vector2 startPos = transform.position;

                while (elapsed < escapeTime)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / escapeTime;
                    transform.position = Vector2.Lerp(startPos, escapePosition, t);
                    yield return null;
                }

                // Apply an extra impulse for good measure
                rb.AddForce(escapeDir * cornerEscapeForce, ForceMode2D.Impulse);

                if (debugMode)
                    Debug.Log($"Found escape direction: {escapeDir}");

                yield return new WaitForSeconds(0.2f);
                yield break; // Fixed: use yield break instead of return
            }
        }

        // If we got here, no clear direction was found
        if (debugMode)
            Debug.Log("No clear escape direction found");

        // Must return something, so return a small wait
        yield return new WaitForSeconds(0.1f);
    }

    void EmergencyTeleport()
    {
        if (debugMode)
            Debug.Log("EMERGENCY TELEPORT ACTIVATED");

        // Try several positions until finding a safe one
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector2 teleportPos = (Vector2)transform.position + (randomDir * safetyTeleportDistance);

            // Check if position is safe (no obstacles)
            Collider2D hitCollider = Physics2D.OverlapCircle(teleportPos, 0.5f, obstacleLayer);

            if (hitCollider == null)
            {
                // Safe position found
                transform.position = teleportPos;
                rb.linearVelocity = Vector2.zero;

                if (debugMode)
                    Debug.Log($"Emergency teleport successful to {teleportPos}");

                return;
            }
        }

        // If we got here, couldn't find safe teleport - just move toward center of level
        Vector2 centerDir = -transform.position.normalized;
        transform.position = (Vector2)transform.position + (centerDir * safetyTeleportDistance);

        if (debugMode)
            Debug.Log("Emergency fallback teleport toward level center");
    }

    void FixedUpdate()
    {
        if (isRescued || isWaiting || isEscapingCorner)
            return;

        // Frozen in darkness - don't move
        if (isFrozenInDark)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Calculate movement direction
        CalculateMoveDirection();

        // Apply movement - adjusted for panic state
        float actualSpeed = isPanicking ? moveSpeed * panicSpeedMultiplier : moveSpeed;

        // Use MovePosition for more precise movement
        rb.linearVelocity = moveDirection * actualSpeed;

        // Optional: Update animator if you have one
        UpdateAnimation();
    }

    void CalculateMoveDirection()
    {
        // If player is nearby, decide whether to run away or approach
        if (isPlayerNearby)
        {
            Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

            if (runAwayFromPlayer && distanceToPlayer < runAwayDistance)
            {
                // Run away from player
                moveDirection = -directionToPlayer;

                if (debugMode && Time.frameCount % 60 == 0) // Fixed: typo in the original code (a60 -> 60)
                    Debug.Log("NPC running away from player!");
            }
            else if (!runAwayFromPlayer)
            {
                // Move toward player
                moveDirection = directionToPlayer;
            }
            else
            {
                // Default to wandering if not running away
                ContinueWandering();
                return;
            }
        }
        // Otherwise, continue wandering behavior
        else
        {
            ContinueWandering();
            return;
        }

        // Obstacle avoidance - more rays for better detection
        AvoidObstacles();
    }

    void ContinueWandering()
    {
        if (isWandering)
        {
            // Check if we're close to target position
            if (Vector2.Distance(rb.position, targetPosition) < 0.5f)
            {
                StartCoroutine(WaitBeforeNewTarget());
                return;
            }

            moveDirection = ((Vector3)targetPosition - transform.position).normalized;

            // Obstacle avoidance
            AvoidObstacles();
        }
    }

    void AvoidObstacles()
    {
        // Cast multiple rays in an arc in front of movement direction
        bool obstacleDetected = false;

        // Main direction ray
        RaycastHit2D hit = Physics2D.Raycast(transform.position, moveDirection,
            obstacleAvoidanceRadius, obstacleLayer);

        if (hit.collider != null)
        {
            obstacleDetected = true;
        }

        // Side rays
        if (!obstacleDetected)
        {
            Vector2 rightDir = RotateVector(moveDirection, 30);
            Vector2 leftDir = RotateVector(moveDirection, -30);

            RaycastHit2D hitRight = Physics2D.Raycast(transform.position, rightDir,
                obstacleAvoidanceRadius * 0.75f, obstacleLayer);
            RaycastHit2D hitLeft = Physics2D.Raycast(transform.position, leftDir,
                obstacleAvoidanceRadius * 0.75f, obstacleLayer);

            obstacleDetected = hitRight.collider != null || hitLeft.collider != null;
        }

        // If any ray hit an obstacle, find a way around
        if (obstacleDetected)
        {
            moveDirection = FindPathAroundObstacle(moveDirection);
        }
    }

    Vector2 FindPathAroundObstacle(Vector2 currentDirection)
    {
        // Try several directions at increasing angles from the desired direction
        float[] testAngles = new float[] { 45f, -45f, 90f, -90f, 135f, -135f, 180f };

        foreach (float angle in testAngles)
        {
            // Rotate the direction vector by the test angle
            Vector2 testDirection = RotateVector(currentDirection, angle);

            // Check if this direction is clear
            RaycastHit2D hit = Physics2D.Raycast(transform.position, testDirection, obstacleAvoidanceRadius, obstacleLayer);

            if (hit.collider == null)
            {
                // Draw debug rays for clear paths
                if (debugMode)
                {
                    Debug.DrawRay(transform.position, testDirection * obstacleAvoidanceRadius, Color.green, 0.1f);
                }

                // Found a clear direction
                return testDirection;
            }
            else if (debugMode)
            {
                Debug.DrawRay(transform.position, testDirection * hit.distance, Color.red, 0.1f);
            }
        }

        // If no clear direction, return a random direction
        return Random.insideUnitCircle.normalized;
    }

    Vector2 RotateVector(Vector2 vector, float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    void CheckLightConditions()
    {
        if (playerLight == null)
            return;

        // NPC freezes in darkness
        if (playerLight.intensity <= stopMovingInDarknessThreshold)
        {
            if (!isFrozenInDark)
            {
                isFrozenInDark = true;
            }
        }
        else
        {
            // No longer frozen
            if (isFrozenInDark)
            {
                isFrozenInDark = false;
            }

            // Check if light is very bright (overflow malfunction)
            if (playerLight.intensity >= playerLight.intensity * 1.5f)
            {
                isPanicking = true;
            }
            else
            {
                isPanicking = false;
            }
        }
    }

    void SetNewWanderTarget()
    {
        isWandering = true;

        // Try multiple potential targets to find a valid one
        for (int i = 0; i < 10; i++)
        {
            // Get a random direction
            Vector2 randomDirection = Random.insideUnitCircle.normalized;

            // Apply to current position to get a target
            Vector2 potentialTarget = (Vector2)transform.position + randomDirection * Random.Range(3f, wanderRadius);

            // Check if path to target is valid - use a smaller raycast to allow more flexibility
            RaycastHit2D hit = Physics2D.Raycast(transform.position, randomDirection,
                Mathf.Min(3f, Vector2.Distance(transform.position, potentialTarget)), obstacleLayer);

            if (hit.collider == null)
            {
                // Found a clear path
                targetPosition = potentialTarget;

                if (debugMode)
                    Debug.Log($"New wander target set: {targetPosition}");

                return;
            }
        }

        // If we get here, couldn't find a good target, so just pick a short distance
        targetPosition = (Vector2)transform.position + Random.insideUnitCircle.normalized * 2f;

        if (debugMode)
            Debug.Log($"Fallback wander target: {targetPosition}");
    }

    IEnumerator WaitBeforeNewTarget()
    {
        isWandering = false;
        isWaiting = true;
        rb.linearVelocity = Vector2.zero;

        float waitDuration = waitTime * Random.Range(0.7f, 1.3f);

        if (debugMode)
            Debug.Log($"Waiting for {waitDuration:F1} seconds before new target");

        yield return new WaitForSeconds(waitDuration);

        SetNewWanderTarget();
        isWaiting = false;
    }

    void UpdateAnimation()
    {
        if (animator == null)
            return;

        // Set animation parameters based on movement
        animator.SetFloat("Horizontal", moveDirection.x);
        animator.SetFloat("Vertical", moveDirection.y);
        animator.SetFloat("Speed", rb.linearVelocity.magnitude);
        animator.SetBool("IsPanicking", isPanicking);
        animator.SetBool("IsFrozen", isFrozenInDark);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the player has collided with the NPC
        if (collision.gameObject.CompareTag("LightSource") && !isRescued)
        {
            TriggerRescue();
        }
    }

    void TriggerRescue()
    {
        isRescued = true;
        rb.linearVelocity = Vector2.zero;

        Debug.Log("Player found the NPC! Triggering rescue.");

        // Transition to dialogue scene with a small delay
        StartCoroutine(TransitionToDialogueScene());
    }

    IEnumerator TransitionToDialogueScene()
    {
        // Short delay before transitioning
        yield return new WaitForSeconds(sceneTransitionDelay);

        Debug.Log("Transitioning to Dialogue scene");

        // Check if the dialogue scene exists in build settings
        if (SceneUtility.GetBuildIndexByScenePath(dialogueSceneName) >= 0)
        {
            SceneManager.LoadScene(dialogueSceneName);
        }
        else
        {
            Debug.LogError("Scene '" + dialogueSceneName + "' is not in build settings! Add it to build settings.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRadius);

        // Draw obstacle avoidance radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, obstacleAvoidanceRadius);

        // Draw run away distance
        if (runAwayFromPlayer)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, runAwayDistance);
        }

        // Draw current target position
        if (Application.isPlaying && isWandering)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
        }
    }
}