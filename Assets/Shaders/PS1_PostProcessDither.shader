Shader "PS1Style/PostProcessDither"
{
    // Финальный "склеивающий" фильтр. Накладывается на ВЕСЬ кадр целиком, независимо от того,
    // из какого пака взята геометрия/текстуры — режет палитру и добавляет узорный дизеринг,
    // из-за чего разнородные ассеты визуально усредняются в один стиль.

    Properties
    {
        // Именно "_MainTex" здесь НЕ используется движком — источник кадра приходит через
        // _BlitTexture (см. ниже), это поле оставлено только для наглядности в инспекторе,
        // трогать/назначать туда ничего не нужно.
        _ColorLevels ("Уровней цвета на канал (напр. 16)", Range(2, 64)) = 16
        _DitherStrength ("Сила дизеринга", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // _BlitTexture — стандартное имя, под которым RenderGraphUtils.AddBlitPass
            // передаёт исходный кадр камеры в материал. Использование любого другого имени
            // (например _MainTex) приводит к тому, что шейдер никогда не получает реальную
            // картинку и всегда сэмплирует дефолтную заглушку (белую/чёрную текстуру) — именно
            // это давало сплошной белый экран.
            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_PointClamp);
            float _ColorLevels;
            float _DitherStrength;

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                // Полноэкранный треугольник без меша (стандартный трюк URP Blit).
                OUT.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                OUT.uv = GetFullScreenTriangleTexCoord(IN.vertexID);
                return OUT;
            }

            // Классическая матрица Байера 4x4 — задаёт узор порогов для дизеринга.
            static const float Bayer4x4[16] =
            {
                 0,  8,  2, 10,
                12,  4, 14,  6,
                 3, 11,  1,  9,
                15,  7, 13,  5
            };

            float GetBayerThreshold(float2 screenPos)
            {
                uint2 p = uint2(screenPos.x, screenPos.y) % 4;
                return Bayer4x4[p.y * 4 + p.x] / 16.0 - 0.5; // диапазон [-0.5, 0.5)
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                half3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, IN.uv).rgb;

                float threshold = GetBayerThreshold(IN.positionCS.xy) * _DitherStrength;

                // Квантуем каждый канал до _ColorLevels ступеней, подмешивая байеровский порог
                // ПЕРЕД округлением — так дизеринг "размывает" резкие ступени градиента в узор точек,
                // вместо банд-полос (типичный артефакт простого урезания палитры без дизера).
                col = col + threshold / _ColorLevels;
                col = floor(col * _ColorLevels + 0.5) / _ColorLevels;

                return half4(saturate(col), 1);
            }
            ENDHLSL
        }
    }
}
