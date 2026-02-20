Shader "_MyAssets/HDRP Fullscreen Sonar Ping"
{
    Properties
    {
        [Header(Colors)]
        [HDR]_LowColor ("Low Color", Color) = (0.00, 0.10, 0.45, 1.0)
        [HDR]_MidColor1 ("Mid Color 1 (Green)", Color) = (0.00, 0.85, 0.30, 1.0)
        [HDR]_MidColor2 ("Mid Color 2 (Yellow)", Color) = (1.00, 0.90, 0.10, 1.0)
        [HDR]_MidColor3 ("Mid Color 3 (Orange)", Color) = (1.00, 0.50, 0.05, 1.0)
        [HDR]_HighColor ("High Color", Color) = (1.00, 0.00, 0.00, 1.0)

        [Header(Warmth Transition)]
        _RedThreshold ("Red Start Threshold", Range(0, 1)) = 0.6
        _ColorDif ("Color Transition Step", Range(0.01, 0.4)) = 0.1
        _TransitionWidth ("Transition Width", Range(0.01, 1.0)) = 0.8
        _RedCurve ("Red Transition Curve", Range(0.1, 8.0)) = 2.0

        [Header(Depth Range)]
        _MinDepth ("Min View Depth (Near)", Range(0, 500)) = 0.0
        _MaxDepth ("Max View Depth (Far)", Range(0, 500)) = 200.0

        [Header(Sonar Ping)]
        _PingCenterX ("Ping Center X (0-1)", Range(0, 1)) = 0.5
        _PingCenterY ("Ping Center Y (0-1)", Range(0, 1)) = 0.5
        _PingSpeed ("Ping Expansion Speed", Range(0.1, 5.0)) = 1.0
        _PingMaxDuration ("Ping Max Duration (seconds)", Range(0.5, 10.0)) = 3.0
        _RingWidth ("Ring Edge Width (0-1 screen)", Range(0.01, 0.5)) = 0.1
        _RevealLinger ("Reveal Linger Time (seconds)", Range(0.1, 5.0)) = 1.0
        _RevealFadeTime ("Reveal Fade Time (seconds)", Range(0.1, 5.0)) = 1.0
        _PingElapsedTime ("Ping Elapsed Time (controlled by script)", Float) = 0.0
        _WaveAmplitude ("Wave Amplitude (0-1 screen)", Range(0.0, 0.2)) = 0.05
        _WaveFrequency ("Wave Frequency", Range(1.0, 20.0)) = 5.0
        _Intensity ("Reveal Intensity", Range(0.5, 2.0)) = 1.0
        _BaselineAlpha ("Baseline Visibility", Range(0.0, 0.3)) = 0.1
        _Saturation ("Color Saturation", Range(0.5, 2.0)) = 1.2

        [Header(Glow)]
        [HDR]_GlowColor ("Glow Color", Color) = (0.0, 0.8, 1.0, 1.0)
        _GlowWidth ("Glow Width (0-1 screen)", Range(0.01, 0.5)) = 0.2
        _GlowIntensity ("Glow Intensity", Range(0.0, 3.0)) = 1.5
    }

    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile _ DEBUG_DISPLAY

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _LowColor;
            float4 _MidColor1;
            float4 _MidColor2;
            float4 _MidColor3;
            float4 _HighColor;
            float4 _GlowColor;
            float _MinDepth;
            float _MaxDepth;
            float _RedThreshold;
            float _ColorDif;
            float _TransitionWidth;
            float _RedCurve;
            float _PingCenterX;
            float _PingCenterY;
            float _PingSpeed;
            float _PingMaxDuration;
            float _RingWidth;
            float _RevealLinger;
            float _RevealFadeTime;
            float _PingElapsedTime;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _Intensity;
            float _BaselineAlpha;
            float _Saturation;
            float _GlowWidth;
            float _GlowIntensity;
        CBUFFER_END

        struct HeightPassVaryings
        {
            float4 positionCS : SV_POSITION;
        };

        HeightPassVaryings Vert(uint vertexID : SV_VertexID)
        {
            HeightPassVaryings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
            return output;
        }

        float4 FullScreenPass(HeightPassVaryings varyings) : SV_Target
        {
            float depth = LoadCameraDepth(varyings.positionCS.xy);

            PositionInputs posInput = GetPositionInput(
                varyings.positionCS.xy,
                _ScreenSize.zw,
                depth,
                UNITY_MATRIX_I_VP,
                UNITY_MATRIX_V
            );

            float viewDepth = posInput.linearDepth;
            float range = max(1e-5, _MaxDepth - _MinDepth);
            float normalizedDepth = saturate((viewDepth - _MinDepth) / range);
            float normalizedNearness = 1.0 - normalizedDepth;

            float curved = saturate(pow(normalizedNearness, _RedCurve));
            float edge0 = saturate(_RedThreshold - (_TransitionWidth * 0.5));
            float edge1 = saturate(_RedThreshold + (_TransitionWidth * 0.5));
            edge1 = max(edge1, edge0 + 1e-5);
            float warmBoost = smoothstep(edge0, edge1, curved);
            float paletteT = max(curved, warmBoost);

            float stop1 = saturate(_RedThreshold - (_ColorDif * 3.0));
            float stop2 = saturate(_RedThreshold - (_ColorDif * 2.0));
            float stop3 = saturate(_RedThreshold - _ColorDif);
            stop3 = min(stop3, 0.999);

            float3 color;
            if (paletteT < stop1)
            {
                float t = saturate(paletteT / max(1e-5, stop1));
                color = lerp(_LowColor.rgb, _MidColor1.rgb, t);
            }
            else if (paletteT < stop2)
            {
                float t = saturate((paletteT - stop1) / max(1e-5, stop2 - stop1));
                color = lerp(_MidColor1.rgb, _MidColor2.rgb, t);
            }
            else if (paletteT < stop3)
            {
                float t = saturate((paletteT - stop2) / max(1e-5, stop3 - stop2));
                color = lerp(_MidColor2.rgb, _MidColor3.rgb, t);
            }
            else
            {
                float t = saturate((paletteT - stop3) / max(1e-5, 1.0 - stop3));
                color = lerp(_MidColor3.rgb, _HighColor.rgb, t);
            }

            float2 screenUV = varyings.positionCS.xy * _ScreenSize.zw;
            float2 pingCenter = float2(_PingCenterX, _PingCenterY);
            float2 toPixel = screenUV - pingCenter;
            float distFromCenter = length(toPixel);

            float elapsedTime = _PingElapsedTime;
            
            // Apply wave distortion to the ring
            float waveDistortion = sin(distFromCenter * _WaveFrequency + elapsedTime * 2.0) * _WaveAmplitude;
            
            float timeToReachPixel = (distFromCenter + waveDistortion) / max(0.001, _PingSpeed);
            float timeSinceRevealed = elapsedTime - timeToReachPixel;
            
            float sonarAlpha = 0.0;
            if (timeSinceRevealed > -_RingWidth && timeSinceRevealed < _RevealLinger + _RevealFadeTime)
            {
                if (timeSinceRevealed < 0.0)
                {
                    // Leading edge fade-in as ring approaches
                    sonarAlpha = smoothstep(-_RingWidth, 0.0, timeSinceRevealed);
                }
                else if (timeSinceRevealed < _RevealLinger)
                {
                    // Full visibility during linger
                    sonarAlpha = 1.0;
                }
                else
                {
                    // Linger fade-out
                    float fadeProgress = (timeSinceRevealed - _RevealLinger) / max(0.001, _RevealFadeTime);
                    sonarAlpha = 1.0 - saturate(fadeProgress);
                }
            }
            
            sonarAlpha *= _Intensity;
            
            // Add neon glow to the outer edge of the ring
            float glowAlpha = 0.0;
            if (timeSinceRevealed > -_GlowWidth && timeSinceRevealed < 0.0)
            {
                // Glow appears as the ring approaches, fades in smoothly
                glowAlpha = smoothstep(-_GlowWidth, 0.0, timeSinceRevealed) * _GlowIntensity;
            }
            
            // Apply saturation boost
            float3 gray = dot(color, float3(0.299, 0.587, 0.114));
            color = lerp(gray, color, _Saturation);
            
            // Blend glow color additively with the depth color
            float3 glowedColor = color + _GlowColor.rgb * glowAlpha;
            
            // Blend baseline color visibility with sonar reveal
            float3 baselineColor = lerp(float3(0.0, 0.0, 0.0), glowedColor, _BaselineAlpha);
            float3 finalColor = lerp(baselineColor, glowedColor, sonarAlpha);
            
            return float4(finalColor, 1.0);
        }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "FullScreenPass"
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }

    Fallback Off
}
