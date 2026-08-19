using GameSettings.Core;
using GameSettings.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GameSettings.UI
{
    /// <summary>Связывает Dropdown "Сглаживание" с SettingsManager.</summary>
    public class AntiAliasingBinder : MonoBehaviour
    {
        [SerializeField] private Dropdown antiAliasingDropdown;

        private static readonly string[] Labels =
        {
            "Выключено", "FXAA", "SMAA", "TAA", "MSAA x2", "MSAA x4", "MSAA x8"
        };

        private void OnEnable()
        {
            EnumDropdownHelper.Fill(antiAliasingDropdown, Labels);
            Sync(SettingsManager.Instance.Current);

            antiAliasingDropdown.onValueChanged.AddListener(OnChanged);
            SettingsManager.Instance.OnSettingsChanged += Sync;
        }

        private void OnDisable()
        {
            antiAliasingDropdown.onValueChanged.RemoveListener(OnChanged);
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnSettingsChanged -= Sync;
        }

        private void OnChanged(int index)
        {
            SettingsManager.Instance.SetAntiAliasing((AntiAliasingOption)index);
        }

        private void Sync(GraphicSettingsData data)
        {
            antiAliasingDropdown.SetValueWithoutNotify((int)data.antiAliasing);
        }
    }
}
