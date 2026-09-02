using System;
using UnityEngine;

namespace CanaryFishing.Fishing
{
    /// <summary>Orquesta la sesión y conecta la captura con el inventario.</summary>
    public sealed class FishingSessionController : MonoBehaviour
    {
        [SerializeField] private FishingRodController rod;
        [SerializeField] private FishAI fish;
        [SerializeField] private FishingInputController input;
        [SerializeField] private FishingTensionUI tensionUI;
        [SerializeField] private PlayerInventory inventory;

        public event Action<FishData> OnFishCaught;

        public void Initialize(FishingRodController targetRod, FishAI targetFish, FishingInputController targetInput,
            FishingTensionUI targetUI, PlayerInventory targetInventory)
        {
            rod = targetRod;
            fish = targetFish;
            input = targetInput;
            tensionUI = targetUI;
            inventory = targetInventory;
        }

        private void Start()
        {
            if (fish != null)
            {
                fish.OnFishCaughtEvent += HandleFishCaught;
                inventory?.Subscribe(fish);
            }

            input?.Initialize(rod);
            tensionUI?.Initialize(rod);
        }

        private void HandleFishCaught(FishData caughtFish) => OnFishCaught?.Invoke(caughtFish);

        private void OnDestroy()
        {
            if (fish != null)
            {
                fish.OnFishCaughtEvent -= HandleFishCaught;
                inventory?.Unsubscribe(fish);
            }
        }
    }
}
