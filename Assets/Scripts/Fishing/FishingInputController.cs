using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CanaryFishing.Fishing
{
    /// <summary>
    /// Entrada MVP basada en Input System: mantener Space carga el cast, soltarlo lanza;
    /// mantener R recoge el carrete. Las teclas se pueden cambiar desde el Inspector.
    /// </summary>
    public sealed class FishingInputController : MonoBehaviour
    {
        [SerializeField] private Key castKey = Key.Space;
        [SerializeField] private Key reelKey = Key.R;
        [SerializeField, Min(0.1f)] private float chargeTime = 1.5f;
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.08f;
        [SerializeField] private Vector2 pitchLimits = new Vector2(-55f, 70f);
        [SerializeField] private FishingRodController rod;

        private float castCharge;
        private bool charging;
        private Camera viewCamera;
        private float yaw;
        private float pitch;

        public void Initialize(FishingRodController targetRod)
        {
            rod = targetRod;
            viewCamera = Camera.main;
            if (viewCamera != null)
            {
                Vector3 angles = viewCamera.transform.eulerAngles;
                yaw = angles.y;
                pitch = angles.x > 180f ? angles.x - 360f : angles.x;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (rod == null || Keyboard.current == null)
            {
                return;
            }

            UpdateMouseLook();

            KeyControl cast = Keyboard.current[castKey];
            if (rod.State == FishingRodState.Idle && cast.wasPressedThisFrame)
            {
                charging = true;
                castCharge = 0f;
            }

            if (charging && cast.isPressed)
            {
                castCharge = Mathf.Clamp01(castCharge + Time.deltaTime / chargeTime);
            }

            if (charging && cast.wasReleasedThisFrame)
            {
                rod.Cast(castCharge);
                charging = false;
            }

            rod.Reel(Keyboard.current[reelKey].isPressed ? 1f : 0f);
        }

        private void UpdateMouseLook()
        {
            if (viewCamera == null || Mouse.current == null) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
            if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, pitchLimits.x, pitchLimits.y);
            viewCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
