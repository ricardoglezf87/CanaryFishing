using UnityEngine;

namespace CanaryFishing.Fishing
{
    /// <summary>
    /// Datos de diseño de una especie de pez.
    /// Crear assets desde: Assets > Create > Canary Fishing > Fish Data.
    /// </summary>
    [CreateAssetMenu(fileName = "FishData", menuName = "Canary Fishing/Fish Data")]
    public sealed class FishData : ScriptableObject
    {
        [SerializeField] private string fishName = "New Fish";
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField, Min(0f)] private float pullForce = 5f;
        [SerializeField, Min(0f)] private float stamina = 100f;
        [SerializeField, Min(0f)] private float preferredDepth = 2f;
        [SerializeField, Range(0f, 1f)] private float biteProbability = 0.25f;

        public string FishName => fishName;
        public float Weight => weight;
        public float PullForce => pullForce;
        public float Stamina => stamina;
        public float PreferredDepth => preferredDepth;
        public float BiteProbability => biteProbability;
    }
}
