using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveSim.Terminal
{
    public class TerminalController : MonoBehaviour, Interaction.IInteractable
    {
        [Header("Настройки экрана")]
        [SerializeField] private string promptText = "Взаимодействовать с терминалом [E]";
        [SerializeField] private Transform screenAnchorTransform;
        [SerializeField] private float transitionDuration = 0.6f;

        private bool _isOccupied;
        private Transform _playerCameraTransform;
        private Player.PlayerMotor _playerMotor;
        private Player.PlayerCamera _playerCamera;
        private Vector3 _savedCamLocalPos;
        private Quaternion _savedCamLocalRot;
        private Transform _savedCamParent;

        public string InteractionPrompt => promptText;
        public bool CanInteract(MonoBehaviour interactor) => !_isOccupied;

        public void OnFocus() { }
        public void OnFocusLost() { }

        public void Interact(MonoBehaviour interactor)
        {
            if (_isOccupied) return;

            _playerMotor = interactor.GetComponentInParent<Player.PlayerMotor>();
            _playerCamera = interactor.GetComponentInParent<Player.PlayerCamera>();

            if (_playerCamera != null)
            {
                Camera cam = _playerCamera.GetComponentInChildren<Camera>();
                if (cam != null) _playerCameraTransform = cam.transform;
            }

            if (_playerCameraTransform == null || screenAnchorTransform == null) return;

            StartCoroutine(EnterTerminalRoutine());
        }

        private IEnumerator EnterTerminalRoutine()
        {
            _isOccupied = true;
            if (_playerMotor != null) _playerMotor.SetInputLocked(true);
            if (_playerCamera != null) _playerCamera.SetLookLocked(true);

            _savedCamParent = _playerCameraTransform.parent;
            _savedCamLocalPos = _playerCameraTransform.localPosition;
            _savedCamLocalRot = _playerCameraTransform.localRotation;

            Vector3 startPos = _playerCameraTransform.position;
            Quaternion startRot = _playerCameraTransform.rotation;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
                _playerCameraTransform.position = Vector3.Lerp(startPos, screenAnchorTransform.position, t);
                _playerCameraTransform.rotation = Quaternion.Slerp(startRot, screenAnchorTransform.rotation, t);
                yield return null;
            }

            _playerCameraTransform.position = screenAnchorTransform.position;
            _playerCameraTransform.rotation = screenAnchorTransform.rotation;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (!_isOccupied) return;

            if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
            {
                StartCoroutine(ExitTerminalRoutine());
            }
        }

        private IEnumerator ExitTerminalRoutine()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Vector3 startPos = _playerCameraTransform.position;
            Quaternion startRot = _playerCameraTransform.rotation;

            Vector3 targetPos = _savedCamParent.TransformPoint(_savedCamLocalPos);
            Quaternion targetRot = _savedCamParent.rotation * _savedCamLocalRot;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
                _playerCameraTransform.position = Vector3.Lerp(startPos, targetPos, t);
                _playerCameraTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            _playerCameraTransform.SetParent(_savedCamParent);
            _playerCameraTransform.localPosition = _savedCamLocalPos;
            _playerCameraTransform.localRotation = _savedCamLocalRot;

            if (_playerMotor != null) _playerMotor.SetInputLocked(false);
            if (_playerCamera != null) _playerCamera.SetLookLocked(false);

            _isOccupied = false;
        }
    }
}