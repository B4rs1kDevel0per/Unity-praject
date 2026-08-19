using GameSettings.Core;
using GameSettings.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GameSettings.UI
{
    /// <summary>Связывает Toggle "Вертикальная синхронизация" с SettingsManager.</summary>
    public class VSyncBinder : MonoBehaviour
    {
        [SerializeField] private Toggle vSyncToggle;

        private void OnEnable()
        {
            Sync(SettingsManager.Instance.Current);
            vSyncToggle.onValueChanged.AddListener(OnChanged);
            SettingsManager.Instance.OnSettingsChanged += Sync;
        }

        private void OnDisable()
        {
            vSyncToggle.onValueChanged.RemoveListener(OnChanged);
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnSettingsChanged -= Sync;
        }

        private void OnChanged(bool value)
        {
            SettingsManager.Instance.SetVSync(value);
        }

        private void Sync(GraphicSettingsData data)
        {
            vSyncToggle.SetIsOnWithoutNotify(data.vSyncEnabled);
        }
    }
}
