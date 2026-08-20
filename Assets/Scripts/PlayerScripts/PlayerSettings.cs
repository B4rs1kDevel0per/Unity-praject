using System;
using UnityEngine;

namespace ImmersiveSim.Data
{
    [Serializable]
    public class SurfaceSoundDefinition
    {
        public string surfaceTag = "Default";
        public PhysicsMaterial physicMaterial;
        public AudioClip[] footstepClips;
        public AudioClip landClip;
    }

    [CreateAssetMenu(fileName = "PlayerSettings", menuName = "Immersive Sim/Player Settings", order = 0)]
    public class PlayerSettings : ScriptableObject
    {
        [Header("Скорости передвижения")]
        public float walkSpeed = 3.2f;
        [Tooltip("Множитель скорости движения боком (A/D)")]
        [Range(0.5f, 1.0f)] public float strafeSpeedMultiplier = 0.75f;
        [Tooltip("Время сглаживания инерции при смене направления A/D")]
        public float strafeInertiaDamp = 0.25f;
        [Range(1.0f, 1.5f)] public float fastWalkMultiplier = 1.18f;
        public float crouchSpeed = 1.6f;
        public float adrenalineSprintSpeed = 6.0f;
        public float accelerationTime = 0.15f;
        public float decelerationTime = 0.12f;

        [Header("Микро-шум скорости")]
        public float speedNoiseFrequency = 0.6f;
        [Range(0f, 0.3f)] public float speedNoiseAmplitude = 0.1f;

        [Header("Прыжок")]
        public float jumpForce = 3.2f;
        public float jumpStaminaCost = 12f;
        public float jumpMinStaminaThreshold = 12f;
        public float airborneGravityMultiplier = 1.6f;
        public float gravity = -9.81f;

        [Header("Наклон Q/E (Lean)")]
        [Range(3f, 12f)] public float leanAngle = 7f;
        public float leanPositionOffset = 0.3f;
        public float leanSmoothTime = 0.15f;
        public float leanCastRadius = 0.18f;
        public float leanCastDistance = 0.4f;
        public LayerMask leanObstacleMask;

        [Header("Камера — Шаги и Позиционный Bob")]
        public float idleBreathingFrequency = 0.25f;
        public float idleBreathingAmplitude = 0.008f;
        public float headBobFrequency = 1.8f;
        public float headBobVerticalAmount = 0.035f;
        public float headBobHorizontalAmount = 0.02f;

        [Header("Камера — Ротация и Удар Landing")]
        public float stepRotationPitch = 0.6f;
        public float stepRotationRoll = 0.4f;
        public float landingImpactForce = 0.12f;
        public float landingImpactRotation = 4.5f;
        public float landingRecoverTime = 0.2f;
        public float landingImpactMinVelocity = 2f;

        [Header("Камера — Крен и Адреналин")]
        public float strafeTiltAngle = 2.5f;
        public float strafeTiltSmoothing = 8f;
        [Range(0f, 6f)] public float adrenalineFovDelta = 2.5f;
        public float fovTransitionSpeed = 4f;
        public AnimationCurve vignetteCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.6f);

        [Header("Мышь и Инерция Камеры")]
        public Vector2 mouseSensitivity = new Vector2(1.5f, 1.5f);
        [Tooltip("Инерция обзора: 0 — жесткая мышь, 0.03-0.08 — мягкая инерция головы")]
        [Range(0.001f, 0.1f)] public float lookSmoothTime = 0.035f;
        public float pitchClamp = 85f;

        [Header("Стамина")]
        public float staminaCapacity = 100f;
        public float staminaRegenDelay = 1.5f;
        public float staminaRegenRate = 14f;
        public float fastWalkStaminaDrainRate = 3f;
        public float sprintStaminaDrainRate = 16f;
        [Range(0f, 1f)] public float lowStaminaThreshold01 = 0.25f;

        [Header("Приседание")]
        public float standingHeight = 1.8f;
        public float crouchHeight = 1.0f;
        public float crouchTransitionTime = 0.3f;

        [Header("Аудио")]
        public float stepIntervalWalk = 0.55f;
        public float stepIntervalFastWalk = 0.45f;
        public float stepIntervalCrouch = 0.7f;
        public float stepIntervalSprint = 0.4f;
        public Vector2 footstepPitchRange = new Vector2(0.92f, 1.08f);
        public Vector2 footstepVolumeRange = new Vector2(0.75f, 1f);

        [Header("Аудио поверхностей")]
        public SurfaceSoundDefinition[] surfaceSounds;
        public AudioClip[] crouchDownClips;
        public AudioClip[] standUpClips;
        public AudioClip[] leanRustleClips;
        public AudioClip[] sharpTurnClips;
        public float sharpTurnYawThreshold = 220f;
    }
}