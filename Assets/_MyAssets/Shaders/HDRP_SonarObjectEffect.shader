Shader "_MyAssets/HDRP Sonar Object Effect"
{
    Properties
    {
        [Header(Base Material)]
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)

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
        _RingWidth ("Ring Edge Width", Range(0.01, 0.5)) = 0.1
        _RevealLinger ("Reveal Linger Time (seconds)", Range(0.1, 5.0)) = 1.0
        _RevealFadeTime ("Reveal Fade Time (seconds)", Range(0.1, 5.0)) = 1.0
        _PingElapsedTime ("Ping Elapsed Time (controlled by script)", Float) = 0.0
        _WaveAmplitude ("Wave Amplitude", Range(0.0, 0.2)) = 0.05
        _WaveFrequency ("Wave Frequency", Range(1.0, 20.0)) = 5.0
        _Intensity ("Reveal Intensity", Range(0.5, 2.0)) = 1.0
        _Saturation ("Color Saturation", Range(0.5, 2.0)) = 1.2

        [Header(Glow)]
        [HDR]_GlowColor ("Glow Color", Color) = (0.0, 0.8, 1.0, 1.0)
        _GlowWidth ("Glow Width", Range(0.01, 0.5)) = 0.2
        _GlowIntensity ("Glow Intensity", Range(0.0, 3.0)) = 1.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "GBuffer"
            Tags { "LightMode" = "GBuffer" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : NORMAL;
                float2 uv : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _LowColor;
                float4 _MidColor1;
                float4 _MidColor2;
                float4 _MidColor3;
                float4 _HighColor;
                float4 _GlowColor;
                float4 _BaseMap_ST;
                float4 _BaseColor;
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
                float _Saturation;
                float _GlowWidth;
                float _GlowIntensity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                
                return output;
            }

            void Frag(Varyings input,
                       out float4 outGBuffer0 : SV_Target0,
                       out float4 outGBuffer1 : SV_Target1,
                       out float4 outGBuffer2 : SV_Target2,
                       out float4 outGBuffer3 : SV_Target3)
            {
                // Base material
                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 normal = normalize(input.normalWS);

                // Calculate view depth
                float3 camPos = GetCurrentViewPosition();
                float viewDepth = length(input.positionWS - camPos);
                
                // Normalize to 0-1 range
                float range = max(1e-5, _MaxDepth - _MinDepth);
                float normalizedDepth = saturate((viewDepth - _MinDepth) / range);
                float normalizedNearness = 1.0 - normalizedDepth;

                // Apply warmth curve
                float curved = saturate(pow(normalizedNearness, _RedCurve));
                float edge0 = saturate(_RedThreshold - (_TransitionWidth * 0.5));
                float edge1 = saturate(_RedThreshold + (_TransitionWidth * 0.5));
                edge1 = max(edge1, edge0 + 1e-5);
                float warmBoost = smoothstep(edge0, edge1, curved);
                float paletteT = max(curved, warmBoost);

                // Calculate color stops
                float stop1 = saturate(_RedThreshold - (_ColorDif * 3.0));
                float stop2 = saturate(_RedThreshold - (_ColorDif * 2.0));
                float stop3 = saturate(_RedThreshold - _ColorDif);
                stop3 = min(stop3, 0.999);

                // Map to heatmap colors
                float3 heatmapColor;
                if (paletteT < stop1)
                {
                    float t = saturate(paletteT / max(1e-5, stop1));
                    heatmapColor = lerp(_LowColor.rgb, _MidColor1.rgb, t);
                }
                else if (paletteT < stop2)
                {
                    float t = saturate((paletteT - stop1) / max(1e-5, stop2 - stop1));
                    heatmapColor = lerp(_MidColor1.rgb, _MidColor2.rgb, t);
                }
                else if (paletteT < stop3)
                {
                    float t = saturate((paletteT - stop2) / max(1e-5, stop3 - stop2));
                    heatmapColor = lerp(_MidColor2.rgb, _MidColor3.rgb, t);
                }
                else
                {
                    float t = saturate((paletteT - stop3) / max(1e-5, 1.0 - stop3));
                    heatmapColor = lerp(_MidColor3.rgb, _HighColor.rgb, t);
                }

                // Screen space ping ring
                float2 screenPos = input.positionCS.xy / _ScreenParams.xy;
                float2 pingCenter = float2(_PingCenterX, _PingCenterY);
                float2 toPixel = screenPos - pingCenter;
                float distFromCenter = length(toPixel);

                float elapsedTime = _PingElapsedTime;
                float waveDistortion = sin(distFromCenter * _WaveFrequency + elapsedTime * 2.0) * _WaveAmplitude;
                float timeToReachPixel = (distFromCenter + waveDistortion) / max(0.001, _PingSpeed);
                float timeSinceRevealed = elapsedTime - timeToReachPixel;

                float sonarAlpha = 0.0;
                if (timeSinceRevealed > -_RingWidth && timeSinceRevealed < _RevealLinger + _RevealFadeTime)
                {
                    if (timeSinceRevealed < 0.0)
                    {
                        sonarAlpha = smoothstep(-_RingWidth, 0.0, timeSinceRevealed);
                    }
                    else if (timeSinceRevealed < _RevealLinger)
                    {
                        sonarAlpha = 1.0;
                    }
                    else
                    {
                        float fadeProgress = (timeSinceRevealed - _RevealLinger) / max(0.001, _RevealFadeTime);
                        sonarAlpha = 1.0 - saturate(fadeProgress);
                    }
                }
                
                sonarAlpha *= _Intensity;

                // Glow effect
                float glowAlpha = 0.0;
                if (timeSinceRevealed > -_GlowWidth && timeSinceRevealed < 0.0)
                {
                    glowAlpha = smoothstep(-_GlowWidth, 0.0, timeSinceRevealed) * _GlowIntensity;
                }

                // Apply saturation
                float3 gray = dot(heatmapColor, float3(0.299, 0.587, 0.114));
                heatmapColor = lerp(gray, heatmapColor, _Saturation);

                // Blend glow
                float3 glowedColor = heatmapColor + _GlowColor.rgb * glowAlpha;

                // Blend sonar effect over object
                float3 finalColor = lerp(baseColor.rgb, glowedColor, sonarAlpha);

                // Output to GBuffer
                outGBuffer0 = float4(finalColor, baseColor.a);
                outGBuffer1 = float4(normal * 0.5 + 0.5, 0.0);
                outGBuffer2 = float4(0.0, 0.0, 0.0, 0.0); // Smoothness/Metallic
                outGBuffer3 = float4(0.0, 0.0, 0.0, 0.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma vertex Vert
            #pragma fragment FragForward

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : NORMAL;
                float2 uv : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _LowColor;
                float4 _MidColor1;
                float4 _MidColor2;
                float4 _MidColor3;
                float4 _HighColor;
                float4 _GlowColor;
                float4 _BaseMap_ST;
                float4 _BaseColor;
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
                float _Saturation;
                float _GlowWidth;
                float _GlowIntensity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                
                return output;
            }

            float4 FragForward(Varyings input) : SV_Target
            {
                // Base material
                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 normal = normalize(input.normalWS);

                // Calculate view depth
                float3 camPos = GetCurrentViewPosition();
                float viewDepth = length(input.positionWS - camPos);
                
                // Normalize to 0-1 range
                float range = max(1e-5, _MaxDepth - _MinDepth);
                float normalizedDepth = saturate((viewDepth - _MinDepth) / range);
                float normalizedNearness = 1.0 - normalizedDepth;

                // Apply warmth curve
                float curved = saturate(pow(normalizedNearness, _RedCurve));
                float edge0 = saturate(_RedThreshold - (_TransitionWidth * 0.5));
                float edge1 = saturate(_RedThreshold + (_TransitionWidth * 0.5));
                edge1 = max(edge1, edge0 + 1e-5);
                float warmBoost = smoothstep(edge0, edge1, curved);
                float paletteT = max(curved, warmBoost);

                // Calculate color stops
                float stop1 = saturate(_RedThreshold - (_ColorDif * 3.0));
                float stop2 = saturate(_RedThreshold - (_ColorDif * 2.0));
                float stop3 = saturate(_RedThreshold - _ColorDif);
                stop3 = min(stop3, 0.999);

                // Map to heatmap colors
                float3 heatmapColor;
                if (paletteT < stop1)
                {
                    float t = saturate(paletteT / max(1e-5, stop1));
                    heatmapColor = lerp(_LowColor.rgb, _MidColor1.rgb, t);
                }
                else if (paletteT < stop2)
                {
                    float t = saturate((paletteT - stop1) / max(1e-5, stop2 - stop1));
                    heatmapColor = lerp(_MidColor1.rgb, _MidColor2.rgb, t);
                }
                else if (paletteT < stop3)
                {
                    float t = saturate((paletteT - stop2) / max(1e-5, stop3 - stop2));
                    heatmapColor = lerp(_MidColor2.rgb, _MidColor3.rgb, t);
                }
                else
                {
                    float t = saturate((paletteT - stop3) / max(1e-5, 1.0 - stop3));
                    heatmapColor = lerp(_MidColor3.rgb, _HighColor.rgb, t);
                }

                // Screen space ping ring
                float2 screenPos = input.positionCS.xy / _ScreenParams.xy;
                float2 pingCenter = float2(_PingCenterX, _PingCenterY);
                float2 toPixel = screenPos - pingCenter;
                float distFromCenter = length(toPixel);

                float elapsedTime = _PingElapsedTime;
                float waveDistortion = sin(distFromCenter * _WaveFrequency + elapsedTime * 2.0) * _WaveAmplitude;
                float timeToReachPixel = (distFromCenter + waveDistortion) / max(0.001, _PingSpeed);
                float timeSinceRevealed = elapsedTime - timeToReachPixel;

                float sonarAlpha = 0.0;
                if (timeSinceRevealed > -_RingWidth && timeSinceRevealed < _RevealLinger + _RevealFadeTime)
                {
                    if (timeSinceRevealed < 0.0)
                    {
                        sonarAlpha = smoothstep(-_RingWidth, 0.0, timeSinceRevealed);
                    }
                    else if (timeSinceRevealed < _RevealLinger)
                    {
                        sonarAlpha = 1.0;
                    }
                    else
                    {
                        float fadeProgress = (timeSinceRevealed - _RevealLinger) / max(0.001, _RevealFadeTime);
                        sonarAlpha = 1.0 - saturate(fadeProgress);
                    }
                }
                
                sonarAlpha *= _Intensity;

                // Glow effect
                float glowAlpha = 0.0;
                if (timeSinceRevealed > -_GlowWidth && timeSinceRevealed < 0.0)
                {
                    glowAlpha = smoothstep(-_GlowWidth, 0.0, timeSinceRevealed) * _GlowIntensity;
                }

                // Apply saturation
                float3 gray = dot(heatmapColor, float3(0.299, 0.587, 0.114));
                heatmapColor = lerp(gray, heatmapColor, _Saturation);

                // Blend glow
                float3 glowedColor = heatmapColor + _GlowColor.rgb * glowAlpha;

                // Blend sonar effect over object
                float3 finalColor = lerp(baseColor.rgb, glowedColor, sonarAlpha);

                return float4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/HDRP/Fallback"
}
