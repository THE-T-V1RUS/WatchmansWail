using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controls the sonar ping timing and triggers associated events.
/// Manages the elapsed time sent to the sonar shader and allows script-driven ping triggering.
/// </summary>
public class SonarPingController : MonoBehaviour
{
    [SerializeField] private Material sonarMaterial;
    
    [Header("Ping Configuration")]
    [SerializeField] private bool pingEnabled = false;
    [SerializeField] private float pingMaxDuration = 3.0f;
    [SerializeField] private bool autoRepeatPing = false;
    [SerializeField] private float timeBetweenPings = 3.0f;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onPingTriggered = new UnityEvent();
    
    private float currentPingTime = 0f;
    private bool isPingActive = false;
    private float timeSinceLastPing = 0f;
    
    public UnityEvent OnPingTriggered => onPingTriggered;
    
    public bool PingEnabled
    {
        get => pingEnabled;
        set => pingEnabled = value;
    }
    
    public bool IsPingActive => isPingActive;
    public float CurrentPingTime => currentPingTime;
    public float CurrentPingProgress => isPingActive ? currentPingTime / pingMaxDuration : 0f;

    private void Start()
    {
        // Auto-find sonar material if not assigned
        if (sonarMaterial == null)
        {
            Debug.LogWarning("SonarPingController: sonarMaterial not assigned. Attempting to find it in scene.");
            // Try to find the custom pass volume and get the material
            // For now, a warning is enough - user should assign it manually
        }
        
        // Reset shader to initial state
        if (sonarMaterial != null)
        {
            sonarMaterial.SetFloat("_PingElapsedTime", pingMaxDuration); // Start with no ping visible
        }
    }

    private void Update()
    {
        // Allow active pings to finish even if pingEnabled is disabled
        if (isPingActive)
        {
            currentPingTime += Time.deltaTime;
            
            // Update the shader property
            if (sonarMaterial != null)
            {
                sonarMaterial.SetFloat("_PingElapsedTime", currentPingTime);
            }
            
            // Check if ping has completed
            if (currentPingTime >= pingMaxDuration)
            {
                isPingActive = false;
                currentPingTime = pingMaxDuration;
            }
        }
        
        // Only start new auto-repeat pings if ping is enabled
        if (pingEnabled && autoRepeatPing && !isPingActive)
        {
            timeSinceLastPing += Time.deltaTime;
            if (timeSinceLastPing >= timeBetweenPings)
            {
                TriggerPing();
                timeSinceLastPing = 0f;
            }
        }
    }
    
    /// <summary>
    /// Triggers a new sonar ping. Resets elapsed time and fires the OnPingTriggered event.
    /// </summary>
    public void TriggerPing()
    {
        if (!pingEnabled) return;
        
        currentPingTime = 0f;
        isPingActive = true;
        timeSinceLastPing = 0f;
        
        // Update shader immediately
        if (sonarMaterial != null)
        {
            sonarMaterial.SetFloat("_PingElapsedTime", 0f);
        }
        
        // Fire the event for sound/effects
        onPingTriggered.Invoke();
    }
    
    /// <summary>
    /// Stops the current ping prematurely.
    /// </summary>
    public void StopPing()
    {
        isPingActive = false;
        currentPingTime = 0f;
        
        if (sonarMaterial != null)
        {
            sonarMaterial.SetFloat("_PingElapsedTime", 0f);
        }
    }
    
    /// <summary>
    /// Sets the material to update. Call this if the material changes at runtime.
    /// </summary>
    public void SetSonarMaterial(Material material)
    {
        sonarMaterial = material;
    }
    
    /// <summary>
    /// Gets the current ping material.
    /// </summary>
    public Material GetSonarMaterial()
    {
        return sonarMaterial;
    }
}
