using System;
using UnityEngine;
using UnityEngine.Events;

namespace CanaryFishing.Fishing
{
    public enum FishAIState
    {
        Attracted,
        Fighting,
        Exhausted
    }

    /// <summary>
    /// IA local de un pez. La instancia conserva el estado de stamina de la captura;
    /// FishData permanece como asset compartido y no se modifica durante la partida.
    /// </summary>
    public sealed class FishAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FishData fishData;
        [SerializeField] private FishingRodController fishingRod;
        [SerializeField] private Transform lure;
        [SerializeField] private Rigidbody fishRigidbody;

        [Header("Water")]
        [SerializeField] private float waterSurfaceY;
        [SerializeField, Min(0f)] private float depthTolerance = 1f;
        [SerializeField, Min(0f)] private float attractionDistance = 1.5f;
        [SerializeField, Min(0f)] private float attractionSpeed = 1f;

        [Header("Bite")]
        [SerializeField, Min(0.01f)] private float biteCheckInterval = 0.5f;

        [Header("Fight")]
        [SerializeField, Min(0f)] private float fightImpulse = 2f;
        [SerializeField, Min(0f)] private float fightImpulseInterval = 1.25f;
        [SerializeField, Min(0f)] private float staminaDrainPerSecond = 12f;
        [SerializeField, Range(0f, 1f)] private float exhaustedThreshold = 0.1f;
        [SerializeField, Range(0f, 1f)] private float exhaustedPullMultiplier = 0.25f;
        [SerializeField, Min(0f)] private float catchDistance = 2f;

        [Header("Inspector events")]
        [SerializeField] private FishCaughtUnityEvent fishCaught = new FishCaughtUnityEvent();

        private float remainingStamina;
        private float biteTimer;
        private float fightTimer;
        private float currentPullForce;

        public event Action<FishData> OnFishCaughtEvent;

        public FishAIState State { get; private set; } = FishAIState.Attracted;
        public FishData Data => fishData;
        public float RemainingStamina => remainingStamina;
        public float CurrentPullForce => currentPullForce;

        private void Awake()
        {
            remainingStamina = fishData != null ? fishData.Stamina : 0f;
            currentPullForce = fishData != null ? fishData.PullForce : 0f;
        }

        private void Update()
        {
            if (fishData == null || lure == null)
            {
                return;
            }

            switch (State)
            {
                case FishAIState.Attracted:
                    UpdateAttracted();
                    break;
                case FishAIState.Fighting:
                    UpdateFighting();
                    break;
                case FishAIState.Exhausted:
                    UpdateExhausted();
                    break;
            }
        }

        private void UpdateAttracted()
        {
            if (fishingRod == null || !IsDepthCompatible())
            {
                return;
            }

            MoveTowardsLure();
            biteTimer -= Time.deltaTime;

            if (biteTimer > 0f || Vector3.Distance(transform.position, lure.position) > attractionDistance)
            {
                return;
            }

            biteTimer = biteCheckInterval;
            if (UnityEngine.Random.value > fishData.BiteProbability)
            {
                return;
            }

            if (fishingRod.TryHookFish(currentPullForce))
            {
                SetState(FishAIState.Fighting);
            }
        }

        private void UpdateFighting()
        {
            if (fishingRod == null)
            {
                return;
            }

            fightTimer -= Time.deltaTime;
            if (fightTimer <= 0f)
            {
                fightTimer = fightImpulseInterval;
                ApplyFightImpulse();
            }

            float resistance = fishingRod.CurrentTension;
            float normalizedResistance = fishingRod.CurrentTension > 0f
                ? Mathf.Clamp01(resistance / Mathf.Max(0.01f, GetMaxTension()))
                : 0f;

            remainingStamina = Mathf.Max(0f, remainingStamina - normalizedResistance * staminaDrainPerSecond * Time.deltaTime);
            currentPullForce = Mathf.Max(0f, fishData.PullForce * (0.5f + remainingStamina / Mathf.Max(0.01f, fishData.Stamina) * 0.5f));
            fishingRod.SetFishPullForce(currentPullForce);

            if (remainingStamina <= fishData.Stamina * exhaustedThreshold)
            {
                SetState(FishAIState.Exhausted);
            }
        }

        private void UpdateExhausted()
        {
            if (fishingRod == null)
            {
                return;
            }

            currentPullForce = fishData.PullForce * exhaustedPullMultiplier;
            fishingRod.SetFishPullForce(currentPullForce);

            if (Vector3.Distance(transform.position, lure.position) <= catchDistance && fishingRod.ReelInput > 0f)
            {
                OnFishCaught();
            }
        }

        /// <summary>
        /// Finaliza la captura y devuelve los datos de la especie para el inventario.
        /// El inventario puede suscribirse a OnFishCaughtEvent sin acoplarse a esta IA.
        /// </summary>
        public FishData OnFishCaught()
        {
            if (fishData == null || State != FishAIState.Exhausted)
            {
                return null;
            }

            FishData caughtFish = fishData;
            if (fishingRod != null)
            {
                fishingRod.ReleaseFish();
            }
            OnFishCaughtEvent?.Invoke(caughtFish);
            fishCaught.Invoke(caughtFish);
            enabled = false;
            return caughtFish;
        }

        private bool IsDepthCompatible()
        {
            float lureDepth = waterSurfaceY - lure.position.y;
            return Mathf.Abs(lureDepth - fishData.PreferredDepth) <= depthTolerance;
        }

        private void MoveTowardsLure()
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                lure.position,
                attractionSpeed * Time.deltaTime);
        }

        private void ApplyFightImpulse()
        {
            Vector3 impulseDirection = UnityEngine.Random.onUnitSphere;
            impulseDirection.y = Mathf.Clamp(impulseDirection.y, -0.35f, 0.35f);
            impulseDirection.Normalize();

            if (fishRigidbody != null)
            {
                fishRigidbody.AddForce(impulseDirection * fightImpulse, ForceMode.Impulse);
            }
        }

        private float GetMaxTension()
        {
            return fishingRod != null
                ? fishingRod.MaxTension
                : Mathf.Max(fishData.PullForce, currentPullForce);
        }

        private void SetState(FishAIState newState)
        {
            State = newState;
            fightTimer = fightImpulseInterval;
        }

        [Serializable]
        private sealed class FishCaughtUnityEvent : UnityEvent<FishData>
        {
        }
    }
}
