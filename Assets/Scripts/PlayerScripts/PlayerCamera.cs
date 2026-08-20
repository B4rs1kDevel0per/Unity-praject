using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveSim.Player
{
    public class PlayerCamera : MonoBehaviour
    {
        [Header("Зависимости")]
        [SerializeField] private Data.PlayerSettings settings;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Systems.StaminaSystem stamina;
        [SerializeField] private Systems.AdrenalineController adrenaline;

        [Header("Трансформы")]
        [SerializeField] private Transform yawTransform;
        [SerializeField] private Transform pitchPivot;
        [SerializeField] private Camera targetCamera;

        private float _pitch;
        private float _yaw;
        private Vector2 _smoothMouseDelta;
        private Vector2 _mouseVelocity;

        private float _stepBobTimer;
        private Vector3 _proceduralPosOffset;
        private Vector3 _proceduralRotOffset;

        private float _landingOffsetY;
        private float _landingPitchOffset;
        private float _landingVelocity;

        private float _currentFov;
        private float _targetFov;
        private float _currentStrafeTilt;
        private bool _lookLocked;

        public float VignetteIntensity { get; private set; }

        private void Awake()
        {
            if (targetCamera == null) targetCamera = GetComponentInChildren<Camera>();
            _currentFov = _targetFov = targetCamera != null ? targetCamera.fieldOfView : 60f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnEnable()
        {
            if (motor != null) motor.OnLanded += HandleLanded;
            if (adrenaline != null) adrenaline.OnAdrenalineStateChanged += HandleAdrenalineChanged;
        }

        private void OnDisable()
        {
            if (motor != null) motor.OnLanded -= HandleLanded;
            if (adrenaline != null) adrenaline.OnAdrenalineStateChanged -= HandleAdrenalineChanged;
        }

        public void SetLookLocked(bool locked) => _lookLocked = locked;

        private void Update()
        {
            float dt = Time.deltaTime;

            if (!_lookLocked) ApplyMouseLook(dt);

            UpdateProceduralMotion(dt);
            UpdateLandingRecovery(dt);
            UpdateAdrenalineVisuals(dt);
            UpdateStrafeTilt(dt);
            ApplyFinalTransform();
        }

        private void ApplyMouseLook(float dt)
        {
            if (Mouse.current == null) return;

            Vector2 rawDelta = Mouse.current.delta.ReadValue();
            Vector2 targetDelta = new Vector2(rawDelta.x * settings.mouseSensitivity.x, rawDelta.y * settings.mouseSensitivity.y);

            // Мягкая инерция камеры при движении мыши (Cam Inertia)
            _smoothMouseDelta = Vector2.SmoothDamp(_smoothMouseDelta, targetDelta, ref _mouseVelocity, Mathf.Max(0.001f, settings.lookSmoothTime));

            _yaw += _smoothMouseDelta.x * 0.1f;
            _pitch = Mathf.Clamp(_pitch - (_smoothMouseDelta.y * 0.1f), -settings.pitchClamp, settings.pitchClamp);

            if (yawTransform != null) yawTransform.localRotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        private void UpdateProceduralMotion(float dt)
        {
            // 1. Дыхание в покое
            float staminaLoss01 = stamina != null ? (1f - stamina.Normalized01) : 0f;
            float breathingMultiplier = 1f + (staminaLoss01 * 1.5f);
            float idleFactor = (motor != null && motor.IsMovingInput) ? 0f : 1f;

            float breatheX = Mathf.Sin(Time.time * settings.idleBreathingFrequency * Mathf.PI * 2f);
            float breatheY = Mathf.Sin(Time.time * settings.idleBreathingFrequency * Mathf.PI * 2f * 0.5f);
            Vector3 breathingOffset = new Vector3(breatheX, breatheY, 0f) * settings.idleBreathingAmplitude * breathingMultiplier * idleFactor;

            // 2. Ощутимый HeadBob при ходьбе (Позиция и Ротация)
            float speed = motor != null ? motor.CurrentHorizontalSpeed : 0f;
            bool isGrounded = motor != null && motor.IsGrounded;

            if (isGrounded && speed > 0.1f)
            {
                _stepBobTimer += dt * speed * settings.headBobFrequency;

                float bobY = Mathf.Sin(_stepBobTimer * Mathf.PI * 2f) * settings.headBobVerticalAmount;
                float bobX = Mathf.Cos(_stepBobTimer * Mathf.PI) * settings.headBobHorizontalAmount;

                // Ротация при шаге (легкие покачивания головы)
                float rotPitch = Mathf.Sin(_stepBobTimer * Mathf.PI * 2f) * settings.stepRotationPitch;
                float rotRoll = Mathf.Cos(_stepBobTimer * Mathf.PI) * settings.stepRotationRoll;

                _proceduralPosOffset = breathingOffset + new Vector3(bobX, bobY, 0f);
                _proceduralRotOffset = new Vector3(rotPitch, 0f, rotRoll);
            }
            else
            {
                _stepBobTimer = 0f;
                _proceduralPosOffset = Vector3.Lerp(_proceduralPosOffset, breathingOffset, dt * 6f);
                _proceduralRotOffset = Vector3.Lerp(_proceduralRotOffset, Vector3.zero, dt * 6f);
            }
        }

        private void HandleLanded(float impactSpeed)
        {
            _landingVelocity -= impactSpeed * settings.landingImpactForce;
            _landingPitchOffset += impactSpeed * settings.landingImpactRotation; // Удар ротации (кивок)
        }

        private void UpdateLandingRecovery(float dt)
        {
            // Пружинящий возврат позиционного проседания и кивка головы при приземлении
            float recoverTime = Mathf.Max(0.01f, settings.landingRecoverTime);
            float springStrength = 1f / recoverTime;

            _landingVelocity += (-_landingOffsetY * springStrength * 12f) * dt;
            _landingVelocity *= Mathf.Clamp01(1f - dt / recoverTime * 2.5f);
            _landingOffsetY += _landingVelocity * dt;

            _landingPitchOffset = Mathf.Lerp(_landingPitchOffset, 0f, dt * 10f);
        }

        private void HandleAdrenalineChanged(bool active)
        {
            _targetFov = (targetCamera != null ? 60f : 60f) + (active ? settings.adrenalineFovDelta : 0f);
        }

        private void UpdateAdrenalineVisuals(float dt)
        {
            _currentFov = Mathf.MoveTowards(_currentFov, _targetFov, settings.fovTransitionSpeed * dt);
            if (targetCamera != null) targetCamera.fieldOfView = _currentFov;

            float adrenalineFactor = adrenaline != null && adrenaline.IsAdrenalineActive ? 1f : 0f;
            float staminaFactor = stamina != null ? (1f - stamina.Normalized01) : 0f;
            VignetteIntensity = settings.vignetteCurve.Evaluate(Mathf.Clamp01(Mathf.Max(adrenalineFactor, staminaFactor)));
        }

        private void UpdateStrafeTilt(float dt)
        {
            float targetTilt = motor != null ? -motor.MoveInputX * settings.strafeTiltAngle : 0f;
            _currentStrafeTilt = Mathf.Lerp(_currentStrafeTilt, targetTilt, 1f - Mathf.Exp(-settings.strafeTiltSmoothing * dt));
        }

        private void ApplyFinalTransform()
        {
            if (pitchPivot == null) return;

            // Итоговая ротация с учётом уклона, поворота шага и кивка приземления
            Quaternion finalRotation = Quaternion.Euler(
                _pitch + _proceduralRotOffset.x + _landingPitchOffset,
                0f,
                _currentStrafeTilt + _proceduralRotOffset.z
            );

            pitchPivot.localRotation = finalRotation;
            pitchPivot.localPosition = _proceduralPosOffset + new Vector3(0f, _landingOffsetY, 0f);
        }
    }
}