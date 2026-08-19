using System;
using UnityEngine;

namespace GameSettings.Data
{
    /// <summary>
    /// Режим экрана.
    /// </summary>
    public enum ScreenModeOption
    {
        FullScreenExclusive = 0, // Полноэкранный эксклюзивный
        FullScreenWindow    = 1, // Полноэкранное окно (borderless)
        Windowed            = 2  // Оконный режим
    }

    /// <summary>
    /// Режим сглаживания. Off/FXAA/SMAA/TAA — постпроцесс-сглаживание (URP Camera Data),
    /// MSAA2x/4x/8x — аппаратное мультисэмплирование (QualitySettings.antiAliasing).
    /// </summary>
    public enum AntiAliasingOption
    {
        Off    = 0,
        FXAA   = 1,
        SMAA   = 2,
        TAA    = 3,
        MSAA2x = 4,
        MSAA4x = 5,
        MSAA8x = 6
    }

    /// <summary>
    /// Качество (разрешение) текстур. Соответствует mip-уровню, который "отрезается".
    /// </summary>
    public enum TextureQualityOption
    {
        Full    = 0, // Полное разрешение
        Half    = 1, // 1/2
        Quarter = 2, // 1/4
        Eighth  = 3  // 1/8
    }

    /// <summary>
    /// Уровень анизотропной фильтрации.
    /// </summary>
    public enum AnisotropicLevelOption
    {
        Disabled    = 0,
        PerTexture  = 1, // Enable — управляется настройками самой текстуры
        ForcedOn    = 2  // ForceEnable — принудительно максимум
    }

    /// <summary>
    /// Качество (разрешение) карт теней.
    /// </summary>
    public enum ShadowQualityOption
    {
        Low      = 0,
        Medium   = 1,
        High     = 2,
        VeryHigh = 3
    }

    /// <summary>
    /// Количество каскадов теней.
    /// </summary>
    public enum ShadowCascadeCountOption
    {
        NoCascades  = 0,
        TwoCascades = 2,
        FourCascades = 4
    }

    /// <summary>
    /// Единая структура данных всех графических настроек.
    /// Не содержит никакой логики применения — только данные (Single Responsibility).
    /// Используется JsonUtility, поэтому все поля должны быть публичными полями (не свойствами).
    /// </summary>
    [Serializable]
    public class GraphicSettingsData
    {
        [Header("Экран")]
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;
        public int refreshRateNumerator = 60;
        public int refreshRateDenominator = 1;
        public ScreenModeOption screenMode = ScreenModeOption.FullScreenWindow;
        public bool vSyncEnabled = true;
        public int fpsLimit = 0; // 0 = без ограничения

        [Header("Общее качество")]
        public int qualityLevelIndex = 2; // Индекс уровня в Project Settings > Quality (базовая пресет-точка)

        [Header("Текстуры")]
        public TextureQualityOption textureQuality = TextureQualityOption.Full;
        public AnisotropicLevelOption anisotropicLevel = AnisotropicLevelOption.ForcedOn;

        [Header("Сглаживание")]
        public AntiAliasingOption antiAliasing = AntiAliasingOption.TAA;

        [Header("Тени")]
        public bool shadowsEnabled = true;
        public ShadowQualityOption shadowResolution = ShadowQualityOption.Medium;
        public float shadowDistance = 75f;
        public ShadowCascadeCountOption shadowCascades = ShadowCascadeCountOption.FourCascades;

        [Header("Детализация моделей")]
        public float lodBias = 1.0f;

        [Header("Постобработка (URP Volume)")]
        public bool bloomEnabled = true;
        [Range(0f, 5f)] public float bloomIntensity = 1.0f;
        public bool ambientOcclusionEnabled = true;
        [Range(0f, 1f)] public float ambientOcclusionIntensity = 1.0f;
        public bool motionBlurEnabled = false;
        [Range(0f, 1f)] public float motionBlurIntensity = 0.5f;

        /// <summary>
        /// Глубокая копия объекта — используется, чтобы UI мог "отменить" изменения (Cancel),
        /// не трогая уже применённый и сохранённый набор данных.
        /// </summary>
        public GraphicSettingsData Clone()
        {
            return (GraphicSettingsData)MemberwiseClone();
        }

        /// <summary>
        /// Значения "по умолчанию" — на основе текущего экрана устройства.
        /// </summary>
        public static GraphicSettingsData CreateDefault()
        {
            var current = Screen.currentResolution;
            return new GraphicSettingsData
            {
                resolutionWidth = current.width,
                resolutionHeight = current.height,
#if UNITY_2022_2_OR_NEWER
                refreshRateNumerator = (int)current.refreshRateRatio.numerator,
                refreshRateDenominator = (int)Mathf.Max(1, current.refreshRateRatio.denominator),
#else
                refreshRateNumerator = current.refreshRate,
                refreshRateDenominator = 1,
#endif
                screenMode = ScreenModeOption.FullScreenWindow,
                vSyncEnabled = true,
                fpsLimit = 0,
                qualityLevelIndex = 2,
                textureQuality = TextureQualityOption.Full,
                anisotropicLevel = AnisotropicLevelOption.ForcedOn,
                antiAliasing = AntiAliasingOption.TAA,
                shadowsEnabled = true,
                shadowResolution = ShadowQualityOption.Medium,
                shadowDistance = 75f,
                shadowCascades = ShadowCascadeCountOption.FourCascades,
                lodBias = 1.0f,
                bloomEnabled = true,
                bloomIntensity = 1.0f,
                ambientOcclusionEnabled = true,
                ambientOcclusionIntensity = 1.0f,
                motionBlurEnabled = false,
                motionBlurIntensity = 0.5f
            };
        }
    }
}
