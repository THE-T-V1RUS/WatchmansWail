Shader "_MyAssets/HDRP HeightMap Gradient"
{
    Properties
    {
        _HeightMap ("Height Map", 2D) = "white" {}

        [HDR]_LowColor ("Low Color", Color) = (0.00, 0.10, 0.45, 1.0)
        [HDR]_HighColor ("High Color", Color) = (1.00, 0.00, 0.00, 1.0)

        _MinHeight ("Min Height Value", Float) = 0.0
        _MaxHeight ("Max Height Value", Float) = 1.0

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
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_HeightMap);
            SAMPLER(sampler_HeightMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _LowColor;
                float4 _HighColor;
                float4 _HeightMap_ST;
                float _MinHeight;
                float _MaxHeight;
                float _RedThreshold;
                float _RedCurve;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv * _HeightMap_ST.xy + _HeightMap_ST.zw;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float heightSample = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, input.uv).r;

                float range = max(1e-5, _MaxHeight - _MinHeight);
                float normalizedHeight = saturate((heightSample - _MinHeight) / range);

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
