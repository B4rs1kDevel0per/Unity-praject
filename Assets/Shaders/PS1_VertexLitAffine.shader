Shader "PS1Style/VertexLitAffine"
{
    // Объектный шейдер под PS1-стиль:
    // 1) Vertex Snapping — "дрожащая" геометрия, как на консоли с целочисленным растеризатором.
    // 2) Affine texture mapping — текстуры "плывут" на плоскостях под углом (без перспективной коррекции UV).
    // Оба эффекта дают тот самый узнаваемый вайб PS1, и работают на ЛЮБОЙ геометрии,
    // из какого бы ассет-пака она ни была — это и есть "склеивающий" слой стиля.
    //
    // ВАЖНО про "плывущий" пол: аффинная ошибка UV растёт с РАЗМЕРОМ полигона. Если пол —
    // это один огромный Quad (2 треугольника на всю комнату), искажение будет экстремальным.
    // На PS1 пол всегда делали из МНОЖЕСТВА мелких полигонов (сетка, а не один плоский прямоугольник) —
    // делайте так же для больших плоскостей, плюс используйте слайдер Affine Strength ниже,
    // чтобы приглушить эффект именно на полу/потолке, оставив его сильным на мелких пропах.

    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        [Header(PS1 Vertex Snapping)]
        _GeometryResolution ("Snap grid resolution (например 160)", Range(16, 640)) = 160

        [Header(Affine Wobble)]
        _AffineMapping ("Сила аффинного плыва (0 = выкл, 1 = максимум)", Range(0, 1)) = 1

        [Header(SmartTiling)]
        [Toggle] _SmartTiling ("Включить (нужна БЕСШОВНАЯ текстура!)", Float) = 0
        _TileGridSize ("Размер клетки де-тайлинга (в повторах UV)", Range(1, 16)) = 4
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
            // noperspective (используется ниже для affine-эффекта) требует Shader Model 4.0+,
            // без явного target шейдер по умолчанию компилируется под старую модель и падает
            // с ошибкой компиляции (из-за чего пропадает из списка в выборе шейдера).
            #pragma target 4.5

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
                // Одни и те же UV передаются ДВУМЯ способами интерполяции:
                // uvCorrect — обычная перспективно-корректная развёртка (стандартное поведение GPU).
                // uvAffine  — "noperspective" отключает перспективную коррекцию для этого варьинга,
                //             интерполяция идёт линейно в экранном пространстве — так "плыли"
                //             текстуры на PS1. В фрагментном шейдере смешиваем их по _AffineMapping,
                //             получая управляемую СИЛУ эффекта, а не жёсткий вкл/выкл.
                float2 uvCorrect            : TEXCOORD0;
                noperspective float2 uvAffine : TEXCOORD1;
                float3 normalWS             : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;
            float _GeometryResolution;
            float _AffineMapping;
            float _SmartTiling;
            float _TileGridSize;

            // Простой хэш координат клетки -> псевдослучайные 0..1 значения.
            // Используем только для решения "зеркалить по X / зеркалить по Y" — дёшево,
            // без дополнительных сэмплов текстуры.
            float2 Hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }

            // Разбивает UV на клетки размера 1/_TileGridSize и случайно зеркалит содержимое
            // каждой клетки по X/Y. Работает БЕЗ ШВОВ только если исходная текстура бесшовная
            // (её противоположные края совпадают) — тогда любое зеркалирование целой клетки
            // по-прежнему стыкуется с соседями идеально ровно.
            float2 ApplySmartTiling(float2 uv, float gridSize)
            {
                float2 scaledUV = uv * gridSize;
                float2 cell = floor(scaledUV);
                float2 localUV = frac(scaledUV);

                float2 h = Hash2(cell);
                if (h.x > 0.5) localUV.x = 1.0 - localUV.x;
                if (h.y > 0.5) localUV.y = 1.0 - localUV.y;

                return (cell + localUV) / gridSize;
            }

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

                float2 uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.uvCorrect = uv;
                OUT.uvAffine = uv;

                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Смешиваем перспективно-корректные и "плывущие" UV по силе эффекта.
                // _AffineMapping = 0 -> обычная чёткая текстура, 1 -> полный PS1-вайб.
                float2 uv = lerp(IN.uvCorrect, IN.uvAffine, _AffineMapping);

                if (_SmartTiling > 0.5)
                    uv = ApplySmartTiling(uv, _TileGridSize);

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color;

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
