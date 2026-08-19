using GameSettings.Core;
using GameSettings.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GameSettings.UI
{
    /// <summary>
    /// Единый биндер для всей группы "Тени": Toggle вкл/выкл, Dropdown разрешения,
    /// Slider дистанции прорисовки, Dropdown количества каскадов.
    /// Объединено в один класс, т.к. UI-элементы логически образуют одну секцию
    /// и часто должны блокироваться вместе (если тени выключены — остальные поля неактивны).
    /// </summary>
    public class ShadowsSettingsBinder : MonoBehaviour
    {
        [Header("Вкл/выкл")]
        [SerializeField] private Toggle shadowsEnabledToggle;

        [Header("Разрешение теней")]
        [SerializeField] private Dropdown shadowResolutionDropdown;
        private static readonly string[] ResolutionLabels = { "Низкое", "Среднее", "Высокое", "Очень высокое" };

        [Header("Дистанция прорисовки")]
        [SerializeField] private Slider shadowDistanceSlider; // рекомендуемый диапазон 10..300
        [SerializeField] private Text shadowDistanceValueLabel; // необязательно, для отображения числа

        [Header("Каскады")]
        [SerializeField] private Dropdown shadowCascadesDropdown;
        private static readonly string[] CascadeLabels = { "Без каскадов", "2 каскада", "4 каскада" };
        private static readonly int[] CascadeValues = { 0, 2, 4 };

        [Header("Группа, которую нужно блокировать при выключенных тенях")]
        [SerializeField] private CanvasGroup dependentControlsGroup;

        private void OnEnable()
        {
            EnumDropdownHelper.Fill(shadowResolutionDropdown, ResolutionLabels);
            EnumDropdownHelper.Fill(shadowCascadesDropdown, CascadeLabels);

            Sync(SettingsManager.Instance.Current);

            shadowsEnabledToggle.onValueChanged.AddListener(OnShadowsEnabledChanged);
            shadowResolutionDropdown.onValueChanged.AddListener(OnShadowResolutionChanged);
            shadowDistanceSlider.onValueChanged.AddListener(OnShadowDistanceChanged);
            shadowCascadesDropdown.onValueChanged.AddListener(OnShadowCascadesChanged);
            SettingsManager.Instance.OnSettingsChanged += Sync;
        }

        private void OnDisable()
        {
            shadowsEnabledToggle.onValueChanged.RemoveListener(OnShadowsEnabledChanged);
            shadowResolutionDropdown.onValueChanged.RemoveListener(OnShadowResolutionChanged);
            shadowDistanceSlider.onValueChanged.RemoveListener(OnShadowDistanceChanged);
            shadowCascadesDropdown.onValueChanged.RemoveListener(OnShadowCascadesChanged);
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnSettingsChanged -= Sync;
        }

        private void OnShadowsEnabledChanged(bool value)
        {
            SettingsManager.Instance.SetShadowsEnabled(value);
            UpdateDependentControlsState(value);
        }

        private void OnShadowResolutionChanged(int index)
        {
            SettingsManager.Instance.SetShadowResolution((ShadowQualityOption)index);
        }

        private void OnShadowDistanceChanged(float value)
        {
            SettingsManager.Instance.SetShadowDistance(value);
            if (shadowDistanceValueLabel != null)
                shadowDistanceValueLabel.text = value.ToString("0") + " м";
        }

        private void OnShadowCascadesChanged(int index)
        {
            SettingsManager.Instance.SetShadowCascades((ShadowCascadeCountOption)CascadeValues[index]);
        }

        private void Sync(GraphicSettingsData data)
        {
            shadowsEnabledToggle.SetIsOnWithoutNotify(data.shadowsEnabled);
            shadowResolutionDropdown.SetValueWithoutNotify((int)data.shadowResolution);
            shadowDistanceSlider.SetValueWithoutNotify(data.shadowDistance);
            if (shadowDistanceValueLabel != null)
                shadowDistanceValueLabel.text = data.shadowDistance.ToString("0") + " м";

            int cascadeIndex = System.Array.IndexOf(CascadeValues, (int)data.shadowCascades);
            shadowCascadesDropdown.SetValueWithoutNotify(cascadeIndex < 0 ? 2 : cascadeIndex);

            UpdateDependentControlsState(data.shadowsEnabled);
        }

        private void UpdateDependentControlsState(bool shadowsEnabled)
        {
            if (dependentControlsGroup == null) return;
            dependentControlsGroup.interactable = shadowsEnabled;
            dependentControlsGroup.alpha = shadowsEnabled ? 1f : 0.5f;
        }
    }
}
