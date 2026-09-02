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
        [SerializeField] private FishingRodController rod;

        private float castCharge;
        private bool charging;

        public void Initialize(FishingRodController targetRod) => rod = targetRod;

        private void Update()
        {
            if (rod == null || Keyboard.current == null)
            {
                return;
            }

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
    }
}
