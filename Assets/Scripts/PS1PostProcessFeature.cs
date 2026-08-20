using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace PS1Style
{
    /// <summary>
    /// Renderer Feature, которая:
    /// 1) Заставляет URP рендерить кадр во ВНУТРЕННЕМ низком разрешении (RenderScale) —
    ///    это и даёт "пиксельный" вид, и резко снижает нагрузку на GPU (замена FSR не нужна,
    ///    т.к. апскейл на экран идёт Point-фильтром, без реконструкции резкости).
    /// 2) Применяет PS1_PostProcessDither.shader поверх готового кадра через Render Graph API
    ///    (актуальный пайплайн Unity 6 / URP 17+; старый Execute/OnCameraSetup здесь не используется —
    ///    он объявлен obsolete и не будет вызван, если в Project Settings -> Graphics -> Render Graph
    ///    не включён Compatibility Mode).
    ///
    /// Установка: Project Settings -> Graphics -> URP Asset -> Renderer Data -> Add Renderer Feature
    /// -> PS1PostProcessFeature.
    /// </summary>
    public class PS1PostProcessFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Материал с шейдером PS1Style/PostProcessDither")]
            public Material ditherMaterial;

            [Tooltip("Внутреннее разрешение рендера в процентах от экранного. " +
                     "0.25 = рендерим в 1/4 по каждой оси (1/16 пикселей) — сильный PS1-вайб и максимум FPS.")]
            [Range(0.1f, 1f)] public float renderScale = 0.35f;
        }

        public Settings settings = new Settings();
        private PS1DitherPass _pass;

        public override void Create()
        {
            _pass = new PS1DitherPass(settings)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.ditherMaterial == null) return;

            // Управляем внутренним разрешением рендера через встроенный механизм URP Render Scale —
            // это и есть "не-FSR" замена: рендерим меньше пикселей, а Blit на экран идёт с
            // Point-фильтрацией, сохраняя чёткие пиксельные блоки вместо мыла.
            renderingData.cameraData.renderScale = settings.renderScale;
            renderer.EnqueuePass(_pass);
        }

        /// <summary>
        /// Render Graph-версия прохода. Вместо CommandBuffer/Execute (устаревший путь)
        /// используется RecordRenderGraph + AddBlitPass — официальный способ Unity 6 (URP 17+)
        /// для полноэкранного блита с материалом.
        /// </summary>
        private class PS1DitherPass : ScriptableRenderPass
        {
            private readonly Material _material;

            public PS1DitherPass(Settings settings)
            {
                _material = settings.ditherMaterial;
                // Point-фильтрация на промежуточной текстуре — сохраняет резкие пиксельные блоки
                // при апскейле низкого внутреннего разрешения, вместо мыла.
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (_material == null) return;

                var resourceData = frameData.Get<UniversalResourceData>();

                // Не трогаем backbuffer напрямую (запрещено в Render Graph) — только цветовую текстуру камеры.
                if (resourceData.isActiveTargetBackBuffer) return;

                TextureHandle source = resourceData.activeColorTexture;

                var destinationDesc = renderGraph.GetTextureDesc(source);
                destinationDesc.name = "_PS1DitherTemp";
                destinationDesc.clearBuffer = false;
                destinationDesc.filterMode = FilterMode.Point;
                TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

                // Блит source -> destination с применением шейдера дизеринга (шейдер-пасс 0).
                RenderGraphUtils.BlitMaterialParameters blitParams = new(source, destination, _material, 0);
                renderGraph.AddBlitPass(blitParams, passName: "PS1 Post Process (Dither)");

                // Подменяем активную цветовую текстуру камеры результатом — последующие пассы
                // (например, UI Overlay) будут рисоваться поверх уже стилизованного кадра.
                resourceData.cameraColor = destination;
            }
        }
    }
}
