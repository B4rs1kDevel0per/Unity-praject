using System;
using UnityEngine;

namespace ImmersiveSim.Systems
{
    public class StaminaSystem : MonoBehaviour
    {
        [SerializeField] private Data.PlayerSettings settings;

        public float CurrentStamina { get; private set; }
        public float MaxStamina => settings != null ? settings.staminaCapacity : 100f;
        public float Normalized01 => MaxStamina <= 0f ? 0f : Mathf.Clamp01(CurrentStamina / MaxStamina);
        public bool IsLow => Normalized01 <= (settings != null ? settings.lowStaminaThreshold01 : 0.25f);
        public bool IsDepleted => CurrentStamina <= 0.0001f;

        public event Action<float> OnStaminaChanged;
        public event Action OnStaminaDepleted;
        public event Action OnStaminaRecovered;

        private float _timeSinceLastDrain;
        private bool _wasDepleted;

        private void Awake()
        {
            if (settings != null) CurrentStamina = settings.staminaCapacity;
        }

        public bool TryDrain(float ratePerSecond, float deltaTime)
        {
            float cost = ratePerSecond * deltaTime;
            if (CurrentStamina < cost) return false;

            SetStamina(CurrentStamina - cost);
            _timeSinceLastDrain = 0f;
            return true;
        }

        public bool TrySpendInstant(float amount)
        {
            if (CurrentStamina < amount) return false;

            SetStamina(CurrentStamina - amount);
            _timeSinceLastDrain = 0f;
            return true;
        }

        public bool HasAtLeast(float amount) => CurrentStamina >= amount;

        public void Tick(float deltaTime, bool drainedThisFrame)
        {
            if (drainedThisFrame)
            {
                _timeSinceLastDrain = 0f;
                return;
            }

            _timeSinceLastDrain += deltaTime;
            if (_timeSinceLastDrain >= settings.staminaRegenDelay && CurrentStamina < MaxStamina)
            {
                SetStamina(CurrentStamina + settings.staminaRegenRate * deltaTime);
            }
        }

        private void SetStamina(float value)
        {
            CurrentStamina = Mathf.Clamp(value, 0f, MaxStamina);
            OnStaminaChanged?.Invoke(Normalized01);

            if (IsDepleted && !_wasDepleted)
            {
                _wasDepleted = true;
                OnStaminaDepleted?.Invoke();
            }
            else if (!IsDepleted && _wasDepleted)
            {
                _wasDepleted = false;
                OnStaminaRecovered?.Invoke();
            }
        }
    }
}