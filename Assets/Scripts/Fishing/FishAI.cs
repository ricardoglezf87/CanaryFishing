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
        [SerializeField, Min(0.01f)] private float minimumSwimmingDepth = 0.25f;
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
        [SerializeField, Min(0.1f)] private float maxHookDistance = 1.25f;
        [SerializeField, Min(0.1f)] private float hookedFollowSpeed = 5f;
        [SerializeField, Min(0.1f)] private float maxSwimSpeed = 4f;

        [Header("Inspector events")]
        [SerializeField] private FishCaughtUnityEvent fishCaught = new FishCaughtUnityEvent();

        private float remainingStamina;
        private float biteTimer;
        private float fightTimer;
        private float currentPullForce;
        private Rigidbody lureRigidbody;

        public event Action<FishData> OnFishCaughtEvent;

        public FishAIState State { get; private set; } = FishAIState.Attracted;
        public FishData Data => fishData;
        public float RemainingStamina => remainingStamina;
        public float CurrentPullForce => currentPullForce;

        public void Initialize(FishData data, FishingRodController rod, Transform targetLure, Rigidbody body)
        {
            Initialize(data, rod, targetLure, body, waterSurfaceY);
        }

        public void Initialize(FishData data, FishingRodController rod, Transform targetLure, Rigidbody body, float surfaceY)
        {
            ApplyRuntimeDefaults();
            fishData = data;
            fishingRod = rod;
            lure = targetLure;
            fishRigidbody = body;
            waterSurfaceY = surfaceY;
            lureRigidbody = targetLure != null ? targetLure.GetComponent<Rigidbody>() : null;
            remainingStamina = data != null ? data.Stamina : 0f;
            currentPullForce = data != null ? data.PullForce : 0f;
        }

        private void ApplyRuntimeDefaults()
        {
            if (depthTolerance <= 0f) depthTolerance = 1f;
            if (attractionDistance <= 0f) attractionDistance = 1.5f;
            if (attractionSpeed <= 0f) attractionSpeed = 1f;
            if (biteCheckInterval <= 0f) biteCheckInterval = 0.5f;
            if (fightImpulse <= 0f) fightImpulse = 1.4f;
            if (fightImpulseInterval <= 0f) fightImpulseInterval = 1.1f;
            if (staminaDrainPerSecond <= 0f) staminaDrainPerSecond = 18f;
            if (exhaustedThreshold <= 0f) exhaustedThreshold = 0.1f;
            if (exhaustedPullMultiplier <= 0f) exhaustedPullMultiplier = 0.25f;
            if (catchDistance <= 0f) catchDistance = 2f;
            if (maxHookDistance <= 0f) maxHookDistance = 1.25f;
            if (hookedFollowSpeed <= 0f) hookedFollowSpeed = 5f;
            if (maxSwimSpeed <= 0f) maxSwimSpeed = 4f;
        }

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

        private void FixedUpdate()
        {
            if (fishData == null || fishRigidbody == null ||
                (State != FishAIState.Fighting && State != FishAIState.Exhausted))
            {
                return;
            }

            KeepFishInWater();
            KeepFishAttachedToLure();

            if (fishRigidbody.linearVelocity.sqrMagnitude > maxSwimSpeed * maxSwimSpeed)
            {
                fishRigidbody.linearVelocity = fishRigidbody.linearVelocity.normalized * maxSwimSpeed;
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

            if (fishingRod.CastPoint != null &&
                Vector3.Distance(lure.position, fishingRod.CastPoint.position) <= catchDistance &&
                fishingRod.ReelInput > 0f)
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
            if (fishRigidbody != null)
            {
                fishRigidbody.linearVelocity = Vector3.zero;
                fishRigidbody.angularVelocity = Vector3.zero;
                fishRigidbody.isKinematic = true;
            }
            enabled = false;
            gameObject.SetActive(false);
            return caughtFish;
        }

        private bool IsDepthCompatible()
        {
            float lureDepth = waterSurfaceY - lure.position.y;
            return Mathf.Abs(lureDepth - fishData.PreferredDepth) <= depthTolerance;
        }

        private void MoveTowardsLure()
        {
            Vector3 target = lure.position;
            // Puede subir para buscar el señuelo, pero nunca salir del agua.
            target.y = Mathf.Min(target.y, waterSurfaceY - minimumSwimmingDepth);
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                attractionSpeed * Time.deltaTime);
            Vector3 position = transform.position;
            position.y = Mathf.Min(position.y, waterSurfaceY - minimumSwimmingDepth);
            transform.position = position;
        }

        private void ApplyFightImpulse()
        {
            Vector2 horizontal = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 impulseDirection = new Vector3(horizontal.x, 0f, horizontal.y);

            if (fishRigidbody != null)
            {
                fishRigidbody.AddForce(impulseDirection * fightImpulse, ForceMode.Impulse);
            }
            if (lureRigidbody != null && fishingRod != null && fishingRod.ReelInput <= 0f)
            {
                lureRigidbody.AddForce(impulseDirection * fightImpulse * 0.35f, ForceMode.Impulse);
            }
        }

        private void KeepFishInWater()
        {
            float preferredY = waterSurfaceY - fishData.PreferredDepth;
            Vector3 position = fishRigidbody.position;
            position.y = Mathf.Clamp(position.y, preferredY - depthTolerance, preferredY + depthTolerance);
            fishRigidbody.position = position;

            Vector3 velocity = fishRigidbody.linearVelocity;
            velocity.y = 0f;
            fishRigidbody.linearVelocity = velocity;
        }

        private void KeepFishAttachedToLure()
        {
            if (lure == null) return;

            Vector3 lureToFish = fishRigidbody.position - lure.position;
            if (lureToFish.sqrMagnitude <= maxHookDistance * maxHookDistance) return;

            Vector3 target = lure.position + lureToFish.normalized * maxHookDistance;
            fishRigidbody.MovePosition(Vector3.MoveTowards(
                fishRigidbody.position,
                target,
                hookedFollowSpeed * Time.fixedDeltaTime));
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
