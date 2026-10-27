Shader "Custom/DissolveSurface"
{
    Properties
    {
        _BaseColor ("Color Base", Color) = (0.25, 0.15, 0.1, 1)
        _EdgeColor ("Color Borde Maná", Color) = (0.3, 1.0, 0.5, 1)
        _DissolveAmount ("Cantidad de Disolucion", Range(0, 1)) = 0.0
        _EdgeWidth ("Grosor Borde", Range(0, 0.2)) = 0.05
        _HitFlash ("Destello de Impacto", Range(0, 1)) = 0.0
        _Smoothness ("Suavizado", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float2 uv         : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
            float3 worldPos   : TEXCOORD1;
            float3 normalWS   : TEXCOORD2;
        };

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _EdgeColor;
            float _DissolveAmount;
            float _EdgeWidth;
            float _HitFlash;
            float _Smoothness;
        CBUFFER_END

        // Generador procedural de ruido básico sin texturas pesadas
        float Noise(float3 p)
        {
            return frac(sin(dot(p, float3(12.9898, 78.233, 45.164))) * 43758.5453);
        }

        Varyings vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = input.uv;
            output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            return output;
        }

        half4 frag(Varyings input) : SV_Target
        {
            float n = frac(sin(dot(floor(input.worldPos * 4.0), float3(12.9898, 78.233, 45.543))) * 43758.5453);

            // Descartar fragmentos según el valor de disolución
            if (n < _DissolveAmount)
                discard;

            // Borde incandescente
            if (n < _DissolveAmount + _EdgeWidth)
                return _EdgeColor * 2.0;

            float3 normalWS = normalize(input.normalWS);

            // Iluminación directa simplificada (equivalente de bloqueo al Lit de URP):
            // evita que el cambio de material aplaste el volumen del enemigo.
            Light luzPrincipal = GetMainLight();
            float ndl = saturate(dot(normalWS, luzPrincipal.direction));
            half3 ambiente = SampleSH(normalWS);

            half3 albedo = _BaseColor.rgb;
            half3 color = albedo * (luzPrincipal.color * ndl + ambiente);

            // Destello blanco de impacto (DamageFlash escribe _HitFlash = 1.0)
            color = lerp(color, half3(1.0, 1.0, 1.0), saturate(_HitFlash));

            return half4(color, 1.0);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            float4 ShadowVert(Attributes input) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif

                return positionCS;
            }

            half4 ShadowFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
