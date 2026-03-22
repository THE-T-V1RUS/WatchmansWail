Shader "Hidden/PostProcess/Pixelate"
{
    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    TEXTURE2D_X(_InputTexture);
    int _PixelWidth;
    int _PixelHeight;

    // Color depth (PSX = 5 bits per channel = 32 levels)
    int _ColorDepth;

    // Dithering
    float _DitherStrength;
    float _DitherScale;

    // Scanlines
    float _ScanlineIntensity;
    float _ScanlineWidth;

    // CRT curvature
    float _CurvatureStrength;

    // Vignette
    float _VignetteStrength;

    // Noise / signal jitter
    float _NoiseStrength;

    // Chromatic aberration
    float _ChromaStrength;

    // Bloom bleed
    float _BloomBleedStrength;
    int _BloomBleedSamples;

    // Color tint
    float4 _ColorTint;
    float _TintStrength;

    // Contrast / brightness
    float _Contrast;
    float _Brightness;

    // Interlacing
    float _InterlaceStrength;

    // Horizontal jitter
    float _HJitterStrength;

    // Color bleed (horizontal chroma smear)
    float _ColorBleedStrength;
    int _ColorBleedSamples;

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    // 4x4 Bayer dither matrix (values 0-15, normalized to 0-1)
    float BayerDither4x4(float2 pixelPos)
    {
        const float bayerPattern[16] = {
             0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
            12.0/16.0,  4.0/16.0, 14.0/16.0,  6.0/16.0,
             3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
            15.0/16.0,  7.0/16.0, 13.0/16.0,  5.0/16.0
        };
        int x = (int)fmod(pixelPos.x, 4.0);
        int y = (int)fmod(pixelPos.y, 4.0);
        return bayerPattern[y * 4 + x] - 0.5; // Center around 0 (-0.5 to +0.5)
    }

    // Hash-based noise for analog signal jitter
    float Hash(float2 p)
    {
        float3 p3 = frac(float3(p.xyx) * 0.1031);
        p3 += dot(p3, p3.yzx + 33.33);
        return frac((p3.x + p3.y) * p3.z);
    }

    // CRT barrel distortion
    float2 CRTCurve(float2 uv, float strength)
    {
        uv = uv * 2.0 - 1.0;
        float2 offset = abs(uv.yx) * strength;
        uv = uv + uv * offset * offset;
        uv = uv * 0.5 + 0.5;
        return uv;
    }

    float4 Frag(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = input.texcoord;

        // --- CRT Curvature ---
        if (_CurvatureStrength > 0.0)
        {
            uv = CRTCurve(uv, _CurvatureStrength);
            if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                return float4(0, 0, 0, 1);
        }

        // --- Horizontal jitter (per-scanline horizontal offset) ---
        if (_HJitterStrength > 0.0)
        {
            float time = _Time.y;
            float row = floor(uv.y * (float)_PixelHeight);
            float jitter = (Hash(float2(row, frac(time * 7.13))) * 2.0 - 1.0) * _HJitterStrength;
            uv.x += jitter;
        }

        // --- Resolution downscale (pixelation) ---
        float2 pixelSize = float2(1.0 / (float)_PixelWidth, 1.0 / (float)_PixelHeight);
        float2 snappedUV = (floor(uv / pixelSize) + 0.5) * pixelSize;

        float4 col;

        // --- Chromatic aberration ---
        if (_ChromaStrength > 0.0)
        {
            float2 centered = snappedUV - 0.5;
            float dist = length(centered);
            float2 dir = centered * dist * _ChromaStrength;

            float r = SAMPLE_TEXTURE2D_X_LOD(_InputTexture, s_point_clamp_sampler, (snappedUV + dir) * _RTHandleScale.xy, 0).r;
            float g = SAMPLE_TEXTURE2D_X_LOD(_InputTexture, s_point_clamp_sampler, snappedUV * _RTHandleScale.xy, 0).g;
            float b = SAMPLE_TEXTURE2D_X_LOD(_InputTexture, s_point_clamp_sampler, (snappedUV - dir) * _RTHandleScale.xy, 0).b;
            float a = SAMPLE_TEXTURE2D_X_LOD(_InputTexture, s_point_clamp_sampler, snappedUV * _RTHandleScale.xy, 0).a;
            col = float4(r, g, b, a);
        }
        else
        {
            col = SAMPLE_TEXTURE2D_X_LOD(_InputTexture, s_point_clamp_sampler, snappedUV * _RTHandleScale.xy, 0);
        }

        // --- Color bleed (horizontal chroma smear) ---
        if (_ColorBleedStrength > 0.0 && _ColorBleedSamples > 0)
        {
            float3 bleed = float3(0, 0, 0);
            float totalWeight = 0.0;
            int samples = _ColorBleedSamples;
            for (int i = 1; i <= samples; i++)
            {
                float weight = 1.0 / (float)i;
                float2 offsetUV = snappedUV - float2(pixelSize.x * (float)i, 0.0);
                bleed += SAMPLE_TEXTURE2D_X_LOD(_InputTexture, s_point_clamp_sampler, offsetUV * _RTHandleScale.xy, 0).rgb * weight;
                totalWeight += weight;
            }
            bleed /= totalWeight;
            col.rgb = lerp(col.rgb, bleed, _ColorBleedStrength);
        }

        // --- Bloom bleed (low-res blur) ---
        if (_BloomBleedStrength > 0.0 && _BloomBleedSamples > 0)
        {
            float3 bloom = float3(0, 0, 0);
            float totalWeight = 0.0;
            int samples = _BloomBleedSamples;
            for (int ox = -samples; ox <= samples; ox++)
            {
                for (int oy = -samples; oy <= samples; oy++)
                {
                    float2 sampleUV = snappedUV + float2((float)ox, (float)oy) * pixelSize;
                    float weight = 1.0 / (1.0 + abs((float)ox) + abs((float)oy));
                    bloom += SAMPLE_TEXTURE2D_X_LOD(_InputTexture, s_point_clamp_sampler, sampleUV * _RTHandleScale.xy, 0).rgb * weight;
                    totalWeight += weight;
                }
            }
            bloom /= totalWeight;
            // Additive blend — only bright areas bleed
            col.rgb += max(bloom - 0.5, 0.0) * _BloomBleedStrength * 2.0;
        }

        // Pixel position in low-res grid (for dither pattern)
        float2 lowResPixel = floor(uv / pixelSize);

        // --- Ordered dithering (Bayer 4x4) ---
        if (_DitherStrength > 0.0)
        {
            float dither = BayerDither4x4(floor(lowResPixel / _DitherScale));
            float levels = max((float)_ColorDepth, 2.0);
            col.rgb += dither * _DitherStrength / levels;
        }

        // --- Color depth reduction (PSX = 32 levels per channel) ---
        if (_ColorDepth > 0)
        {
            float levels = (float)_ColorDepth - 1.0;
            col.rgb = floor(col.rgb * levels + 0.5) / levels;
        }

        // --- Analog noise / signal jitter ---
        if (_NoiseStrength > 0.0)
        {
            float time = _Time.y;
            float noise = Hash(lowResPixel + frac(time * 43.17)) * 2.0 - 1.0;
            col.rgb += noise * _NoiseStrength;
        }

        // --- Scanlines ---
        if (_ScanlineIntensity > 0.0)
        {
            float scanline = fmod(lowResPixel.y, _ScanlineWidth * 2.0);
            float scanFade = step(_ScanlineWidth, scanline);
            col.rgb *= lerp(1.0, 1.0 - _ScanlineIntensity, scanFade);
        }

        // --- CRT Vignette ---
        if (_VignetteStrength > 0.0)
        {
            float2 centered = uv * 2.0 - 1.0;
            float vignette = 1.0 - dot(centered, centered) * _VignetteStrength;
            vignette = saturate(vignette);
            col.rgb *= vignette;
        }

        // --- Contrast / Brightness ---
        if (_Contrast != 1.0 || _Brightness != 0.0)
        {
            col.rgb = (col.rgb - 0.5) * _Contrast + 0.5 + _Brightness;
        }

        // --- Color tint ---
        if (_TintStrength > 0.0)
        {
            col.rgb = lerp(col.rgb, col.rgb * _ColorTint.rgb, _TintStrength);
        }

        // --- Interlacing ---
        if (_InterlaceStrength > 0.0)
        {
            float row = floor(input.positionCS.y);
            float frame = floor(_Time.y * 30.0); // ~30Hz flicker
            float evenOdd = fmod(row + frame, 2.0);
            col.rgb *= lerp(1.0, 1.0 - _InterlaceStrength, evenOdd);
        }

        col.rgb = saturate(col.rgb);
        return col;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "Pixelate"
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }

    Fallback Off
}
