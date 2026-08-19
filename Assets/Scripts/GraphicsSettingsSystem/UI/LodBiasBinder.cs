using GameSettings.Core;
using GameSettings.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GameSettings.UI
{
    /// <summary>Связывает Slider "Детализация моделей (LOD)" с SettingsManager.</summary>
    public class LodBiasBinder : MonoBehaviour
    {
        [SerializeField] private Slider lodBiasSlider; // рекомендуемый диапазон 0.25 .. 2.0
        [SerializeField] private Text valueLabel;       // необязательно

        private void OnEnable()
        {
            Sync(SettingsManager.Instance.Current);
            lodBiasSlider.onValueChanged.AddListener(OnChanged);
            SettingsManager.Instance.OnSettingsChanged += Sync;
        }

        private void OnDisable()
        {
            lodBiasSlider.onValueChanged.RemoveListener(OnChanged);
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnSettingsChanged -= Sync;
        }

        private void OnChanged(float value)
        {
            SettingsManager.Instance.SetLodBias(value);
            if (valueLabel != null)
                valueLabel.text = value.ToString("0.00");
        }

        private void Sync(GraphicSettingsData data)
        {
            lodBiasSlider.SetValueWithoutNotify(data.lodBias);
            if (valueLabel != null)
                valueLabel.text = data.lodBias.ToString("0.00");
        }
    }
}
