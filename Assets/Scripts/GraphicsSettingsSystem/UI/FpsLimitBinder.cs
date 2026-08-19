using System.Linq;
using GameSettings.Core;
using GameSettings.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GameSettings.UI
{
    /// <summary>
    /// Связывает Dropdown "Лимит FPS" с SettingsManager.
    /// Список пресетов задаётся вручную (0 = без ограничения).
    /// </summary>
    public class FpsLimitBinder : MonoBehaviour
    {
        [SerializeField] private Dropdown fpsLimitDropdown;
        [SerializeField] private int[] presets = { 0, 30, 60, 90, 120, 144, 240 };

        private void OnEnable()
        {
            var labels = presets.Select(p => p == 0 ? "Без ограничения" : $"{p} FPS").ToArray();
            EnumDropdownHelper.Fill(fpsLimitDropdown, labels);

            Sync(SettingsManager.Instance.Current);
            fpsLimitDropdown.onValueChanged.AddListener(OnChanged);
            SettingsManager.Instance.OnSettingsChanged += Sync;
        }

        private void OnDisable()
        {
            fpsLimitDropdown.onValueChanged.RemoveListener(OnChanged);
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnSettingsChanged -= Sync;
        }

        private void OnChanged(int index)
        {
            SettingsManager.Instance.SetFpsLimit(presets[index]);
        }

        private void Sync(GraphicSettingsData data)
        {
            int index = System.Array.IndexOf(presets, data.fpsLimit);
            if (index < 0) index = 0;
            fpsLimitDropdown.SetValueWithoutNotify(index);
        }
    }
}
