using System;
using UnityEngine;

namespace ImmersiveSim.Player
{
    public class StepManager : MonoBehaviour
    {
        [Header("Зависимости")]
        [SerializeField] private Data.PlayerSettings settings;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerLean lean;
        [SerializeField] private AudioSource footstepAudioSource;
        [SerializeField] private AudioSource clothingAudioSource;

        private float _accumulatedDistance;
        private float _lastYaw;

        private void OnEnable()
        {
            if (motor != null) motor.OnCrouchTransition += HandleCrouchTransition;
            if (lean != null) lean.OnLeanStarted += HandleLeanStarted;
        }

        private void OnDisable()
        {
            if (motor != null) motor.OnCrouchTransition -= HandleCrouchTransition;
            if (lean != null) lean.OnLeanStarted -= HandleLeanStarted;
        }

        private void Update()
        {
            if (motor == null || !motor.IsGrounded) return;

            float speed = motor.CurrentHorizontalSpeed;
            if (speed > 0.05f)
            {
                _accumulatedDistance += speed * Time.deltaTime;
                float currentInterval = GetStepIntervalForState(motor.CurrentState);

                if (_accumulatedDistance >= currentInterval)
                {
                    _accumulatedDistance = 0f;
                    PlayFootstepSound();
                }
            }

            float currentYaw = transform.eulerAngles.y;
            float yawSpeed = Mathf.Abs(Mathf.DeltaAngle(currentYaw, _lastYaw)) / Time.deltaTime;
            _lastYaw = currentYaw;

            if (yawSpeed > settings.sharpTurnYawThreshold && clothingAudioSource != null && !clothingAudioSource.isPlaying)
            {
                PlayRandomClip(clothingAudioSource, settings.sharpTurnClips, 0.4f, 0.6f);
            }
        }

        private float GetStepIntervalForState(MovementState state)
        {
            if (motor.IsCrouching) return settings.stepIntervalCrouch;
            switch (state)
            {
                case MovementState.Sprinting: return settings.stepIntervalSprint;
                case MovementState.FastWalking: return settings.stepIntervalFastWalk;
                default: return settings.stepIntervalWalk;
            }
        }

        private void PlayFootstepSound()
        {
            if (footstepAudioSource == null || settings.surfaceSounds == null || settings.surfaceSounds.Length == 0) return;

            string detectedTag = "Default";
            PhysicsMaterial detectedMaterial = null;

            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 0.8f))
            {
                detectedTag = hit.collider.tag;
                detectedMaterial = hit.collider.sharedMaterial;
            }

            Data.SurfaceSoundDefinition matchedDef = null;
            foreach (var def in settings.surfaceSounds)
            {
                if (detectedMaterial != null && def.physicMaterial == detectedMaterial)
                {
                    matchedDef = def;
                    break;
                }
                if (def.surfaceTag.Equals(detectedTag, StringComparison.OrdinalIgnoreCase))
                {
                    matchedDef = def;
                }
            }

            if (matchedDef == null) matchedDef = settings.surfaceSounds[0];
            if (matchedDef.footstepClips != null && matchedDef.footstepClips.Length > 0)
            {
                float pitch = UnityEngine.Random.Range(settings.footstepPitchRange.x, settings.footstepPitchRange.y);
                float volume = UnityEngine.Random.Range(settings.footstepVolumeRange.x, settings.footstepVolumeRange.y);
                footstepAudioSource.pitch = pitch;
                AudioClip clip = matchedDef.footstepClips[UnityEngine.Random.Range(0, matchedDef.footstepClips.Length)];
                footstepAudioSource.PlayOneShot(clip, volume);
            }
        }

        private void HandleCrouchTransition(bool isCrouching)
        {
            AudioClip[] clips = isCrouching ? settings.crouchDownClips : settings.standUpClips;
            PlayRandomClip(clothingAudioSource, clips, 0.5f, 0.7f);
        }

        private void HandleLeanStarted()
        {
            PlayRandomClip(clothingAudioSource, settings.leanRustleClips, 0.3f, 0.5f);
        }

        private void PlayRandomClip(AudioSource source, AudioClip[] clips, float minVol, float maxVol)
        {
            if (source == null || clips == null || clips.Length == 0) return;
            AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            source.PlayOneShot(clip, UnityEngine.Random.Range(minVol, maxVol));
        }
    }
}