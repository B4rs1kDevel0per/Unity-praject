using GameSettings.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        [Header("Переход при закрытии")]
        [Tooltip("Если true — кнопка 'Закрыть' не просто прячет панель, а грузит сцену меню (см. menuSceneName).")]
        [SerializeField] private bool closeLoadsMenuScene = true;
        [SerializeField] private string menuSceneName = "Menu";

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

            if (!closeLoadsMenuScene)
            {
                settingsPanelRoot.SetActive(false);
                return;
            }

            // Если в проекте есть SceneTransitionManager (глитч-переход между сценами) — используем его.
            // Иначе — обычная синхронная загрузка сцены.
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadScene(menuSceneName);
            }
            else
            {
                SceneManager.LoadScene(menuSceneName);
            }
        }

        public void Open()
        {
            settingsPanelRoot.SetActive(true);
        }
    }
}
