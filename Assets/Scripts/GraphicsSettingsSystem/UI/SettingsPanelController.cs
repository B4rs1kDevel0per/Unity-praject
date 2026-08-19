using GameSettings.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GameSettings.UI
{
    /// <summary>
    /// Верхнеуровневый контроллер панели настроек: кнопки "Применить/Сохранить",
    /// "Отмена", "Сбросить по умолчанию", "Закрыть". Все конкретные биндеры (Resolution,
    /// Shadows, PostProcessing и т.д.) работают независимо и не знают об этом классе.
    /// </summary>
    public class SettingsPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject settingsPanelRoot;

        [Header("Кнопки")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            applyButton.onClick.AddListener(OnApplyClicked);
            cancelButton.onClick.AddListener(OnCancelClicked);
            resetButton.onClick.AddListener(OnResetClicked);
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDestroy()
        {
            applyButton.onClick.RemoveListener(OnApplyClicked);
            cancelButton.onClick.RemoveListener(OnCancelClicked);
            resetButton.onClick.RemoveListener(OnResetClicked);
            closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnApplyClicked()
        {
            SettingsManager.Instance.SaveSettings();
        }

        private void OnCancelClicked()
        {
            SettingsManager.Instance.CancelChanges();
        }

        private void OnResetClicked()
        {
            SettingsManager.Instance.ResetToDefaults();
        }

        private void OnCloseClicked()
        {
            // По желанию: откатываем несохранённые изменения при закрытии без Apply.
            SettingsManager.Instance.CancelChanges();
            settingsPanelRoot.SetActive(false);
        }

        public void Open()
        {
            settingsPanelRoot.SetActive(true);
        }
    }
}
