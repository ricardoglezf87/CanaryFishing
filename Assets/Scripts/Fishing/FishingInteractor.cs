using UnityEngine;
using UnityEngine.InputSystem;

namespace CanaryFishing.Fishing
{
    public interface IFishingInteractable { void Interact(PlayerInventory inventory); }

    public sealed class FishingInteractor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(0.1f)] private float range = 3f;
        [SerializeField] private PlayerInventory inventory;
        private void Awake() { if (viewCamera == null) viewCamera = Camera.main; }
        private void Update()
        {
            if (Keyboard.current == null || viewCamera == null || !Keyboard.current.eKey.wasPressedThisFrame) return;
            if (!Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward, out RaycastHit hit, range)) return;
            foreach (MonoBehaviour component in hit.collider.GetComponentsInParent<MonoBehaviour>())
            {
                if (component is IFishingInteractable interactable) { interactable.Interact(inventory); break; }
            }
        }
    }
}
