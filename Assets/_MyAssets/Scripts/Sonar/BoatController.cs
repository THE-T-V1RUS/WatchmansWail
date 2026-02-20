using UnityEngine;
using UnityEngine.InputSystem;

public class BoatController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float driftSpeed = 5f;

    [Header("Steering")]
    [SerializeField] private float turnSpeed = 90f; // degrees per second
    [SerializeField] private float returnSpeed = 45f; // degrees per second
    [SerializeField] private float maxRotationAngle = 30f; // max yaw angle

    [Header("Physics")]
    [SerializeField] private bool usePhysics = true;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float collisionDamage = 20f;
    [SerializeField] private float minImpactSpeed = 1f;
    [SerializeField] private float deathFadeDuration = 3f;
    [SerializeField] private SpriteRenderer healthSpriteRenderer;

    private float currentYaw = 0f;
    private float yawVelocity = 0f;
    private float currentHealth;
    private Rigidbody rb;
    private bool isDead = false;
    private float fadeTimer = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null && usePhysics)
        {
            Debug.LogWarning("BoatController: No Rigidbody found. Please add a Rigidbody component to the boat.");
        }

        if (healthSpriteRenderer == null)
        {
            healthSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        currentHealth = maxHealth;
        UpdateHealthColor();
    }

    private void Update()
    {
        HandleSteering();
        UpdateRotation();
        UpdateDeathFade();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void HandleSteering()
    {
        float steerInput = 0f;

        if (Keyboard.current.leftArrowKey.isPressed)
        {
            steerInput = -1f;
        }
        else if (Keyboard.current.rightArrowKey.isPressed)
        {
            steerInput = 1f;
        }

        float targetYaw = steerInput * maxRotationAngle;
        float speed = Mathf.Abs(steerInput) > 0f ? turnSpeed : returnSpeed;
        float smoothTime = speed > 0f ? 1f / speed : 0.01f;
        currentYaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref yawVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
    }

    private void ApplyMovement()
    {
        if (isDead)
        {
            if (usePhysics && rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }
            return;
        }

        if (usePhysics && rb != null)
        {
            rb.linearVelocity = transform.forward * driftSpeed;
        }
        else
        {
            transform.position += transform.forward * driftSpeed * Time.deltaTime;
        }
    }

    private void UpdateRotation()
    {
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minImpactSpeed) return;

        ApplyDamage(collisionDamage);
    }

    private void ApplyDamage(float damage)
    {
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        UpdateHealthColor();

        if (currentHealth <= 0f)
        {
            isDead = true;
            fadeTimer = 0f;
        }
    }

    private void UpdateHealthColor()
    {
        if (healthSpriteRenderer == null) return;

        float healthPercent = maxHealth > 0f ? currentHealth / maxHealth : 0f;

        Color targetColor;
        if (healthPercent > 0.66f)
        {
            float t = (healthPercent - 0.66f) / 0.34f;
            targetColor = Color.Lerp(Color.yellow, Color.white, t);
        }
        else if (healthPercent > 0.33f)
        {
            float t = (healthPercent - 0.33f) / 0.33f;
            targetColor = Color.Lerp(new Color(1f, 0.5f, 0f, 1f), Color.yellow, t);
        }
        else
        {
            float t = healthPercent / 0.33f;
            targetColor = Color.Lerp(Color.red, new Color(1f, 0.5f, 0f, 1f), t);
        }

        healthSpriteRenderer.color = new Color(targetColor.r, targetColor.g, targetColor.b, healthSpriteRenderer.color.a);
    }

    private void UpdateDeathFade()
    {
        if (!isDead || healthSpriteRenderer == null) return;

        fadeTimer += Time.deltaTime;
        float t = deathFadeDuration > 0f ? Mathf.Clamp01(fadeTimer / deathFadeDuration) : 1f;
        Color current = healthSpriteRenderer.color;
        healthSpriteRenderer.color = new Color(current.r, current.g, current.b, 1f - t);
    }
}
