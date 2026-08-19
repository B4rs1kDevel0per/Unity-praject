using GameSettings.Core;
using GameSettings.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GameSettings.UI
{
    /// <summary>
    /// Биндер секции "Постобработка": Bloom, Ambient Occlusion, Motion Blur.
    /// Каждый эффект — Toggle (вкл/выкл) + Slider (интенсивность).
    /// </summary>
    public class PostProcessingBinder : MonoBehaviour
    {
        [Header("Bloom")]
        [SerializeField] private Toggle bloomToggle;
        [SerializeField] private Slider bloomIntensitySlider;

        [Header("Ambient Occlusion")]
        [SerializeField] private Toggle aoToggle;
        [SerializeField] private Slider aoIntensitySlider;

        [Header("Motion Blur")]
        [SerializeField] private Toggle motionBlurToggle;
        [SerializeField] private Slider motionBlurIntensitySlider;

        private void OnEnable()
        {
            Sync(SettingsManager.Instance.Current);

            bloomToggle.onValueChanged.AddListener(OnBloomChanged);
            bloomIntensitySlider.onValueChanged.AddListener(OnBloomChanged);

            aoToggle.onValueChanged.AddListener(OnAoChanged);
            aoIntensitySlider.onValueChanged.AddListener(OnAoChanged);

            motionBlurToggle.onValueChanged.AddListener(OnMotionBlurChanged);
            motionBlurIntensitySlider.onValueChanged.AddListener(OnMotionBlurChanged);

            SettingsManager.Instance.OnSettingsChanged += Sync;
        }

        private void OnDisable()
        {
            bloomToggle.onValueChanged.RemoveListener(OnBloomChanged);
            bloomIntensitySlider.onValueChanged.RemoveListener(OnBloomChanged);

            aoToggle.onValueChanged.RemoveListener(OnAoChanged);
            aoIntensitySlider.onValueChanged.RemoveListener(OnAoChanged);

            motionBlurToggle.onValueChanged.RemoveListener(OnMotionBlurChanged);
            motionBlurIntensitySlider.onValueChanged.RemoveListener(OnMotionBlurChanged);

            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnSettingsChanged -= Sync;
        }

        // Слушатели переиспользуются и Toggle'ом, и Slider'ом — сигнатура bool/float, поэтому две перегрузки:
        private void OnBloomChanged(bool _) => PushBloom();
        private void OnBloomChanged(float _) => PushBloom();
        private void PushBloom() => SettingsManager.Instance.SetBloom(bloomToggle.isOn, bloomIntensitySlider.value);

        private void OnAoChanged(bool _) => PushAo();
        private void OnAoChanged(float _) => PushAo();
        private void PushAo() => SettingsManager.Instance.SetAmbientOcclusion(aoToggle.isOn, aoIntensitySlider.value);

        private void OnMotionBlurChanged(bool _) => PushMotionBlur();
        private void OnMotionBlurChanged(float _) => PushMotionBlur();
        private void PushMotionBlur() => SettingsManager.Instance.SetMotionBlur(motionBlurToggle.isOn, motionBlurIntensitySlider.value);

        private void Sync(GraphicSettingsData data)
        {
            bloomToggle.SetIsOnWithoutNotify(data.bloomEnabled);
            bloomIntensitySlider.SetValueWithoutNotify(data.bloomIntensity);

            aoToggle.SetIsOnWithoutNotify(data.ambientOcclusionEnabled);
            aoIntensitySlider.SetValueWithoutNotify(data.ambientOcclusionIntensity);

            motionBlurToggle.SetIsOnWithoutNotify(data.motionBlurEnabled);
            motionBlurIntensitySlider.SetValueWithoutNotify(data.motionBlurIntensity);
        }
    }
}
