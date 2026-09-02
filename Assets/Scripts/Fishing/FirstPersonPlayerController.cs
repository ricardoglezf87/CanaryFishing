using UnityEngine;
using UnityEngine.InputSystem;

namespace CanaryFishing.Fishing
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonPlayerController : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(0.1f)] private float moveSpeed = 4f;
        [SerializeField, Min(0.01f)] private float lookSensitivity = 0.08f;
        [SerializeField] private Vector2 pitchLimits = new Vector2(-70f, 80f);
        [SerializeField, Min(0f)] private float gravity = 18f;
        private CharacterController controller;
        private float pitch;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (viewCamera == null) viewCamera = Camera.main;
        }

        private void Update()
        {
            if (Keyboard.current == null || viewCamera == null) return;
            Vector2 move = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) move.y += 1f;
            if (Keyboard.current.sKey.isPressed) move.y -= 1f;
            if (Keyboard.current.dKey.isPressed) move.x += 1f;
            if (Keyboard.current.aKey.isPressed) move.x -= 1f;
            Vector3 direction = (transform.right * move.x + transform.forward * move.y).normalized;
            controller.Move(direction * moveSpeed * Time.deltaTime + Vector3.down * gravity * Time.deltaTime);
            if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = Mouse.current.delta.ReadValue() * lookSensitivity;
                transform.Rotate(Vector3.up, look.x);
                pitch = Mathf.Clamp(pitch - look.y, pitchLimits.x, pitchLimits.y);
                viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }
    }
}
