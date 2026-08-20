using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameSettings.Core
{
    /// <summary>
    /// Кладите этот компонент в КАЖДУЮ игровую сцену (не в сцену настроек!), рядом с камерой игрока
    /// и глобальным Volume этой сцены. Он нужен только потому, что настройки у вас вынесены в
    /// отдельную сцену "Settings": SettingsManager/GraphicsApplier живут постоянно
    /// (DontDestroyOnLoad), а камера и Volume каждой игровой сцены создаются заново при её загрузке.
    /// Без регистрации сглаживание (AA) и постобработка (Bloom/AO/MotionBlur) не будут применяться
    /// к текущей сцене после возврата из настроек.
    ///
    /// Ставится на GameObject с камерой (или на любой объект, где в инспекторе можно
    /// перетащить ссылку на Camera и Volume текущей сцены).
    /// </summary>
    public class RuntimeGraphicsTargets : MonoBehaviour
    {
        [Tooltip("Камера этой сцены (например, дочерняя камера у PlayerCamera). Если оставить пустым — возьмётся Camera.main.")]
        [SerializeField] private Camera sceneCamera;

        [Tooltip("Global Volume этой сцены (Is Global = true), на профиле которого лежат Bloom/MotionBlur.")]
        [SerializeField] private Volume sceneVolume;

        [Tooltip("Необязательно: Renderer Feature экранного AO этой сцены, если отличается от общего.")]
        [SerializeField] private ScriptableRendererFeature ambientOcclusionFeature;

        private void Start()
        {
            if (sceneCamera == null)
                sceneCamera = Camera.main;

            if (SettingsManager.Instance == null)
            {
                Debug.LogWarning("[RuntimeGraphicsTargets] SettingsManager не найден в сцене. " +
                                  "Убедитесь, что объект SettingsSystem помечен DontDestroyOnLoad и существует до загрузки этой сцены.");
                return;
            }

            SettingsManager.Instance.RegisterRuntimeTargets(sceneCamera, sceneVolume, ambientOcclusionFeature);
        }
    }
}
