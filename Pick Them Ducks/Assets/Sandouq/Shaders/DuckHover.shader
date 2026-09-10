Shader "Sandouq/Duck Hover URP"
{
    Properties { _OutlineColor("Color", Color) = (1,1,0.85,1) _Width("Width", Float) = 0.018 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front ZWrite Off ZTest LEqual
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _OutlineColor;
            float _Width;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; };
            Varyings vert(Attributes input)
            {
                Varyings o;
                float3 p = TransformObjectToWorld(input.positionOS.xyz);
                p += TransformObjectToWorldNormal(input.normalOS) * _Width;
                o.positionCS = TransformWorldToHClip(p); return o;
            }
            half4 frag(Varyings input):SV_Target { return _OutlineColor; }
            ENDHLSL
        }
    }
}
