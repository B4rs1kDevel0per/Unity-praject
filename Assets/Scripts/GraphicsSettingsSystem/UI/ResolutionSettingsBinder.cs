using System.Collections.Generic;
using System.Linq;
using GameSettings.Core;
using UnityEngine;
using UnityEngine.UI;

// Если в проекте используется TextMeshPro-версия Dropdown, замените
// "UnityEngine.UI.Dropdown" на "TMPro.TMP_Dropdown" (using TMPro;) — API идентично.

namespace GameSettings.UI
{
    /// <summary>
    /// Связывает Dropdown "Разрешение" и Dropdown "Частота обновления" с SettingsManager.
    /// Разрешения берутся из Screen.resolutions (список, который реально поддерживает монитор).
    /// ВАЖНО: используется Resolution.refreshRateRatio — доступно с Unity 2022.2+.
    /// На более старых версиях замените на устаревшее поле Resolution.refreshRate (int),
    /// а denominator всегда считайте равным 1.
    /// </summary>
    public class ResolutionSettingsBinder : MonoBehaviour
    {
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Dropdown refreshRateDropdown;

        // Уникальные разрешения (без дублей по частоте обновления)
        private List<(int width, int height)> _resolutions;
        // Доступные частоты обновления для текущего выбранного разрешения
        private List<Resolution> _refreshRatesForCurrentResolution;

        private void OnEnable()
        {
            BuildResolutionList();
            PopulateResolutionDropdown();
            SyncFromCurrent();

            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            refreshRateDropdown.onValueChanged.AddListener(OnRefreshRateChanged);
            SettingsManager.Instance.OnSettingsChanged += HandleExternalSettingsChanged;
        }

        private void OnDisable()
        {
            resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            refreshRateDropdown.onValueChanged.RemoveListener(OnRefreshRateChanged);
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnSettingsChanged -= HandleExternalSettingsChanged;
        }

        private void BuildResolutionList()
        {
            _resolutions = Screen.resolutions
                .Select(r => (r.width, r.height))
                .Distinct()
                .OrderBy(r => r.width * r.height)
                .ToList();
        }

        private void PopulateResolutionDropdown()
        {
            var labels = _resolutions.Select(r => $"{r.width} x {r.height}").ToArray();
            EnumDropdownHelper.Fill(resolutionDropdown, labels);
        }

        private void PopulateRefreshRatesFor(int width, int height)
        {
            _refreshRatesForCurrentResolution = Screen.resolutions
                .Where(r => r.width == width && r.height == height)
                .OrderBy(r => r.refreshRateRatio.value)
                .ToList();

            if (_refreshRatesForCurrentResolution.Count == 0)
            {
                // Фоллбэк — если по какой-то причине список пуст, используем текущий экран
                _refreshRatesForCurrentResolution = new List<Resolution> { Screen.currentResolution };
            }

            var labels = _refreshRatesForCurrentResolution
                .Select(r => $"{r.refreshRateRatio.value:0} Hz")
                .ToArray();

            EnumDropdownHelper.Fill(refreshRateDropdown, labels);
        }

        private void SyncFromCurrent()
        {
            var data = SettingsManager.Instance.Current;

            int resIndex = _resolutions.FindIndex(r => r.width == data.resolutionWidth && r.height == data.resolutionHeight);
            if (resIndex < 0) resIndex = 0;

            resolutionDropdown.SetValueWithoutNotify(resIndex);
            PopulateRefreshRatesFor(_resolutions[resIndex].width, _resolutions[resIndex].height);

            int rateIndex = _refreshRatesForCurrentResolution.FindIndex(r =>
                Mathf.Approximately((float)r.refreshRateRatio.value, (float)data.refreshRateNumerator / data.refreshRateDenominator));
            if (rateIndex < 0) rateIndex = 0;

            refreshRateDropdown.SetValueWithoutNotify(rateIndex);
        }

        private void OnResolutionChanged(int index)
        {
            var (width, height) = _resolutions[index];
            PopulateRefreshRatesFor(width, height);
            refreshRateDropdown.SetValueWithoutNotify(0);

            SettingsManager.Instance.SetResolution(width, height);
            ApplySelectedRefreshRate(0);
        }

        private void OnRefreshRateChanged(int index)
        {
            ApplySelectedRefreshRate(index);
        }

        private void ApplySelectedRefreshRate(int index)
        {
            if (_refreshRatesForCurrentResolution == null || _refreshRatesForCurrentResolution.Count == 0)
                return;

            var res = _refreshRatesForCurrentResolution[index];
            SettingsManager.Instance.SetRefreshRate((int)res.refreshRateRatio.numerator, (int)res.refreshRateRatio.denominator);
        }

        private void HandleExternalSettingsChanged(GameSettings.Data.GraphicSettingsData data)
        {
            SyncFromCurrent();
        }
    }
}
