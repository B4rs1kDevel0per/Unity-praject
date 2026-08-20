Shader "PS1Style/VertexLitAffine"
{
    // Объектный шейдер под PS1-стиль:
    // 1) Vertex Snapping — "дрожащая" геометрия, как на консоли с целочисленным растеризатором.
    // 2) Affine texture mapping — текстуры "плывут" на плоскостях под углом (без перспективной коррекции UV).
    // Оба эффекта дают тот самый узнаваемый вайб PS1, и работают на ЛЮБОЙ геометрии,
    // из какого бы ассет-пака она ни была — это и есть "склеивающий" слой стиля.

    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        [Header(PS1 Vertex Snapping)]
        _GeometryResolution ("Snap grid resolution (например 160)", Range(16, 640)) = 160

        [Header(Affine Wobble)]
        [Toggle] _AffineMapping ("Включить аффинные текстуры", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                // "noperspective" — ключевое слово, которое отключает перспективную коррекцию
                // интерполяции. Именно так на PS1 "плыли" текстуры на полу и стенах под углом.
                noperspective float2 uv : TEXCOORD0;
                float3 normalWS  : TEXCOORD1;
                float3 positionWS: TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;
            float _GeometryResolution;
            float _AffineMapping;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float4 positionCS = TransformWorldToHClip(positionWS);

                // --- Vertex Snapping ---
                // Переводим clip-space в NDC (-1..1), "прилипаем" к сетке заданного разрешения,
                // возвращаем обратно. Чем меньше _GeometryResolution — тем сильнее трясётся геометрия.
                float4 snapped = positionCS;
                float2 grid = _GeometryResolution.xx;
                snapped.xy = snapped.xy / snapped.w;               // -> NDC
                snapped.xy = floor(snapped.xy * grid) / grid;       // -> прилипание к сетке
                snapped.xy = snapped.xy * snapped.w;                // -> обратно в clip space

                OUT.positionCS = snapped;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS = positionWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

                // Простое ламбертовское освещение по основному источнику — большего PS1 и не знала.
                Light mainLight = GetMainLight();
                float ndotl = saturate(dot(normalize(IN.normalWS), mainLight.direction));
                half3 lighting = mainLight.color * ndotl + unity_AmbientSky.rgb;

                return half4(tex.rgb * lighting, tex.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
