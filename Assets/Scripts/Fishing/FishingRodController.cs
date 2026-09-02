using System;
using UnityEngine;
using UnityEngine.Events;

namespace CanaryFishing.Fishing
{
    /// <summary>
    /// Estados principales del equipo de pesca.
    /// </summary>
    public enum FishingRodState
    {
        Idle,
        Casting,
        WaitingForBite,
        Hooked,
        Reeling
    }

    /// <summary>
    /// Controla el lanzamiento, la recogida y la tensión de la línea.
    ///
    /// El pez no necesita conocer la implementación del carrete: puede informar
    /// de su fuerza mediante SetFishPullForce y reaccionar a los eventos expuestos.
    /// </summary>
    public sealed class FishingRodController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform castPoint;
        [SerializeField] private Rigidbody lureRigidbody;

        [Header("Casting")]
        [SerializeField, Min(0f)] private float minCastForce = 5f;
        [SerializeField, Min(0f)] private float maxCastForce = 25f;
        [SerializeField] private Vector3 castDirection = Vector3.forward;
        [SerializeField, Min(0f)] private float castingDuration = 0.25f;

        [Header("Reeling")]
        [SerializeField, Range(0f, 1f)] private float reelInput;
        [SerializeField, Min(0f)] private float reelForcePerSpeed = 8f;
        [SerializeField, Min(0f)] private float reelSpeed = 1f;

        [Header("Line tension")]
        [SerializeField, Min(0f)] private float maxTension = 20f;
        [SerializeField, Min(0f)] private float maxTensionDuration = 2f;
        [SerializeField, Min(0f)] private float tensionSmoothing = 10f;

        [Header("Inspector events")]
        [SerializeField] private FloatUnityEvent tensionChanged = new FloatUnityEvent();
        [SerializeField] private UnityEvent lineBreak = new UnityEvent();

        private float castingTimer;
        private float fishPullForce;
        private float currentTension;
        private float overTensionTimer;
        private bool lineBroken;

        /// <summary>Notifica cambios de tensión: tensión actual y valor máximo configurado.</summary>
        public event Action<float, float> OnTensionChanged;

        /// <summary>Se dispara cuando la tensión excede el límite durante el tiempo configurado.</summary>
        public event Action OnLineBreak;

        /// <summary>Notifica cualquier cambio de estado de la caña.</summary>
        public event Action<FishingRodState> OnStateChanged;

        public FishingRodState State { get; private set; } = FishingRodState.Idle;
        public float CurrentTension => currentTension;
        public float MaxTension => maxTension;
        public Transform CastPoint => castPoint;
        public float ReelInput => reelInput;
        public float FishPullForce => fishPullForce;

        public void Initialize(Transform targetCastPoint, Rigidbody targetLure)
        {
            castPoint = targetCastPoint;
            lureRigidbody = targetLure;
        }

        private void Update()
        {
            if (State == FishingRodState.Casting)
            {
                castingTimer -= Time.deltaTime;
                if (castingTimer <= 0f)
                {
                    SetState(FishingRodState.WaitingForBite);
                }
            }

            UpdateLureReel();
            UpdateTension(Time.deltaTime);
        }

        /// <summary>
        /// Lanza el señuelo. input debe estar normalizado entre 0 y 1.
        /// </summary>
        public void Cast(float input)
        {
            if (State != FishingRodState.Idle || lureRigidbody == null)
            {
                return;
            }

            float normalizedInput = Mathf.Clamp01(input);
            float force = Mathf.Lerp(minCastForce, maxCastForce, normalizedInput);
            Vector3 direction = GetCastDirection();

            lureRigidbody.position = castPoint != null ? castPoint.position : transform.position;
            lureRigidbody.linearVelocity = Vector3.zero;
            lureRigidbody.angularVelocity = Vector3.zero;
            lureRigidbody.AddForce(direction * force, ForceMode.VelocityChange);

            castingTimer = castingDuration;
            SetState(FishingRodState.Casting);
        }

        /// <summary>
        /// Actualiza la entrada del carrete. input debe estar normalizado entre 0 y 1.
        /// </summary>
        public void Reel(float input)
        {
            reelInput = Mathf.Clamp01(input);

            if (State == FishingRodState.Hooked || State == FishingRodState.Reeling)
            {
                SetState(reelInput > 0f ? FishingRodState.Reeling : FishingRodState.Hooked);
            }
        }

        private void UpdateLureReel()
        {
            if (lureRigidbody == null || castPoint == null || reelInput <= 0f ||
                (State != FishingRodState.WaitingForBite && State != FishingRodState.Hooked && State != FishingRodState.Reeling))
            {
                return;
            }

            Vector3 toRod = castPoint.position - lureRigidbody.position;
            float reelDistance = reelSpeed * reelInput * Time.deltaTime;
            lureRigidbody.MovePosition(Vector3.MoveTowards(lureRigidbody.position, castPoint.position, reelDistance));

            if (State == FishingRodState.WaitingForBite && toRod.magnitude <= 0.1f)
            {
                ReleaseFish();
            }
        }

        /// <summary>
        /// Engancha un pez y registra la fuerza que está aplicando a la línea.
        /// </summary>
        public bool TryHookFish(float initialPullForce)
        {
            if (State != FishingRodState.WaitingForBite)
            {
                return false;
            }

            fishPullForce = Mathf.Max(0f, initialPullForce);
            lineBroken = false;
            overTensionTimer = 0f;
            SetState(FishingRodState.Hooked);
            return true;
        }

        /// <summary>
        /// Permite a la IA del pez cambiar su fuerza de tirón durante la lucha.
        /// </summary>
        public void SetFishPullForce(float pullForce)
        {
            fishPullForce = Mathf.Max(0f, pullForce);
        }

        /// <summary>Libera el pez y deja la caña lista para otro lanzamiento.</summary>
        public void ReleaseFish()
        {
            fishPullForce = 0f;
            reelInput = 0f;
            overTensionTimer = 0f;
            SetState(FishingRodState.Idle);
        }

        private void UpdateTension(float deltaTime)
        {
            float reelForce = reelInput * reelSpeed * reelForcePerSpeed;
            float targetTension = State == FishingRodState.Hooked || State == FishingRodState.Reeling
                ? Mathf.Max(0f, fishPullForce - reelForce)
                : 0f;

            currentTension = tensionSmoothing > 0f
                ? Mathf.MoveTowards(currentTension, targetTension, tensionSmoothing * deltaTime)
                : targetTension;

            OnTensionChanged?.Invoke(currentTension, maxTension);
            tensionChanged.Invoke(currentTension);

            // El temporizador usa la tensión objetivo para no retrasar la rotura
            // cuando la suavización visual mantiene la barra por debajo del límite.
            if (!lineBroken && targetTension > maxTension)
            {
                overTensionTimer += deltaTime;
                if (overTensionTimer >= maxTensionDuration)
                {
                    BreakLine();
                }
            }
            else
            {
                overTensionTimer = 0f;
            }
        }

        private void BreakLine()
        {
            fishPullForce = 0f;
            reelInput = 0f;
            currentTension = 0f;
            overTensionTimer = 0f;
            lineBroken = true;
            SetState(FishingRodState.Idle);

            OnLineBreak?.Invoke();
            lineBreak.Invoke();
        }

        private void SetState(FishingRodState newState)
        {
            if (State == newState)
            {
                return;
            }

            State = newState;
            OnStateChanged?.Invoke(State);
        }

        private Vector3 GetCastDirection()
        {
            Vector3 direction = castPoint != null ? castPoint.TransformDirection(castDirection) : transform.TransformDirection(castDirection);
            return direction.sqrMagnitude > 0f ? direction.normalized : transform.forward;
        }

        [Serializable]
        private sealed class FloatUnityEvent : UnityEvent<float>
        {
        }
    }
}
