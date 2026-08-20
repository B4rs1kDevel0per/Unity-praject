using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveSim.Player
{
    public class PlayerLean : MonoBehaviour
    {
        [Header("Зависимости")]
        [SerializeField] private Data.PlayerSettings settings;
        [SerializeField] private Transform leanPivot;

        private float _currentLeanT = 0f;
        public event Action OnLeanStarted;

        private void Update()
        {
            float dt = Time.deltaTime;
            float targetLean = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.qKey.isPressed) targetLean -= 1f;
                if (Keyboard.current.eKey.isPressed) targetLean += 1f;
            }

            if (Mathf.Abs(targetLean) > 0.01f && Mathf.Abs(_currentLeanT) < 0.01f)
            {
                OnLeanStarted?.Invoke();
            }

            _currentLeanT = Mathf.MoveTowards(_currentLeanT, targetLean, dt / Mathf.Max(0.01f, settings.leanSmoothTime));

            float clampedLeanT = _currentLeanT;
            if (Mathf.Abs(_currentLeanT) > 0.01f && leanPivot != null)
            {
                Vector3 leanDir = leanPivot.right * Mathf.Sign(_currentLeanT);
                if (Physics.SphereCast(leanPivot.position, settings.leanCastRadius, leanDir, out RaycastHit hit, settings.leanCastDistance, settings.leanObstacleMask))
                {
                    float allowedRatio = Mathf.Clamp01((hit.distance - 0.05f) / settings.leanCastDistance);
                    clampedLeanT *= allowedRatio;
                }
            }

            Vector3 targetPos = new Vector3(clampedLeanT * settings.leanPositionOffset, 0f, 0f);
            Quaternion targetRot = Quaternion.Euler(0f, 0f, -clampedLeanT * settings.leanAngle);

            if (leanPivot != null)
            {
                leanPivot.localPosition = Vector3.Lerp(leanPivot.localPosition, targetPos, 1f - Mathf.Exp(-15f * dt));
                leanPivot.localRotation = Quaternion.Slerp(leanPivot.localRotation, targetRot, 1f - Mathf.Exp(-15f * dt));
            }
        }
    }
}