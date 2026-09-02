using UnityEngine;

namespace CanaryFishing.Fishing
{
    /// <summary>HUD de prueba garantizado en la Game View, independiente de un Canvas 3D.</summary>
    public sealed class FishingRuntimeHUD : MonoBehaviour
    {
        private FishingRodController rod;
        private PlayerInventory inventory;
        private string lastCatch = "Capturas: ninguna";

        public void Initialize(FishingRodController targetRod, PlayerInventory targetInventory)
        {
            rod = targetRod;
            inventory = targetInventory;
            if (inventory != null) inventory.OnInventoryChanged += OnInventoryChanged;
        }

        private void OnGUI()
        {
            if (rod == null) return;
            float tension = rod.CurrentTension;
            float maximum = Mathf.Max(0.01f, rod.MaxTension);
            float normalized = Mathf.Clamp01(tension / maximum);
            Color previousColor = GUI.color;

            GUI.color = Color.white;
            GUI.Label(new Rect(20f, 20f, 500f, 28f), $"Tensión: {tension:0.0} / {maximum:0.0}");
            GUI.Label(new Rect(20f, 48f, 500f, 28f), $"Estado: {rod.State}");
            GUI.Label(new Rect(20f, 76f, 700f, 28f), "SPACE: cargar/lanzar   |   R: recoger línea");
            GUI.color = normalized >= 0.8f ? Color.red : normalized >= 0.5f ? Color.yellow : Color.green;
            GUI.HorizontalSlider(new Rect(20f, 108f, 320f, 24f), normalized, 0f, 1f);
            GUI.color = Color.white;
            GUI.Label(new Rect(20f, 138f, 700f, 28f), lastCatch);
            GUI.color = previousColor;
        }

        private void OnInventoryChanged(FishData fish, FishInventoryEntry entry)
        {
            lastCatch = $"Captura: {fish.FishName} | {fish.Weight:0.0} kg | Cantidad: {entry.quantity}";
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.OnInventoryChanged -= OnInventoryChanged;
        }
    }
}
