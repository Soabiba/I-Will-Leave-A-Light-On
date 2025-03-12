using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Light Settings")]
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D playerLight;
    [SerializeField] private float startLightRadius = 5f;
    [SerializeField] private float endLightRadius = 1.5f;

    [Header("Game Timer")]
    [SerializeField] private float totalGameTime = 120f; // 2 minutes
    private float gameTimer;

    [Header("Malfunction Settings")]
    [SerializeField] private float firstMalfunctionMinTime = 20f; // Earliest first malfunction
    [SerializeField] private float firstMalfunctionMaxTime = 60f; // Latest first malfunction
    [SerializeField] private float malfunctionDuration = 2.0f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private bool isMalfunctioning = false;
    private float originalIntensity;
    private float malfunctionTimer;
    private bool firstMalfunctionTriggered = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // If light isn't assigned in inspector, try to get it
        if (playerLight == null)
            playerLight = GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();

        originalIntensity = playerLight.intensity;
        gameTimer = totalGameTime;

        // Set initial light
        playerLight.pointLightOuterRadius = startLightRadius;
        playerLight.pointLightInnerRadius = startLightRadius * 0.5f;

        // Set first malfunction timer
        malfunctionTimer = Random.Range(firstMalfunctionMinTime, firstMalfunctionMaxTime);
        Debug.Log($"First malfunction will occur in approximately {malfunctionTimer.ToString("F1")} seconds");
    }

    void Update()
    {
        // Update game timer
        gameTimer -= Time.deltaTime;

        // Input handling
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        if (!isMalfunctioning)
        {
            // Gradually dim light based on game timer
            float gameProgress = 1 - (gameTimer / totalGameTime);
            float currentLightRadius = Mathf.Lerp(startLightRadius, endLightRadius, gameProgress);

            // Update light radius
            playerLight.pointLightOuterRadius = currentLightRadius;
            playerLight.pointLightInnerRadius = currentLightRadius * 0.5f;

            // Check for malfunction timing
            if (!firstMalfunctionTriggered)
            {
                malfunctionTimer -= Time.deltaTime;

                if (malfunctionTimer <= 0)
                {
                    TriggerRandomMalfunction();
                    firstMalfunctionTriggered = true;
                }
            }
            else if (Random.value < 0.0001f) // Very rare chance for additional malfunctions
            {
                TriggerRandomMalfunction();
            }
        }
    }

    void TriggerRandomMalfunction()
    {
        Debug.Log("Light malfunction triggered!");

        // Choose a random malfunction type
        float randomValue = Random.value;

        if (randomValue < 0.33f)
        {
            // Light goes out completely
            StartCoroutine(BlackoutMalfunction());
        }
        else if (randomValue < 0.66f)
        {
            // Light gets extremely bright
            StartCoroutine(OverflowMalfunction());
        }
        else
        {
            // Severe flicker
            StartCoroutine(FlickerMalfunction());
        }
    }

    IEnumerator BlackoutMalfunction()
    {
        isMalfunctioning = true;

        // Store original values
        float originalRadius = playerLight.pointLightOuterRadius;

        // Light goes out completely
        float fadeOutTime = 0.2f;
        float elapsed = 0;

        // Quick fade to black
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            playerLight.intensity = Mathf.Lerp(originalIntensity, 0, elapsed / fadeOutTime);
            yield return null;
        }

        playerLight.intensity = 0;

        // Stay black for duration
        yield return new WaitForSeconds(malfunctionDuration);

        // Gradually come back
        float recoveryTime = 0.8f;
        elapsed = 0;

        while (elapsed < recoveryTime)
        {
            elapsed += Time.deltaTime;
            playerLight.intensity = Mathf.Lerp(0, originalIntensity, elapsed / recoveryTime);
            yield return null;
        }

        playerLight.intensity = originalIntensity;
        isMalfunctioning = false;
    }

    IEnumerator OverflowMalfunction()
    {
        isMalfunctioning = true;

        // Store original values
        float originalRadius = playerLight.pointLightOuterRadius;

        // Light gets extremely bright and wide
        float rampUpTime = 0.3f;
        float elapsed = 0;

        // Quick ramp up
        while (elapsed < rampUpTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rampUpTime;
            playerLight.intensity = Mathf.Lerp(originalIntensity, originalIntensity * 3f, t);
            playerLight.pointLightOuterRadius = Mathf.Lerp(originalRadius, startLightRadius * 3f, t);
            playerLight.pointLightInnerRadius = Mathf.Lerp(originalRadius * 0.5f, startLightRadius * 1.5f, t);
            yield return null;
        }

        // Stay bright for duration
        yield return new WaitForSeconds(malfunctionDuration);

        // Gradually return to normal
        float recoveryTime = 1.0f;
        elapsed = 0;

        while (elapsed < recoveryTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoveryTime;
            playerLight.intensity = Mathf.Lerp(originalIntensity * 3f, originalIntensity, t);
            playerLight.pointLightOuterRadius = Mathf.Lerp(startLightRadius * 3f, originalRadius, t);
            playerLight.pointLightInnerRadius = Mathf.Lerp(startLightRadius * 1.5f, originalRadius * 0.5f, t);
            yield return null;
        }

        // Reset light intensity
        playerLight.intensity = originalIntensity;
        isMalfunctioning = false;
    }

    IEnumerator FlickerMalfunction()
    {
        isMalfunctioning = true;

        // Severe flickering
        float duration = malfunctionDuration * 1.5f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Random intensity changes
            playerLight.intensity = originalIntensity * Random.Range(0.1f, 1.2f);

            // Random wait time between flickers
            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
        }

        // Reset light
        playerLight.intensity = originalIntensity;
        isMalfunctioning = false;
    }

    void FixedUpdate()
    {
        // Move player
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}