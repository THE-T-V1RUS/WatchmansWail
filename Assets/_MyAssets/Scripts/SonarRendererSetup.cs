using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Script to set up and manage Renderers custom pass for sonar effect.
/// Attach this to the Custom Pass Volume that has been converted to Renderers type.
/// </summary>
public class SonarRendererSetup : MonoBehaviour
{
    private CustomPassVolume customPassVolume;

    private void Start()
    {
        customPassVolume = GetComponent<CustomPassVolume>();
        
        if (customPassVolume == null)
        {
            Debug.LogError("SonarRendererSetup: CustomPassVolume component not found!");
            return;
        }

        // Verify custom pass volume has passes configured
        if (customPassVolume.customPasses.Count > 0)
        {
            Debug.Log("Renderers custom pass found and configured for Sonar layer effects.");
        }
        else
        {
            Debug.LogWarning("SonarRendererSetup: No custom passes found. Please add a Renderers CustomPass to this volume.");
        }
    }
}
