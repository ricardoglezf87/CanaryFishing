using System;
using System.Collections.Generic;
using UnityEngine;

namespace CanaryFishing.Fishing
{
    [Serializable]
    public sealed class FishInventoryEntry
    {
        public FishData fish;
        public int quantity;
        public float totalWeight;
    }

    /// <summary>Inventario mínimo que agrupa capturas por especie.</summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private List<FishInventoryEntry> catches = new List<FishInventoryEntry>();
        [SerializeField] private List<EquipmentSlot> equipmentSlots = new List<EquipmentSlot>();
        [SerializeField, Min(1)] private int maxSlots = 24;
        [SerializeField, Min(0f)] private float keepnetCapacityKg = 20f;
        [SerializeField, Min(0)] private int coins;
        private readonly HashSet<FishAI> subscribedFish = new HashSet<FishAI>();

        public IReadOnlyList<FishInventoryEntry> Catches => catches;
        public IReadOnlyList<EquipmentSlot> EquipmentSlots => equipmentSlots;
        public float KeepnetWeight { get; private set; }
        public float KeepnetCapacityKg => keepnetCapacityKg;
        public int Coins => coins;
        public event Action<FishData, FishInventoryEntry> OnInventoryChanged;

        public void Subscribe(FishAI fishAI)
        {
            if (fishAI != null && subscribedFish.Add(fishAI))
            {
                fishAI.OnFishCaughtEvent += AddFish;
            }
        }

        public void Unsubscribe(FishAI fishAI)
        {
            if (fishAI != null && subscribedFish.Remove(fishAI))
            {
                fishAI.OnFishCaughtEvent -= AddFish;
            }
        }

        public void AddFish(FishData fish)
        {
            if (fish == null)
            {
                return;
            }

            if (KeepnetWeight + fish.Weight > keepnetCapacityKg) return;
            FishInventoryEntry entry = catches.Find(item => item.fish == fish);
            if (entry == null)
            {
                entry = new FishInventoryEntry { fish = fish };
                catches.Add(entry);
            }

            entry.quantity++;
            entry.totalWeight += fish.Weight;
            KeepnetWeight += fish.Weight;
            OnInventoryChanged?.Invoke(fish, entry);
        }

        public bool AddEquipment(FishingEquipment item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;
            for (int i = 0; i < equipmentSlots.Count; i++)
                if (equipmentSlots[i].item == item) { EquipmentSlot slot = equipmentSlots[i]; slot.quantity += quantity; equipmentSlots[i] = slot; return true; }
            if (equipmentSlots.Count >= maxSlots) return false;
            equipmentSlots.Add(new EquipmentSlot(item, quantity));
            return true;
        }

        public bool SellFish(FishData fish, int quantity = 1)
        {
            FishInventoryEntry entry = catches.Find(item => item.fish == fish);
            if (entry == null || quantity <= 0 || entry.quantity < quantity) return false;
            entry.quantity -= quantity; entry.totalWeight -= fish.Weight * quantity; KeepnetWeight -= fish.Weight * quantity;
            coins += Mathf.RoundToInt(fish.Value * quantity);
            if (entry.quantity == 0) catches.Remove(entry);
            OnInventoryChanged?.Invoke(fish, entry);
            return true;
        }
    }
}
