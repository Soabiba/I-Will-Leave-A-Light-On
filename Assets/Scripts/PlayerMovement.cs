using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Light Settings")]
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D playerLight;
    [SerializeField] private float minLightRadius;
    [SerializeField] private float maxLightRadius;
    [SerializeField] private float currentLightRadius;

    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // If light isn't assigned in inspector, try to get it
        if (playerLight == null)
            playerLight = GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();

        currentLightRadius = maxLightRadius;
        UpdateLightRadius();
    }

    void Update()
    {
        // Input handling
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Light radius controls (optional)
        if (Input.GetKey(KeyCode.Q))
        {
            currentLightRadius = Mathf.Max(minLightRadius, currentLightRadius - Time.deltaTime * 2);
            UpdateLightRadius();
        }

        if (Input.GetKey(KeyCode.E))
        {
            currentLightRadius = Mathf.Min(maxLightRadius, currentLightRadius + Time.deltaTime * 2);
            UpdateLightRadius();
        }
    }

    void FixedUpdate()
    {
        // Move player
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }

    void UpdateLightRadius()
    {
        if (playerLight != null)
        {
            playerLight.pointLightOuterRadius = currentLightRadius;
            playerLight.pointLightInnerRadius = currentLightRadius * 0.5f;
        }
    }
}