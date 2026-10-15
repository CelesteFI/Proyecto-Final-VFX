Shader "Custom/DissolveSurface"
{
    Properties
    {
        _BaseColor ("Color Base", Color) = (0.25, 0.15, 0.1, 1)
        _EdgeColor ("Color Borde Maná", Color) = (0.3, 1.0, 0.5, 1)
        _DissolveAmount ("Cantidad de Disolucion", Range(0, 1)) = 0.0
        _EdgeWidth ("Grosor Borde", Range(0, 0.2)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 worldPos : TEXCOORD1;
        };

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _EdgeColor;
            float _DissolveAmount;
            float _EdgeWidth;
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

            return _BaseColor;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}