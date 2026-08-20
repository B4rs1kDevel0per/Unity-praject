using GameSettings.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Требует пакет com.unity.render-pipelines.universal (URP).
                                        // Если вы используете Built-in RP без URP — удалите/закомментируйте
                                        // блоки, помеченные [URP].

namespace GameSettings.Core
{
    /// <summary>
    /// Единственная зона ответственности этого класса — ПРИМЕНЕНИЕ данных GraphicSettingsData
    /// к реальным системам движка. Класс ничего не знает про UI и про сохранение файлов.
    /// Каждый Apply-метод атомарен, чтобы UI мог применять изменения "на лету" (live-preview)
    /// без пересборки всех настроек сразу.
    /// </summary>
    public class GraphicsApplier : MonoBehaviour
    {
        [Header("URP Post-Processing")]
        [Tooltip("Глобальный Volume со включённым 'Is Global', на профиле которого лежат Bloom / MotionBlur.")]
        [SerializeField] private Volume globalVolume;

        [Tooltip("Renderer Feature экранного AO (ScreenSpaceAmbientOcclusion) вашего URP Renderer Data. " +
                 "Перетащите сюда суб-ассет из Project window (разверните стрелку у Renderer Data).")]
        [SerializeField] private ScriptableRendererFeature ambientOcclusionFeature;

        [Tooltip("Камера, к которой применяется режим постпроцесс-AA (FXAA/SMAA/TAA). Обычно — Main Camera.")]
        [SerializeField] private Camera targetCamera;

        private void Reset()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        /// <summary>
        /// Позволяет заменить "живые" объекты сцены (камеру, Volume, AO-фичу) во время работы игры.
        /// Нужно, когда UI настроек вынесен в отдельную сцену (главное меню), а GraphicsApplier
        /// живёт постоянно через DontDestroyOnLoad — камера и Volume игровой сцены при каждой
        /// загрузке новой сцены создаются заново, и старые сериализованные ссылки становятся невалидными.
        /// Вызывайте этот метод из RuntimeGraphicsTargets.cs при старте каждой игровой сцены.
        /// </summary>
        public void SetRuntimeTargets(Camera camera, Volume volume, ScriptableRendererFeature aoFeature)
        {
            if (camera != null) targetCamera = camera;
            if (volume != null) globalVolume = volume;
            if (aoFeature != null) ambientOcclusionFeature = aoFeature;
        }

        // ---------------------------------------------------------------
        // ЭКРАН
        // ---------------------------------------------------------------

        public void ApplyScreen(GraphicSettingsData data)
        {
            FullScreenMode mode = ToUnityFullScreenMode(data.screenMode);

#if UNITY_2022_2_OR_NEWER
            var refreshRate = new RefreshRate
            {
                numerator = (uint)Mathf.Max(1, data.refreshRateNumerator),
                denominator = (uint)Mathf.Max(1, data.refreshRateDenominator)
            };
            Screen.SetResolution(data.resolutionWidth, data.resolutionHeight, mode, refreshRate);
#else
            Screen.SetResolution(data.resolutionWidth, data.resolutionHeight, mode, data.refreshRateNumerator);
#endif
        }

        public void ApplyVSync(GraphicSettingsData data)
        {
            QualitySettings.vSyncCount = data.vSyncEnabled ? 1 : 0;
            ApplyFpsLimit(data); // FPS-лимит имеет смысл пересчитать вместе с VSync
        }

        public void ApplyFpsLimit(GraphicSettingsData data)
        {
            if (data.vSyncEnabled)
            {
                // Когда VSync включён, Application.targetFrameRate игнорируется движком,
                // поэтому выставляем -1, чтобы не мешать VSync.
                Application.targetFrameRate = -1;
            }
            else
            {
                Application.targetFrameRate = data.fpsLimit > 0 ? data.fpsLimit : -1;
            }
        }

        private static FullScreenMode ToUnityFullScreenMode(ScreenModeOption option)
        {
            switch (option)
            {
                case ScreenModeOption.FullScreenExclusive: return FullScreenMode.ExclusiveFullScreen;
                case ScreenModeOption.FullScreenWindow: return FullScreenMode.FullScreenWindow;
                case ScreenModeOption.Windowed: return FullScreenMode.Windowed;
                default: return FullScreenMode.FullScreenWindow;
            }
        }

        // ---------------------------------------------------------------
        // ОБЩЕЕ КАЧЕСТВО / ТЕКСТУРЫ
        // ---------------------------------------------------------------

        public void ApplyQualityLevel(GraphicSettingsData data)
        {
            // applyExpensiveChanges = true — сразу применяет тяжёлые изменения (тени, LOD и т.д.)
            QualitySettings.SetQualityLevel(data.qualityLevelIndex, true);
        }

        public void ApplyTextureQuality(GraphicSettingsData data)
        {
#if UNITY_2022_2_OR_NEWER
            QualitySettings.globalTextureMipmapLimit = (int)data.textureQuality;
#else
            QualitySettings.masterTextureLimit = (int)data.textureQuality;
#endif
        }

        public void ApplyAnisotropicFiltering(GraphicSettingsData data)
        {
            switch (data.anisotropicLevel)
            {
                case AnisotropicLevelOption.Disabled:
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                    break;
                case AnisotropicLevelOption.PerTexture:
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                    break;
                case AnisotropicLevelOption.ForcedOn:
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    break;
            }
        }

        // ---------------------------------------------------------------
        // СГЛАЖИВАНИЕ
        // ---------------------------------------------------------------

        public void ApplyAntiAliasing(GraphicSettingsData data)
        {
            int msaaSamples = 1;
            AntialiasingMode postAaMode = AntialiasingMode.None;

            switch (data.antiAliasing)
            {
                case AntiAliasingOption.Off:
                    msaaSamples = 1;
                    postAaMode = AntialiasingMode.None;
                    break;
                case AntiAliasingOption.FXAA:
                    postAaMode = AntialiasingMode.FastApproximateAntialiasing;
                    break;
                case AntiAliasingOption.SMAA:
                    postAaMode = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    break;
                case AntiAliasingOption.TAA:
                    postAaMode = AntialiasingMode.TemporalAntiAliasing;
                    break;
                case AntiAliasingOption.MSAA2x:
                    msaaSamples = 2;
                    break;
                case AntiAliasingOption.MSAA4x:
                    msaaSamples = 4;
                    break;
                case AntiAliasingOption.MSAA8x:
                    msaaSamples = 8;
                    break;
            }

            // Built-in RP / общий QualitySettings MSAA:
            QualitySettings.antiAliasing = msaaSamples == 1 ? 0 : msaaSamples;

            // [URP] MSAA на самом Pipeline Asset:
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                urpAsset.msaaSampleCount = msaaSamples;
            }

            // [URP] Постпроцесс-AA (FXAA/SMAA/TAA) выставляется per-camera:
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (targetCamera != null)
            {
                var camData = targetCamera.GetUniversalAdditionalCameraData();
                if (camData != null)
                {
                    camData.antialiasing = postAaMode;
                    if (postAaMode == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                        camData.antialiasingQuality = AntialiasingQuality.High;
                }
            }
        }

        // ---------------------------------------------------------------
        // ТЕНИ
        // ---------------------------------------------------------------

        public void ApplyShadows(GraphicSettingsData data)
        {
            // Built-in RP. Явно указываем UnityEngine.ShadowQuality — URP имеет собственный
            // одноимённый тип UnityEngine.Rendering.Universal.ShadowQuality, отсюда CS0104.
            QualitySettings.shadows = data.shadowsEnabled ? UnityEngine.ShadowQuality.All : UnityEngine.ShadowQuality.Disable;
            // Явно указываем UnityEngine.ShadowResolution — этот тип одноимённо конфликтует
            // с UnityEngine.Rendering.Universal.ShadowResolution (URP), отсюда и CS0104.
            QualitySettings.shadowResolution = ToUnityShadowResolution(data.shadowResolution);
            QualitySettings.shadowDistance = data.shadowDistance;
            QualitySettings.shadowCascades = (int)data.shadowCascades;

            // [URP] Дублируем значения на Pipeline Asset:
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                urpAsset.shadowDistance = data.shadowDistance;

                // Примечание: публичный сеттер cascadeCount доступен начиная с URP 12+ (Unity 2021.2+).
                // Если у вас более старая версия URP — этот параметр настраивается только
                // через инспектор самого UniversalRenderPipelineAsset, уберите строку ниже.
                int cascadeCount = data.shadowCascades == ShadowCascadeCountOption.NoCascades ? 1 :
                                    data.shadowCascades == ShadowCascadeCountOption.TwoCascades ? 2 : 4;
                urpAsset.shadowCascadeCount = cascadeCount;
            }
        }

        // Возвращаемый тип и все значения явно квалифицированы как UnityEngine.ShadowResolution,
        // чтобы не конфликтовать с UnityEngine.Rendering.Universal.ShadowResolution (URP).
        private static UnityEngine.ShadowResolution ToUnityShadowResolution(ShadowQualityOption option)
        {
            switch (option)
            {
                case ShadowQualityOption.Low: return UnityEngine.ShadowResolution.Low;
                case ShadowQualityOption.Medium: return UnityEngine.ShadowResolution.Medium;
                case ShadowQualityOption.High: return UnityEngine.ShadowResolution.High;
                case ShadowQualityOption.VeryHigh: return UnityEngine.ShadowResolution.VeryHigh;
                default: return UnityEngine.ShadowResolution.Medium;
            }
        }

        // ---------------------------------------------------------------
        // LOD
        // ---------------------------------------------------------------

        public void ApplyLodBias(GraphicSettingsData data)
        {
            QualitySettings.lodBias = data.lodBias;
        }

        // ---------------------------------------------------------------
        // ПОСТОБРАБОТКА (URP Volume)
        // ---------------------------------------------------------------

        public void ApplyPostProcessing(GraphicSettingsData data)
        {
            if (globalVolume == null || globalVolume.profile == null)
            {
                Debug.LogWarning("[GraphicsApplier] Global Volume не назначен — постобработка не применена.");
                return;
            }

            var profile = globalVolume.profile;

            if (profile.TryGet<Bloom>(out var bloom))
            {
                bloom.active = data.bloomEnabled;
                bloom.intensity.value = data.bloomIntensity;
            }

            if (profile.TryGet<MotionBlur>(out var motionBlur))
            {
                motionBlur.active = data.motionBlurEnabled;
                motionBlur.intensity.value = data.motionBlurIntensity;
            }

            // AO в URP обычно реализован как Renderer Feature (ScreenSpaceAmbientOcclusion),
            // а не Volume Override, поэтому управляем через SetActive() отдельного объекта.
            if (ambientOcclusionFeature != null)
            {
                ambientOcclusionFeature.SetActive(data.ambientOcclusionEnabled);
            }

            // Если в вашем проекте AO всё же реализован как VolumeComponent (кастомный),
            // можно дополнительно проставить интенсивность через profile.TryGet<YourAOComponent>(...).
        }

        // ---------------------------------------------------------------
        // ПРИМЕНИТЬ ВСЁ СРАЗУ (используется при старте игры)
        // ---------------------------------------------------------------

        public void ApplyAll(GraphicSettingsData data)
        {
            ApplyQualityLevel(data);
            ApplyScreen(data);
            ApplyVSync(data);
            ApplyTextureQuality(data);
            ApplyAnisotropicFiltering(data);
            ApplyAntiAliasing(data);
            ApplyShadows(data);
            ApplyLodBias(data);
            ApplyPostProcessing(data);
        }
    }
}
