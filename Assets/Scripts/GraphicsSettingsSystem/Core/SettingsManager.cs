using System;
using GameSettings.Data;
using GameSettings.Storage;
using UnityEngine;

namespace GameSettings.Core
{
    /// <summary>
    /// Центральный узел системы (Singleton). UI НИКОГДА не обращается к QualitySettings/Screen
    /// и не пишет файлы напрямую — вся коммуникация идёт через методы этого класса.
    /// Паттерн работы: каждое изменение UI сразу применяется (live-preview) через GraphicsApplier,
    /// а физическая запись на диск происходит только по кнопке "Применить/Сохранить".
    /// Кнопка "Отмена" откатывает несохранённые изменения, кнопка "Сброс" возвращает заводские значения.
    /// </summary>
    [DisallowMultipleComponent]
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [Header("Ссылки")]
        [SerializeField] private GraphicsApplier applier;

        [Header("Поведение")]
        [Tooltip("Если true — при первом запуске игры используется JSON-файл, иначе PlayerPrefs.")]
        [SerializeField] private bool useJsonStorage = true;

        /// <summary>Текущие (уже применённые к движку) настройки.</summary>
        public GraphicSettingsData Current { get; private set; }

        /// <summary>Последние сохранённые на диск настройки — точка отката для "Отмена".</summary>
        private GraphicSettingsData _savedSnapshot;

        private ISettingsStorage _storage;

        /// <summary>Событие для UI: настройки изменились (после Apply-операции, включая Cancel/Reset).</summary>
        public event Action<GraphicSettingsData> OnSettingsChanged;

        /// <summary>Событие: настройки были физически сохранены на диск.</summary>
        public event Action OnSettingsSaved;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _storage = useJsonStorage ? (ISettingsStorage)new JsonSettingsStorage() : new PlayerPrefsSettingsStorage();

            if (applier == null)
                applier = GetComponent<GraphicsApplier>();

            LoadOrCreateDefaults();
        }

        private void LoadOrCreateDefaults()
        {
            var loaded = _storage.Exists() ? _storage.Load() : null;
            Current = loaded ?? GraphicSettingsData.CreateDefault();
            _savedSnapshot = Current.Clone();

            applier.ApplyAll(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        // -----------------------------------------------------------
        // ОБЩЕЕ УПРАВЛЕНИЕ
        // -----------------------------------------------------------

        /// <summary>Сохранить текущее состояние на диск.</summary>
        public void SaveSettings()
        {
            _storage.Save(Current);
            _savedSnapshot = Current.Clone();
            OnSettingsSaved?.Invoke();
        }

        /// <summary>Откатить несохранённые изменения к последнему сохранённому состоянию.</summary>
        public void CancelChanges()
        {
            Current = _savedSnapshot.Clone();
            applier.ApplyAll(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        /// <summary>Сбросить настройки к заводским значениям (не сохраняет автоматически).</summary>
        public void ResetToDefaults()
        {
            Current = GraphicSettingsData.CreateDefault();
            applier.ApplyAll(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        /// <summary>
        /// Регистрирует "живые" объекты новой игровой сцены (камеру, Volume, AO-фичу) и сразу
        /// заново применяет к ним текущие настройки. Вызывается из RuntimeGraphicsTargets.cs
        /// при старте каждой игровой сцены — актуально, когда UI настроек вынесен в отдельную сцену.
        /// </summary>
        public void RegisterRuntimeTargets(Camera camera, UnityEngine.Rendering.Volume volume,
            UnityEngine.Rendering.Universal.ScriptableRendererFeature aoFeature)
        {
            applier.SetRuntimeTargets(camera, volume, aoFeature);
            applier.ApplyAll(Current);
        }

        // -----------------------------------------------------------
        // ЭКРАН
        // -----------------------------------------------------------

        public void SetResolution(int width, int height)
        {
            Current.resolutionWidth = width;
            Current.resolutionHeight = height;
            applier.ApplyScreen(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        public void SetRefreshRate(int numerator, int denominator)
        {
            Current.refreshRateNumerator = numerator;
            Current.refreshRateDenominator = denominator;
            applier.ApplyScreen(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        public void SetScreenMode(ScreenModeOption mode)
        {
            Current.screenMode = mode;
            applier.ApplyScreen(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        public void SetVSync(bool enabled)
        {
            Current.vSyncEnabled = enabled;
            applier.ApplyVSync(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        public void SetFpsLimit(int fps)
        {
            Current.fpsLimit = fps;
            applier.ApplyFpsLimit(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        // -----------------------------------------------------------
        // КАЧЕСТВО / ТЕКСТУРЫ
        // -----------------------------------------------------------

        public void SetQualityLevel(int index)
        {
            Current.qualityLevelIndex = index;
            applier.ApplyQualityLevel(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        public void SetTextureQuality(TextureQualityOption quality)
        {
            Current.textureQuality = quality;
            applier.ApplyTextureQuality(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        public void SetAnisotropicLevel(AnisotropicLevelOption level)
        {
            Current.anisotropicLevel = level;
            applier.ApplyAnisotropicFiltering(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        // -----------------------------------------------------------
        // СГЛАЖИВАНИЕ
        // -----------------------------------------------------------

        public void SetAntiAliasing(AntiAliasingOption option)
        {
            Current.antiAliasing = option;
            applier.ApplyAntiAliasing(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        // -----------------------------------------------------------
        // ТЕНИ
        // -----------------------------------------------------------

        public void SetShadowsEnabled(bool enabled)
        {
            Current.shadowsEnabled = enabled;
            applier.ApplyShadows(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        public void SetShadowResolution(ShadowQualityOption quality)
        {
            Current.shadowResolution = quality;
            applier.ApplyShadows(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        public void SetShadowDistance(float distance)
        {
            Current.shadowDistance = distance;
            applier.ApplyShadows(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        public void SetShadowCascades(ShadowCascadeCountOption cascades)
        {
            Current.shadowCascades = cascades;
            applier.ApplyShadows(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        // -----------------------------------------------------------
        // LOD
        // -----------------------------------------------------------

        public void SetLodBias(float bias)
        {
            Current.lodBias = bias;
            applier.ApplyLodBias(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        // -----------------------------------------------------------
        // ПОСТОБРАБОТКА
        // -----------------------------------------------------------

        public void SetBloom(bool enabled, float intensity)
        {
            Current.bloomEnabled = enabled;
            Current.bloomIntensity = intensity;
            applier.ApplyPostProcessing(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        public void SetAmbientOcclusion(bool enabled, float intensity)
        {
            Current.ambientOcclusionEnabled = enabled;
            Current.ambientOcclusionIntensity = intensity;
            applier.ApplyPostProcessing(Current);
            OnSettingsChanged?.Invoke(Current);
        }

        public void SetMotionBlur(bool enabled, float intensity)
        {
            Current.motionBlurEnabled = enabled;
            Current.motionBlurIntensity = intensity;
            applier.ApplyPostProcessing(Current);
            OnSettingsChanged?.Invoke(Current);
        }
    }
}
