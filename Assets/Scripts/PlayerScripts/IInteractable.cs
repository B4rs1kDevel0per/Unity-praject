using UnityEngine;

namespace ImmersiveSim.Interaction
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract(MonoBehaviour interactor);
        void Interact(MonoBehaviour interactor);
        void OnFocus();
        void OnFocusLost();
    }
}