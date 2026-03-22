using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Optimizes a render-texture camera by disabling expensive HDRP features
/// and rendering at a reduced framerate. Attach to any Camera that renders
/// to a RenderTexture (e.g., in-game monitors).
/// </summary>
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(HDAdditionalCameraData))]
public class LightweightCameraRT : MonoBehaviour
{
    [Header("Render Rate")]
    [Tooltip("Render every Nth frame. 1 = every frame, 4 = every 4th frame (~15fps at 60).")]
    [Range(1, 10)]
    [SerializeField] private int renderEveryNFrames = 4;

    [Header("Feature Overrides")]
    [SerializeField] private bool disableVolumetrics = true;
    [SerializeField] private bool disableSSR = true;
    [SerializeField] private bool disableSSAO = true;
    [SerializeField] private bool disableContactShadows = true;
    [SerializeField] private bool disableSubsurfaceScattering = true;
    [SerializeField] private bool disableVolumetricClouds = true;
    [SerializeField] private bool disableAtmosphericScattering = true;
    [SerializeField] private bool disableTransparentPostprocess = true;
    [SerializeField] private bool disableMotionBlur = true;
    [SerializeField] private bool disableBloom = true;
    [SerializeField] private bool disableShadows = false;

    private Camera cam;
    private HDAdditionalCameraData hdCamData;
    private int frameCounter;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        hdCamData = GetComponent<HDAdditionalCameraData>();

        ApplyFrameSettingsOverrides();

        // Start the camera disabled; we'll manually enable it on the right frames.
        if (renderEveryNFrames > 1)
        {
            cam.enabled = false;
        }
    }

    private void ApplyFrameSettingsOverrides()
    {
        if (hdCamData == null) return;

        // Tell HDRP to use custom frame settings instead of the project defaults.
        hdCamData.customRenderingSettings = true;

        ref var overrideMask = ref hdCamData.renderingPathCustomFrameSettingsOverrideMask;
        ref var settings = ref hdCamData.renderingPathCustomFrameSettings;

        if (disableVolumetrics)
        {
            overrideMask.mask[(uint)FrameSettingsField.Volumetrics] = true;
            settings.SetEnabled(FrameSettingsField.Volumetrics, false);
            overrideMask.mask[(uint)FrameSettingsField.ReprojectionForVolumetrics] = true;
            settings.SetEnabled(FrameSettingsField.ReprojectionForVolumetrics, false);
        }

        if (disableSSR)
        {
            overrideMask.mask[(uint)FrameSettingsField.SSR] = true;
            settings.SetEnabled(FrameSettingsField.SSR, false);
        }

        if (disableSSAO)
        {
            overrideMask.mask[(uint)FrameSettingsField.SSAO] = true;
            settings.SetEnabled(FrameSettingsField.SSAO, false);
        }

        if (disableContactShadows)
        {
            overrideMask.mask[(uint)FrameSettingsField.ContactShadows] = true;
            settings.SetEnabled(FrameSettingsField.ContactShadows, false);
        }

        if (disableSubsurfaceScattering)
        {
            overrideMask.mask[(uint)FrameSettingsField.SubsurfaceScattering] = true;
            settings.SetEnabled(FrameSettingsField.SubsurfaceScattering, false);
        }

        if (disableVolumetricClouds)
        {
            overrideMask.mask[(uint)FrameSettingsField.VolumetricClouds] = true;
            settings.SetEnabled(FrameSettingsField.VolumetricClouds, false);
        }

        if (disableAtmosphericScattering)
        {
            overrideMask.mask[(uint)FrameSettingsField.AtmosphericScattering] = true;
            settings.SetEnabled(FrameSettingsField.AtmosphericScattering, false);
        }

        if (disableTransparentPostprocess)
        {
            overrideMask.mask[(uint)FrameSettingsField.TransparentPostpass] = true;
            settings.SetEnabled(FrameSettingsField.TransparentPostpass, false);
        }

        if (disableMotionBlur)
        {
            overrideMask.mask[(uint)FrameSettingsField.MotionBlur] = true;
            settings.SetEnabled(FrameSettingsField.MotionBlur, false);
        }

        if (disableBloom)
        {
            overrideMask.mask[(uint)FrameSettingsField.Bloom] = true;
            settings.SetEnabled(FrameSettingsField.Bloom, false);
        }

        if (disableShadows)
        {
            overrideMask.mask[(uint)FrameSettingsField.ShadowMaps] = true;
            settings.SetEnabled(FrameSettingsField.ShadowMaps, false);
        }
    }

    private void Update()
    {
        if (renderEveryNFrames <= 1) return;

        frameCounter++;
        if (frameCounter >= renderEveryNFrames)
        {
            frameCounter = 0;
            cam.Render();
        }
    }
}
