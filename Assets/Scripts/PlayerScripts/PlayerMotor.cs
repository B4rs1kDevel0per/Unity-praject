using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveSim.Player
{
    public enum MovementState { Idle, Walking, FastWalking, Sprinting }

    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Зависимости")]
        [SerializeField] private Data.PlayerSettings settings;
        [SerializeField] private Systems.StaminaSystem stamina;
        [SerializeField] private Systems.AdrenalineController adrenaline;

        [Header("Трансформы")]
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private float cameraStandingLocalY = 1.65f;
        [SerializeField] private float cameraCrouchLocalY = 0.9f;

        private CharacterController _controller;
        private Vector2 _moveInput;
        private Vector2 _smoothedStrafeInput;
        private Vector2 _strafeInputVelocity;
        private Vector3 _verticalVelocity;
        private Vector3 _horizontalVelocity;
        private Vector3 _horizontalVelocityDampVelocity;

        private bool _isCrouching;
        private float _crouchLerpT = 0f;
        private float _cameraYVelocity;
        private float _noiseSeedX;
        private bool _inputLocked;
        private bool _wasGroundedLastFrame = true;

        public MovementState CurrentState { get; private set; } = MovementState.Idle;
        public bool IsGrounded => _controller.isGrounded;
        public bool IsCrouching => _isCrouching;
        public bool IsMovingInput => _moveInput.sqrMagnitude > 0.01f;
        public Vector3 HorizontalVelocity => _horizontalVelocity;
        public float CurrentHorizontalSpeed => _horizontalVelocity.magnitude;
        public float MoveInputX => _smoothedStrafeInput.x;

        public event Action OnJump;
        public event Action<float> OnLanded;
        public event Action<bool> OnCrouchTransition;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _noiseSeedX = UnityEngine.Random.value * 1000f;

            if (settings != null)
            {
                _controller.height = settings.standingHeight;
                _controller.center = new Vector3(0f, settings.standingHeight * 0.5f, 0f);
            }
        }

        public void SetInputLocked(bool locked)
        {
            _inputLocked = locked;
            if (locked) _moveInput = Vector2.zero;
        }

        private void Update()
        {
            if (!_inputLocked) ReadInput();

            float dt = Time.deltaTime;
            UpdateCrouchState(dt);
            UpdateMovementState();
            UpdateHorizontalVelocity(dt);
            UpdateVerticalVelocity(dt);

            Vector3 motion = (_horizontalVelocity + _verticalVelocity) * dt;
            _controller.Move(motion);

            HandleLandingDetection();
        }

        private void ReadInput()
        {
            float x = 0f, y = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) y += 1f;
                if (Keyboard.current.sKey.isPressed) y -= 1f;
                if (Keyboard.current.aKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed) x += 1f;

                if (Keyboard.current.cKey.wasPressedThisFrame || Keyboard.current.leftCtrlKey.wasPressedThisFrame)
                {
                    SetCrouching(!_isCrouching);
                }

                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    TryJump();
                }
            }

            _moveInput = new Vector2(x, y);
            if (_moveInput.sqrMagnitude > 1f) _moveInput.Normalize();
        }

        public void SetCrouching(bool crouching)
        {
            if (_isCrouching == crouching) return;
            _isCrouching = crouching;
            OnCrouchTransition?.Invoke(crouching);
        }

        private void UpdateCrouchState(float dt)
        {
            float target = _isCrouching ? 1f : 0f;
            float speed = settings.crouchTransitionTime <= 0f ? 10f : 1f / settings.crouchTransitionTime;

            // Плавная сглаженная интерполяция высоты
            _crouchLerpT = Mathf.MoveTowards(_crouchLerpT, target, speed * dt);
            float smoothT = Mathf.SmoothStep(0f, 1f, _crouchLerpT);

            float currentHeight = Mathf.Lerp(settings.standingHeight, settings.crouchHeight, smoothT);
            _controller.height = currentHeight;
            _controller.center = new Vector3(0f, currentHeight * 0.5f, 0f);

            if (cameraRoot != null)
            {
                float targetCamY = Mathf.Lerp(cameraStandingLocalY, cameraCrouchLocalY, smoothT);
                Vector3 lp = cameraRoot.localPosition;
                float newCamY = Mathf.SmoothDamp(lp.y, targetCamY, ref _cameraYVelocity, 0.05f);
                cameraRoot.localPosition = new Vector3(lp.x, newCamY, lp.z);
            }
        }

        private void UpdateMovementState()
        {
            if (_inputLocked || !IsMovingInput)
            {
                CurrentState = MovementState.Idle;
                return;
            }

            bool isShift = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            bool isAlt = Keyboard.current != null && Keyboard.current.leftAltKey.isPressed;

            if (isShift && adrenaline != null && adrenaline.IsAdrenalineActive && !_isCrouching && stamina.HasAtLeast(0.01f))
            {
                CurrentState = MovementState.Sprinting;
            }
            else if (isAlt && !_isCrouching && stamina.HasAtLeast(0.01f))
            {
                CurrentState = MovementState.FastWalking;
            }
            else
            {
                CurrentState = MovementState.Walking;
            }
        }

        private void UpdateHorizontalVelocity(float dt)
        {
            // Инерция переключения бокового движения (A/D)
            _smoothedStrafeInput = Vector2.SmoothDamp(_smoothedStrafeInput, _moveInput, ref _strafeInputVelocity, settings.strafeInertiaDamp);

            float baseSpeed = GetTargetSpeedForState(CurrentState);

            // Раздельный расчёт для продольного (W/S) и бокового (A/D) движения
            Vector3 forwardMove = transform.forward * _smoothedStrafeInput.y * baseSpeed;
            Vector3 sideMove = transform.right * _smoothedStrafeInput.x * (baseSpeed * settings.strafeSpeedMultiplier);

            Vector3 wishDir = forwardMove + sideMove;

            // Ограничиваем диагональный вектор, чтобы W+D не ускоряло
            if (wishDir.magnitude > baseSpeed)
            {
                wishDir = wishDir.normalized * baseSpeed;
            }

            float noise = Mathf.PerlinNoise(_noiseSeedX + Time.time * settings.speedNoiseFrequency, 0.5f) * 2f - 1f;
            Vector3 targetVelocity = wishDir + (wishDir.normalized * (noise * settings.speedNoiseAmplitude));

            float smoothTime = targetVelocity.sqrMagnitude > _horizontalVelocity.sqrMagnitude
                ? settings.accelerationTime
                : settings.decelerationTime;

            _horizontalVelocity = Vector3.SmoothDamp(
                _horizontalVelocity, targetVelocity, ref _horizontalVelocityDampVelocity, Mathf.Max(0.001f, smoothTime));

            ApplyStaminaDrain(dt);
        }

        private float GetTargetSpeedForState(MovementState state)
        {
            if (_isCrouching) return settings.crouchSpeed;
            switch (state)
            {
                case MovementState.Sprinting: return settings.adrenalineSprintSpeed;
                case MovementState.FastWalking: return settings.walkSpeed * settings.fastWalkMultiplier;
                case MovementState.Walking: return settings.walkSpeed;
                default: return 0f;
            }
        }

        private void ApplyStaminaDrain(float dt)
        {
            bool drained = false;
            if (CurrentState == MovementState.Sprinting)
            {
                drained = stamina.TryDrain(settings.sprintStaminaDrainRate, dt);
                if (!drained) CurrentState = MovementState.Walking;
            }
            else if (CurrentState == MovementState.FastWalking)
            {
                drained = stamina.TryDrain(settings.fastWalkStaminaDrainRate, dt);
                if (!drained) CurrentState = MovementState.Walking;
            }

            if (stamina != null) stamina.Tick(dt, drained);
        }

        private void UpdateVerticalVelocity(float dt)
        {
            if (_controller.isGrounded && _verticalVelocity.y < 0f) _verticalVelocity.y = -1f;
            float gravityMult = _controller.isGrounded ? 1f : settings.airborneGravityMultiplier;
            _verticalVelocity.y += settings.gravity * gravityMult * dt;
        }

        private void TryJump()
        {
            if (_inputLocked || !_controller.isGrounded || _isCrouching || stamina == null) return;
            if (!stamina.HasAtLeast(settings.jumpMinStaminaThreshold)) return;

            stamina.TrySpendInstant(settings.jumpStaminaCost);
            _verticalVelocity.y = settings.jumpForce;
            OnJump?.Invoke();
        }

        private void HandleLandingDetection()
        {
            bool groundedNow = _controller.isGrounded;
            if (groundedNow && !_wasGroundedLastFrame)
            {
                float impactSpeed = Mathf.Abs(_verticalVelocity.y);
                if (impactSpeed >= settings.landingImpactMinVelocity) OnLanded?.Invoke(impactSpeed);
            }
            _wasGroundedLastFrame = groundedNow;
        }
    }
}