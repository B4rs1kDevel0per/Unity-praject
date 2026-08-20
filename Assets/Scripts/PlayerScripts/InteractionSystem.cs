using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveSim.Player
{
    public class InteractionSystem : MonoBehaviour
    {
        [Header("Зависимости")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float interactDistance = 2.2f;
        [SerializeField] private LayerMask interactableMask = ~0;

        private Interaction.IInteractable _currentInteractable;
        public Interaction.IInteractable CurrentInteractable => _currentInteractable;

        private void Update()
        {
            if (playerCamera == null) return;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableMask))
            {
                var interactable = hit.collider.GetComponentInParent<Interaction.IInteractable>();
                if (interactable != null && interactable.CanInteract(this))
                {
                    if (_currentInteractable != interactable)
                    {
                        _currentInteractable?.OnFocusLost();
                        _currentInteractable = interactable;
                        _currentInteractable.OnFocus();
                    }

                    if (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame))
                    {
                        _currentInteractable.Interact(this);
                    }
                    return;
                }
            }

            if (_currentInteractable != null)
            {
                _currentInteractable.OnFocusLost();
                _currentInteractable = null;
            }
        }
    }
}