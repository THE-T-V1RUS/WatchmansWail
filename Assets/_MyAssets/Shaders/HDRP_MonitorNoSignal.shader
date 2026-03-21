Shader "_MyAssets/HDRP Monitor No Signal"
{
    Properties
    {
        [Header(Power)]
        _Power("Monitor Power", Range(0, 1)) = 1

        [Header(Background)]
        _TintColor("Static Tint", Color) = (0.82, 0.85, 0.90, 1)
        _BackgroundDark("Background Dark", Range(0, 2)) = 0.08
        _BackgroundBright("Background Bright", Range(0, 2)) = 0.68
        _NoiseScale("Noise Scale", Range(32, 1200)) = 420

        [Header(Motion)]
        _TimeScale("Time Scale", Range(0, 4)) = 1
        _StaticIntensity("Static Intensity", Range(0, 2)) = 1
        _FlickerIntensity("Flicker Intensity", Range(0, 1)) = 0.35
        _ScanlineIntensity("Scanline Intensity", Range(0, 1)) = 0.25
        _Distortion("Horizontal Distortion", Range(0, 0.08)) = 0.02

        [Header(Text)]
        [HDR]_TextColor("Text Color", Color) = (1.0, 0.98, 0.95, 1)
        [HDR]_TextGlowColor("Text Glow Color", Color) = (1.8, 1.7, 1.55, 1)
        _TextGlow("Text Glow", Range(0, 3)) = 0.9
        _TextBlinkSpeed("Text Blink Speed", Range(0, 12)) = 1.4
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

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
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _TintColor;
                float4 _TextColor;
                float4 _TextGlowColor;
                float _Power;
                float _BackgroundDark;
                float _BackgroundBright;
                float _NoiseScale;
                float _TimeScale;
                float _StaticIntensity;
                float _FlickerIntensity;
                float _ScanlineIntensity;
                float _Distortion;
                float _TextGlow;
                float _TextBlinkSpeed;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float RectMask(float2 p, float2 center, float2 halfSize)
            {
                float2 d = abs(p - center) - halfSize;
                float outside = max(d.x, d.y);
                return 1.0 - step(0.0, outside);
            }

            float Glyph5x7(float2 p, int r0, int r1, int r2, int r3, int r4, int r5, int r6)
            {
                if (p.x < 0.0 || p.x >= 1.0 || p.y < 0.0 || p.y >= 1.0)
                {
                    return 0.0;
                }

                int px = (int)floor(p.x * 5.0);
                int py = (int)floor((1.0 - p.y) * 7.0);

                int row = 0;
                if (py == 0) row = r0;
                else if (py == 1) row = r1;
                else if (py == 2) row = r2;
                else if (py == 3) row = r3;
                else if (py == 4) row = r4;
                else if (py == 5) row = r5;
                else if (py == 6) row = r6;

                int bitIndex = 4 - px;
                return ((row >> bitIndex) & 1) ? 1.0 : 0.0;
            }

            float DrawGlyph(float2 uv, float2 origin, float2 size, int r0, int r1, int r2, int r3, int r4, int r5, int r6)
            {
                float2 p = (uv - origin) / max(size, 1e-5);
                return Glyph5x7(p, r0, r1, r2, r3, r4, r5, r6);
            }

            float DrawNoSignal(float2 uv)
            {
                float2 charSize = float2(0.058, 0.115);
                float spacing = 0.014;
                float2 start = float2(0.18, 0.44);

                float text = 0.0;
                float2 pos = start;

                // N
                text = max(text, DrawGlyph(uv, pos, charSize, 17, 25, 21, 19, 17, 17, 17));
                pos.x += charSize.x + spacing;

                // O
                text = max(text, DrawGlyph(uv, pos, charSize, 14, 17, 17, 17, 17, 17, 14));
                pos.x += charSize.x + spacing;

                // space
                pos.x += charSize.x * 0.45;

                // S
                text = max(text, DrawGlyph(uv, pos, charSize, 15, 16, 16, 14, 1, 1, 30));
                pos.x += charSize.x + spacing;

                // I
                text = max(text, DrawGlyph(uv, pos, charSize, 31, 4, 4, 4, 4, 4, 31));
                pos.x += charSize.x + spacing;

                // G
                text = max(text, DrawGlyph(uv, pos, charSize, 14, 17, 16, 23, 17, 17, 14));
                pos.x += charSize.x + spacing;

                // N
                text = max(text, DrawGlyph(uv, pos, charSize, 17, 25, 21, 19, 17, 17, 17));
                pos.x += charSize.x + spacing;

                // A
                text = max(text, DrawGlyph(uv, pos, charSize, 14, 17, 17, 31, 17, 17, 17));
                pos.x += charSize.x + spacing;

                // L
                text = max(text, DrawGlyph(uv, pos, charSize, 16, 16, 16, 16, 16, 16, 31));

                return text;
            }

            float DrawNoSignalGlow(float2 uv, float offset)
            {
                float g = 0.0;
                g = max(g, DrawNoSignal(uv + float2(offset, 0.0)));
                g = max(g, DrawNoSignal(uv + float2(-offset, 0.0)));
                g = max(g, DrawNoSignal(uv + float2(0.0, offset)));
                g = max(g, DrawNoSignal(uv + float2(0.0, -offset)));
                g = max(g, DrawNoSignal(uv + float2(offset, offset)));
                g = max(g, DrawNoSignal(uv + float2(-offset, offset)));
                g = max(g, DrawNoSignal(uv + float2(offset, -offset)));
                g = max(g, DrawNoSignal(uv + float2(-offset, -offset)));
                return g;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float power = saturate(_Power);
                float t = _Time.y * _TimeScale;
                float frameRate = 24.0;
                float frameTick = floor(t * frameRate);
                float frameLerp = frac(t * frameRate);

                float bootScale = lerp(50.0, 1.0, smoothstep(0.0, 0.18, power));
                float2 uvBoot = float2(input.uv.x, (input.uv.y - 0.5) * bootScale + 0.5);

                float inScreen = step(0.0, uvBoot.x) * step(uvBoot.x, 1.0) * step(0.0, uvBoot.y) * step(uvBoot.y, 1.0);
                float2 uv = saturate(uvBoot);

                float distortionGate = step(0.8, Hash21(float2(floor(t * 5.0), 2.0)));
                float horizontalWarp = sin((uv.y * 140.0) + t * 8.0) * _Distortion * distortionGate;
                float2 uvDistorted = float2(frac(uv.x + horizontalWarp), uv.y);

                // Blend between two random states to keep movement without directional drift.
                float frameTickNext = frameTick + 1.0;

                float2 coarseJitterA = (float2(Hash21(float2(frameTick, 1.3)), Hash21(float2(frameTick, 2.7))) - 0.5) * 160.0;
                float2 coarseJitterB = (float2(Hash21(float2(frameTickNext, 1.3)), Hash21(float2(frameTickNext, 2.7))) - 0.5) * 160.0;
                float2 fineJitterA = (float2(Hash21(float2(frameTick, 3.1)), Hash21(float2(frameTick, 4.9))) - 0.5) * 42.0;
                float2 fineJitterB = (float2(Hash21(float2(frameTickNext, 3.1)), Hash21(float2(frameTickNext, 4.9))) - 0.5) * 42.0;

                float coarseNoiseA = Hash21(floor(uvDistorted * _NoiseScale + coarseJitterA));
                float coarseNoiseB = Hash21(floor(uvDistorted * _NoiseScale + coarseJitterB));
                float coarseNoise = lerp(coarseNoiseA, coarseNoiseB, frameLerp);

                float fineNoiseA = ValueNoise(uvDistorted * (_NoiseScale * 0.35) + fineJitterA);
                float fineNoiseB = ValueNoise(uvDistorted * (_NoiseScale * 0.35) + fineJitterB);
                float fineNoise = lerp(fineNoiseA, fineNoiseB, frameLerp);
                float staticMix = saturate(lerp(coarseNoise, fineNoise, 0.45) * _StaticIntensity);

                float scan = 0.5 + 0.5 * sin(uvDistorted.y * _ScreenParams.y * 1.45);
                float scanMul = 1.0 - (_ScanlineIntensity * (1.0 - scan));

                float randomFlicker = Hash21(float2(floor(t * 24.0), 0.0));
                float harmonicFlicker = 0.5 + 0.5 * sin(t * 57.0);
                float flicker = lerp(1.0, lerp(harmonicFlicker, randomFlicker, 0.4), _FlickerIntensity);

                float baseLevel = lerp(_BackgroundDark, _BackgroundBright, staticMix);
                float3 staticColor = _TintColor.rgb * baseLevel * scanMul * flicker;

                float2 snowJitterA = (float2(Hash21(float2(frameTick, 8.0)), Hash21(float2(frameTick, 9.0))) - 0.5) * 300.0;
                float2 snowJitterB = (float2(Hash21(float2(frameTickNext, 8.0)), Hash21(float2(frameTickNext, 9.0))) - 0.5) * 300.0;
                float snowA = step(0.992, Hash21(uvDistorted * _NoiseScale * 2.4 + snowJitterA));
                float snowB = step(0.992, Hash21(uvDistorted * _NoiseScale * 2.4 + snowJitterB));
                float snow = lerp(snowA, snowB, frameLerp);
                staticColor += snow * 0.6 * _StaticIntensity;

                float textJitterGate = step(0.83, Hash21(float2(floor(t * 10.0), 7.0)));
                float textJitter = (Hash21(float2(floor(t * 90.0), 11.0)) - 0.5) * _Distortion * 10.0 * textJitterGate;
                float2 textUV = uvDistorted + float2(textJitter, 0.0);

                float textMask = DrawNoSignal(textUV);
                float glowMask = DrawNoSignalGlow(textUV, 0.0035);

                float textBlink = 0.75 + 0.25 * sin(t * _TextBlinkSpeed * 6.2831853);
                float3 textColor = _TextColor.rgb * textMask * textBlink;
                float3 glowColor = _TextGlowColor.rgb * (glowMask - textMask) * _TextGlow * (0.7 + 0.3 * textBlink);

                float vignette = saturate(1.0 - dot((uv - 0.5) * 1.6, (uv - 0.5) * 1.6));
                staticColor *= lerp(0.45, 1.0, vignette);

                float3 finalColor = staticColor + textColor + glowColor;
                finalColor *= power * inScreen;
                float finalAlpha = power * inScreen;

                return float4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
