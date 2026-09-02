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

        public IReadOnlyList<FishInventoryEntry> Catches => catches;
        public event Action<FishData, FishInventoryEntry> OnInventoryChanged;

        public void Subscribe(FishAI fishAI)
        {
            if (fishAI != null)
            {
                fishAI.OnFishCaughtEvent += AddFish;
            }
        }

        public void Unsubscribe(FishAI fishAI)
        {
            if (fishAI != null)
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

            FishInventoryEntry entry = catches.Find(item => item.fish == fish);
            if (entry == null)
            {
                entry = new FishInventoryEntry { fish = fish };
                catches.Add(entry);
            }

            entry.quantity++;
            entry.totalWeight += fish.Weight;
            OnInventoryChanged?.Invoke(fish, entry);
        }
    }
}
