// Halo rúnico translúcido: glow dorado aditivo con borde atenuado.
// Sustituye la esfera sólida opaca de los objetos interactuables: el centro del
// volumen queda casi transparente y solo el contorno brilla, de modo que el halo
// resalta el objeto sin encerrarlo en una masa sólida.
Shader "Custom/HaloGlowAdditive"
{
    Properties
    {
        _Color ("Color del halo", Color) = (1, 0.85, 0.35, 1)
        _Intensity ("Intensidad", Float) = 1.6
        _RimPower ("Dureza del borde", Range(0.5, 8)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "HaloGlow"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _RimPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionCS = pos.positionCS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS  = GetWorldSpaceViewDir(pos.positionWS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                float3 v = normalize(IN.viewDirWS);

                // Fresnel: 0 en el centro del volumen, 1 en el contorno.
                float rim = 1.0 - saturate(dot(n, v));
                float glow = pow(rim, _RimPower);

                float3 color = _Color.rgb * _Intensity * glow;
                return half4(color, glow);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
