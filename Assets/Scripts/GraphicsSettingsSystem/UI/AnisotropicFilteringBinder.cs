using GameSettings.Core;
using GameSettings.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GameSettings.UI
{
    /// <summary>Связывает Dropdown "Анизотропная фильтрация" с SettingsManager.</summary>
    public class AnisotropicFilteringBinder : MonoBehaviour
    {
        [SerializeField] private Dropdown anisotropicDropdown;

        private static readonly string[] Labels = { "Выключена", "По текстуре", "Принудительно (макс.)" };

        private void OnEnable()
        {
            EnumDropdownHelper.Fill(anisotropicDropdown, Labels);
            Sync(SettingsManager.Instance.Current);

            anisotropicDropdown.onValueChanged.AddListener(OnChanged);
            SettingsManager.Instance.OnSettingsChanged += Sync;
        }

        private void OnDisable()
        {
            anisotropicDropdown.onValueChanged.RemoveListener(OnChanged);
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnSettingsChanged -= Sync;
        }

        private void OnChanged(int index)
        {
            SettingsManager.Instance.SetAnisotropicLevel((AnisotropicLevelOption)index);
        }

        private void Sync(GraphicSettingsData data)
        {
            anisotropicDropdown.SetValueWithoutNotify((int)data.anisotropicLevel);
        }
    }
}
