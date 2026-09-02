using System;
using UnityEngine;

namespace CanaryFishing.Fishing
{
    public enum FishingEquipmentType { Rod, Reel, Line, Leader, Hook, Bait, Lure }
    public enum RodPower { L, ML, M, MH, H }
    public enum LineMaterial { Monofilament, Fluorocarbon, Braided }

    [CreateAssetMenu(fileName = "FishingEquipment", menuName = "Canary Fishing/Equipment")]
    public sealed class FishingEquipment : ScriptableObject
    {
        [SerializeField] private string displayName = "Equipment";
        [SerializeField] private FishingEquipmentType type;
        [SerializeField] private RodPower rodPower = RodPower.M;
        [SerializeField] private LineMaterial lineMaterial = LineMaterial.Monofilament;
        [SerializeField, Min(0f)] private float durabilityKg = 10f;
        [SerializeField, Min(0f)] private float lineCapacityM = 100f;
        [SerializeField, Min(0f)] private float retrieveRatio = 5f;
        [SerializeField, Range(0f, 1f)] private float maxDrag = 0.6f;
        [SerializeField, Min(1)] private int hookSize = 4;
        [SerializeField, Min(0f)] private float weightGrams = 10f;
        [SerializeField] private string[] preferredFish = Array.Empty<string>();

        public string DisplayName => displayName;
        public FishingEquipmentType Type => type;
        public RodPower RodPower => rodPower;
        public LineMaterial LineMaterial => lineMaterial;
        public float DurabilityKg => durabilityKg;
        public float LineCapacityM => lineCapacityM;
        public float RetrieveRatio => retrieveRatio;
        public float MaxDrag => maxDrag;
        public int HookSize => hookSize;
        public float WeightGrams => weightGrams;
        public string[] PreferredFish => preferredFish;
    }

    [Serializable]
    public struct EquipmentSlot
    {
        public FishingEquipment item;
        public int quantity;
        public EquipmentSlot(FishingEquipment value, int count) { item = value; quantity = count; }
    }

    public sealed class TackleLoadout : MonoBehaviour
    {
        [SerializeField] private FishingEquipment rod;
        [SerializeField] private FishingEquipment reel;
        [SerializeField] private FishingEquipment line;
        [SerializeField] private FishingEquipment leader;
        [SerializeField] private FishingEquipment hook;
        [SerializeField] private FishingEquipment bait;
        [SerializeField] private FishingEquipment lure;

        public FishingEquipment Rod => rod;
        public FishingEquipment Reel => reel;
        public FishingEquipment Line => line;
        public FishingEquipment Leader => leader;
        public FishingEquipment Hook => hook;
        public FishingEquipment Bait => bait;
        public FishingEquipment Lure => lure;

        public bool Validate(out string error)
        {
            if (rod == null || rod.Type != FishingEquipmentType.Rod) { error = "Falta una caña válida."; return false; }
            if (reel == null || reel.Type != FishingEquipmentType.Reel) { error = "Falta un carrete válido."; return false; }
            if (line == null || (line.Type != FishingEquipmentType.Line && line.Type != FishingEquipmentType.Leader)) { error = "Falta una línea válida."; return false; }
            if (hook == null || hook.Type != FishingEquipmentType.Hook) { error = "Falta un anzuelo válido."; return false; }
            if (bait == null && lure == null) { error = "Monta un cebo o señuelo."; return false; }
            if (line.DurabilityKg > 0f && rod.DurabilityKg > 0f && line.DurabilityKg > rod.DurabilityKg * 2.5f) { error = "La línea supera la compatibilidad de la caña."; return false; }
            if (reel.LineCapacityM <= 0f) { error = "El carrete no tiene capacidad de línea."; return false; }
            error = string.Empty;
            return true;
        }

        public float GetWeakestDurability(float fallback) {
            float result = fallback;
            FishingEquipment[] parts = { rod, reel, line, leader, hook };
            foreach (FishingEquipment part in parts) if (part != null && part.DurabilityKg > 0f) result = Mathf.Min(result, part.DurabilityKg);
            return result;
        }

        public float GetDragLimit(float fallback) => reel != null && reel.MaxDrag > 0f ? reel.MaxDrag : fallback;
    }
}
