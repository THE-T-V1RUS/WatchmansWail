using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[System.Serializable, VolumeComponentMenu("Post-processing/Pixelate")]
public sealed class Pixelate : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Header("Resolution")]
    public ClampedIntParameter pixelWidth = new ClampedIntParameter(854, 64, 3840);
    public ClampedIntParameter pixelHeight = new ClampedIntParameter(480, 64, 2160);

    [Header("Color Depth (PSX = 32 levels, 0 = off)")]
    public ClampedIntParameter colorDepth = new ClampedIntParameter(0, 0, 256);

    [Header("Dithering")]
    public ClampedFloatParameter ditherStrength = new ClampedFloatParameter(0f, 0f, 2f);
    public ClampedFloatParameter ditherScale = new ClampedFloatParameter(1f, 1f, 8f);

    [Header("Scanlines")]
    public ClampedFloatParameter scanlineIntensity = new ClampedFloatParameter(0f, 0f, 1f);
    public ClampedFloatParameter scanlineWidth = new ClampedFloatParameter(1f, 1f, 4f);

    [Header("CRT Curvature")]
    public ClampedFloatParameter curvatureStrength = new ClampedFloatParameter(0f, 0f, 0.2f);

    [Header("Vignette")]
    public ClampedFloatParameter vignetteStrength = new ClampedFloatParameter(0f, 0f, 2f);

    [Header("Analog Noise")]
    public ClampedFloatParameter noiseStrength = new ClampedFloatParameter(0f, 0f, 0.2f);

    [Header("Chromatic Aberration")]
    public ClampedFloatParameter chromaStrength = new ClampedFloatParameter(0f, 0f, 2f);

    [Header("Bloom Bleed")]
    public ClampedFloatParameter bloomBleedStrength = new ClampedFloatParameter(0f, 0f, 2f);
    public ClampedIntParameter bloomBleedSamples = new ClampedIntParameter(2, 1, 4);

    [Header("Color Tint")]
    public ColorParameter colorTint = new ColorParameter(Color.white);
    public ClampedFloatParameter tintStrength = new ClampedFloatParameter(0f, 0f, 1f);

    [Header("Contrast / Brightness")]
    public ClampedFloatParameter contrast = new ClampedFloatParameter(1f, 0.5f, 2f);
    public ClampedFloatParameter brightness = new ClampedFloatParameter(0f, -0.5f, 0.5f);

    [Header("Interlacing")]
    public ClampedFloatParameter interlaceStrength = new ClampedFloatParameter(0f, 0f, 1f);

    [Header("Horizontal Jitter")]
    public ClampedFloatParameter hJitterStrength = new ClampedFloatParameter(0f, 0f, 0.01f);

    [Header("Color Bleed (Chroma Smear)")]
    public ClampedFloatParameter colorBleedStrength = new ClampedFloatParameter(0f, 0f, 1f);
    public ClampedIntParameter colorBleedSamples = new ClampedIntParameter(3, 1, 8);

    Material m_Material;

    static readonly int PixelWidthId = Shader.PropertyToID("_PixelWidth");
    static readonly int PixelHeightId = Shader.PropertyToID("_PixelHeight");
    static readonly int ColorDepthId = Shader.PropertyToID("_ColorDepth");
    static readonly int DitherStrengthId = Shader.PropertyToID("_DitherStrength");
    static readonly int DitherScaleId = Shader.PropertyToID("_DitherScale");
    static readonly int ScanlineIntensityId = Shader.PropertyToID("_ScanlineIntensity");
    static readonly int ScanlineWidthId = Shader.PropertyToID("_ScanlineWidth");
    static readonly int CurvatureStrengthId = Shader.PropertyToID("_CurvatureStrength");
    static readonly int VignetteStrengthId = Shader.PropertyToID("_VignetteStrength");
    static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
    static readonly int ChromaStrengthId = Shader.PropertyToID("_ChromaStrength");
    static readonly int BloomBleedStrengthId = Shader.PropertyToID("_BloomBleedStrength");
    static readonly int BloomBleedSamplesId = Shader.PropertyToID("_BloomBleedSamples");
    static readonly int ColorTintId = Shader.PropertyToID("_ColorTint");
    static readonly int TintStrengthId = Shader.PropertyToID("_TintStrength");
    static readonly int ContrastId = Shader.PropertyToID("_Contrast");
    static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
    static readonly int InterlaceStrengthId = Shader.PropertyToID("_InterlaceStrength");
    static readonly int HJitterStrengthId = Shader.PropertyToID("_HJitterStrength");
    static readonly int ColorBleedStrengthId = Shader.PropertyToID("_ColorBleedStrength");
    static readonly int ColorBleedSamplesId = Shader.PropertyToID("_ColorBleedSamples");
    static readonly int InputTextureId = Shader.PropertyToID("_InputTexture");

    public bool IsActive() => m_Material != null;

    public override CustomPostProcessInjectionPoint injectionPoint =>
        CustomPostProcessInjectionPoint.AfterPostProcess;

    public override void Setup()
    {
        m_Material = CoreUtils.CreateEngineMaterial("Hidden/PostProcess/Pixelate");
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
            return;

        m_Material.SetInt(PixelWidthId, pixelWidth.value);
        m_Material.SetInt(PixelHeightId, pixelHeight.value);
        m_Material.SetInt(ColorDepthId, colorDepth.value);
        m_Material.SetFloat(DitherStrengthId, ditherStrength.value);
        m_Material.SetFloat(DitherScaleId, ditherScale.value);
        m_Material.SetFloat(ScanlineIntensityId, scanlineIntensity.value);
        m_Material.SetFloat(ScanlineWidthId, scanlineWidth.value);
        m_Material.SetFloat(CurvatureStrengthId, curvatureStrength.value);
        m_Material.SetFloat(VignetteStrengthId, vignetteStrength.value);
        m_Material.SetFloat(NoiseStrengthId, noiseStrength.value);
        m_Material.SetFloat(ChromaStrengthId, chromaStrength.value);
        m_Material.SetFloat(BloomBleedStrengthId, bloomBleedStrength.value);
        m_Material.SetInt(BloomBleedSamplesId, bloomBleedSamples.value);
        m_Material.SetColor(ColorTintId, colorTint.value);
        m_Material.SetFloat(TintStrengthId, tintStrength.value);
        m_Material.SetFloat(ContrastId, contrast.value);
        m_Material.SetFloat(BrightnessId, brightness.value);
        m_Material.SetFloat(InterlaceStrengthId, interlaceStrength.value);
        m_Material.SetFloat(HJitterStrengthId, hJitterStrength.value);
        m_Material.SetFloat(ColorBleedStrengthId, colorBleedStrength.value);
        m_Material.SetInt(ColorBleedSamplesId, colorBleedSamples.value);
        m_Material.SetTexture(InputTextureId, source);
        HDUtils.DrawFullScreen(cmd, m_Material, destination, shaderPassId: 0);
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}
