Shader "_MyAssets/HDRP World Height Gradient"
{
    Properties
    {
        [HDR]_LowColor ("Low Color", Color) = (0.00, 0.10, 0.45, 1.0)
        [HDR]_HighColor ("High Color", Color) = (1.00, 0.00, 0.00, 1.0)

        _MinWorldY ("Min World Y", Float) = 0.0
        _MaxWorldY ("Max World Y", Float) = 100.0

        _RedThreshold ("Red Start Threshold", Range(0, 1)) = 0.7
        _RedCurve ("Red Transition Curve", Range(0.1, 8.0)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _LowColor;
                float4 _HighColor;
                float _MinWorldY;
                float _MaxWorldY;
                float _RedThreshold;
                float _RedCurve;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);

                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float range = max(1e-5, _MaxWorldY - _MinWorldY);
                float normalizedHeight = saturate((input.positionWS.y - _MinWorldY) / range);

                float curved = pow(normalizedHeight, _RedCurve);
                float thresholded = saturate((curved - _RedThreshold) / max(1e-5, (1.0 - _RedThreshold)));
                float blendFactor = max(normalizedHeight, thresholded);

                float3 color = lerp(_LowColor.rgb, _HighColor.rgb, blendFactor);
                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
