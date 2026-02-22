using UnityEngine;
using UnityEngine.InputSystem;

public class BoatController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float driftSpeed = 5f;

    [Header("Steering")]
    [SerializeField] private float maxTurnRate = 60f; // degrees per second
    [SerializeField] private float turnAcceleration = 180f; // degrees per second^2
    [SerializeField] private float turnDeceleration = 120f; // degrees per second^2
    [SerializeField] private float inputResponse = 4f; // input smoothing
    [SerializeField] private float minTurnRadius = 5f; // minimum turn radius in world units
    [SerializeField] private float maxYawAngle = 80f; // max left/right yaw angle

    [Header("Physics")]
    [SerializeField] private bool usePhysics = true;

    [Header("Speed")]
    [SerializeField] private float acceleration = 6f; // units per second^2
    [SerializeField] private float deceleration = 8f; // units per second^2

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float collisionDamage = 20f;
    [SerializeField] private float minImpactSpeed = 1f;
    [SerializeField] private float maxImpactSpeed = 8f;
    [SerializeField] private float invulnerableDuration = 0.6f;
    [SerializeField] private float deathFadeDuration = 3f;
    [SerializeField] private SpriteRenderer healthSpriteRenderer;

    [Header("Feedback")]
    [SerializeField] private AudioClip collisionClip;
    [SerializeField, Range(0f, 1f)] private float collisionVolume = 0.9f;
    [SerializeField] private Transform shakeTarget;
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 0.08f;
    [SerializeField] private float shakeFrequency = 30f;
    [SerializeField] private float shakeDamping = 8f;

    [Header("Camera Follow")]
    [SerializeField] private Transform followCamera;
    [SerializeField] private float followXSpeed = 5f;
    [SerializeField] private float minCameraX = -5f;
    [SerializeField] private float maxCameraX = 5f;
    [SerializeField] private float followZOffset = -10f;
    [SerializeField] private float followZSpeed = 5f;

    [Header("Distance Tracking")]
    [SerializeField] private float targetZPosition = 100f;

    private float currentYaw = 0f;
    private float currentTurnRate = 0f;
    private float steeringInput = 0f;
    private float currentSpeed = 0f;
    private float currentHealth;
    private Rigidbody rb;
    
    // Public distance property for UI access
    public float DistanceToSafeZone { get; private set; }
    private bool isDead = false;
    private float fadeTimer = 0f;
    private float cameraXVelocity = 0f;
    private float cameraZVelocity = 0f;
    private Vector3 shakeBaseLocalPos;
    private Coroutine shakeRoutine;
    private float invulnerableTimer = 0f;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private RigidbodyConstraints initialConstraints;
    private bool hasCachedConstraints = false;

    private void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            initialConstraints = rb.constraints;
            hasCachedConstraints = true;
        }

        if (rb == null && usePhysics)
        {
            Debug.LogWarning("BoatController: No Rigidbody found. Please add a Rigidbody component to the boat.");
        }

        if (healthSpriteRenderer == null)
        {
            healthSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (shakeTarget == null && healthSpriteRenderer != null)
        {
            shakeTarget = healthSpriteRenderer.transform;
        }

        if (shakeTarget != null)
        {
            shakeBaseLocalPos = shakeTarget.localPosition;
        }

        currentHealth = maxHealth;
        UpdateHealthColor();
        ResetBoatState();
    }

    private void OnEnable()
    {
        if (hasCachedConstraints || rb != null)
        {
            ResetBoatState();
        }
    }

    private void OnDisable()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
    }

    private void Update()
    {
        if (invulnerableTimer > 0f)
        {
            invulnerableTimer -= Time.deltaTime;
        }

        if (isDead)
        {
            UpdateDeathFade();
            return;
        }

        ReadSteeringInput();
        if (!usePhysics)
        {
            ApplySteering(Time.deltaTime);
            UpdateRotation();
        }
        UpdateDistanceToSafeZone();
        UpdateDeathFade();
    }

    private void LateUpdate()
    {
        UpdateCameraFollow();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        if (usePhysics)
        {
            ApplySteering(Time.fixedDeltaTime);
            UpdateRotation();
        }
        ApplyMovement(Time.fixedDeltaTime);
    }

    private void ReadSteeringInput()
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

        steeringInput = Mathf.MoveTowards(steeringInput, steerInput, inputResponse * Time.deltaTime);
    }

    private void ApplySteering(float deltaTime)
    {
        float targetTurnRate = steeringInput * maxTurnRate;
        if (minTurnRadius > 0.01f && currentSpeed > 0.01f)
        {
            float maxYawRateFromRadius = Mathf.Rad2Deg * (currentSpeed / minTurnRadius);
            float clampedMax = Mathf.Min(Mathf.Abs(maxTurnRate), maxYawRateFromRadius);
            targetTurnRate = Mathf.Clamp(targetTurnRate, -clampedMax, clampedMax);
        }
        float accel = Mathf.Abs(steeringInput) > 0.01f ? turnAcceleration : turnDeceleration;
        currentTurnRate = Mathf.MoveTowards(currentTurnRate, targetTurnRate, accel * deltaTime);
        currentYaw += currentTurnRate * deltaTime;
        currentYaw = Mathf.Clamp(currentYaw, -Mathf.Abs(maxYawAngle), Mathf.Abs(maxYawAngle));
    }

    private void ApplyMovement(float deltaTime)
    {
        if (isDead)
        {
            if (usePhysics && rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }
            return;
        }

        float targetSpeed = Mathf.Max(0f, driftSpeed);
        float speedAccel = targetSpeed > currentSpeed ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedAccel * deltaTime);
        currentSpeed = Mathf.Max(0f, currentSpeed);

        if (usePhysics && rb != null)
        {
            rb.linearVelocity = transform.forward * currentSpeed;
        }
        else
        {
            transform.position += transform.forward * currentSpeed * deltaTime;
        }
    }

    private void UpdateRotation()
    {
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;
        if (invulnerableTimer > 0f) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minImpactSpeed) return;

        ApplyDamage(collisionDamage);
        PlayCollisionFeedback(impactSpeed);
        invulnerableTimer = invulnerableDuration;
    }

    private void ApplyDamage(float damage)
    {
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        UpdateHealthColor();

        if (currentHealth <= 0f)
        {
            isDead = true;
            fadeTimer = 0f;
            if (usePhysics && rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
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

    private void UpdateCameraFollow()
    {
        if (followCamera == null) return;

        Vector3 camPos = followCamera.position;
        float targetX = rb != null ? rb.position.x : transform.position.x;
        targetX = Mathf.Clamp(targetX, minCameraX, maxCameraX);
        camPos.x = Mathf.SmoothDamp(camPos.x, targetX, ref cameraXVelocity, 1f / Mathf.Max(0.01f, followXSpeed), Mathf.Infinity, Time.deltaTime);

        float targetZ = (rb != null ? rb.position.z : transform.position.z) + followZOffset;
        camPos.z = Mathf.SmoothDamp(camPos.z, targetZ, ref cameraZVelocity, 1f / Mathf.Max(0.01f, followZSpeed), Mathf.Infinity, Time.deltaTime);
        followCamera.position = camPos;
    }

    private void UpdateDistanceToSafeZone()
    {
        float currentZ = rb != null ? rb.position.z : transform.position.z;
        DistanceToSafeZone = Mathf.Max(0f, targetZPosition - currentZ);
    }

    private void ResetBoatState()
    {
        isDead = false;
        fadeTimer = 0f;
        invulnerableTimer = 0f;
        currentYaw = 0f;
        currentTurnRate = 0f;
        steeringInput = 0f;
        currentSpeed = 0f;

        transform.position = initialPosition;
        transform.rotation = initialRotation;

        if (rb != null)
        {
            if (!hasCachedConstraints)
            {
                initialConstraints = rb.constraints;
                hasCachedConstraints = true;
            }

            rb.constraints = initialConstraints;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = initialPosition;
            rb.rotation = initialRotation;
        }

        currentHealth = maxHealth;
        UpdateHealthColor();

        if (healthSpriteRenderer != null)
        {
            Color color = healthSpriteRenderer.color;
            healthSpriteRenderer.color = new Color(color.r, color.g, color.b, 1f);
        }

        if (shakeTarget != null)
        {
            shakeTarget.localPosition = shakeBaseLocalPos;
        }
    }

    private void PlayCollisionFeedback(float impactSpeed)
    {
        float denom = Mathf.Max(0.01f, maxImpactSpeed - minImpactSpeed);
        float intensity = Mathf.Clamp01((impactSpeed - minImpactSpeed) / denom);
        float volume = Mathf.Lerp(0.4f, 1f, intensity) * collisionVolume;

        AudioManager.Instance.PlaySfx(collisionClip, volume);

        if (shakeTarget != null)
        {
            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
                shakeTarget.localPosition = shakeBaseLocalPos;
            }

            shakeRoutine = StartCoroutine(ShakeSprite(intensity));
        }
    }

    private System.Collections.IEnumerator ShakeSprite(float intensity)
    {
        float duration = shakeDuration * Mathf.Lerp(0.7f, 1.3f, intensity);
        float magnitude = shakeMagnitude * Mathf.Lerp(0.5f, 1.6f, intensity);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float damper = Mathf.Exp(-shakeDamping * elapsed);
            float angle = elapsed * shakeFrequency * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * magnitude * damper;
            shakeTarget.localPosition = shakeBaseLocalPos + offset;
            yield return null;
        }

        shakeTarget.localPosition = shakeBaseLocalPos;
        shakeRoutine = null;
    }
}
