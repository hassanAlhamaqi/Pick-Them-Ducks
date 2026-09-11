Shader "Sandouq/Park Water"
{
    Properties { _BaseColor("Water",Color)=(0.07,0.4,0.5,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; };
            Varyings Vert(Attributes v) { Varyings o; o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);return o; }
            half4 Frag(Varyings i):SV_Target
            {
                float2 p=i.positionWS.xz;
                float wave=sin(p.x*1.4+p.y*.8+_Time.y*.8)*sin(p.y*1.9-p.x*.2-_Time.y*.5);
                float glint=pow(saturate(wave),18)*.22;
                float bands=sin(p.x*.09+p.y*.14)*.035;
                half3 color=_BaseColor.rgb+bands+glint;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
