using GameSettings.Core;
using GameSettings.Data;
using TMPro;
using UnityEngine;

namespace GameSettings.UI
{
    /// <summary>Связывает TMP_Dropdown "Режим экрана" (Полноэкранный/Оконный/Borderless) с SettingsManager.</summary>
    public class ScreenModeBinder : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown screenModeDropdown;

        private static readonly string[] Labels =
        {
            "Полноэкранный (эксклюзивный)",
            "Полноэкранное окно (без рамки)",
            "Оконный режим"
        };

        private void OnEnable()
        {
            EnumDropdownHelper.Fill(screenModeDropdown, Labels);
            Sync(SettingsManager.Instance.Current);

            screenModeDropdown.onValueChanged.AddListener(OnChanged);
            SettingsManager.Instance.OnSettingsChanged += Sync;
        }

        private void OnDisable()
        {
            screenModeDropdown.onValueChanged.RemoveListener(OnChanged);
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnSettingsChanged -= Sync;
        }

        private void OnChanged(int index)
        {
            SettingsManager.Instance.SetScreenMode((ScreenModeOption)index);
        }

        private void Sync(GraphicSettingsData data)
        {
            screenModeDropdown.SetValueWithoutNotify((int)data.screenMode);
        }
    }
}
