using System;
using UnityEngine;

namespace ImmersiveSim.Systems
{
    public class AdrenalineController : MonoBehaviour
    {
        public bool IsAdrenalineActive { get; private set; }
        public event Action<bool> OnAdrenalineStateChanged;

        public void SetAdrenalineState(bool active)
        {
            if (IsAdrenalineActive == active) return;
            IsAdrenalineActive = active;
            OnAdrenalineStateChanged?.Invoke(IsAdrenalineActive);
        }
    }
}