using GameSettings.Core;
using GameSettings.Data;
using TMPro;
using UnityEngine;

namespace GameSettings.UI
{
    /// <summary>Связывает TMP_Dropdown "Качество текстур" с SettingsManager.</summary>
    public class TextureQualityBinder : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown textureQualityDropdown;

        private static readonly string[] Labels = { "Полное", "Половина", "Четверть", "Восьмая" };

        private void OnEnable()
        {
            EnumDropdownHelper.Fill(textureQualityDropdown, Labels);
            Sync(SettingsManager.Instance.Current);

            textureQualityDropdown.onValueChanged.AddListener(OnChanged);
            SettingsManager.Instance.OnSettingsChanged += Sync;
        }

        private void OnDisable()
        {
            textureQualityDropdown.onValueChanged.RemoveListener(OnChanged);
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnSettingsChanged -= Sync;
        }

        private void OnChanged(int index)
        {
            SettingsManager.Instance.SetTextureQuality((TextureQualityOption)index);
        }

        private void Sync(GraphicSettingsData data)
        {
            textureQualityDropdown.SetValueWithoutNotify((int)data.textureQuality);
        }
    }
}
