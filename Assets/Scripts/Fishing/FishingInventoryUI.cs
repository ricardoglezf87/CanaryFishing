using UnityEngine;
using UnityEngine.UI;

namespace CanaryFishing.Fishing
{
    /// <summary>Lista las capturas acumuladas por el jugador en el HUD.</summary>
    public sealed class FishingInventoryUI : MonoBehaviour
    {
        [SerializeField] private Text catchesText;

        public void Initialize(PlayerInventory inventory)
        {
            EnsureText();
            if (inventory == null) return;
            inventory.OnInventoryChanged += HandleInventoryChanged;
            Refresh(inventory);
        }

        private void HandleInventoryChanged(FishData fish, FishInventoryEntry entry)
        {
            if (catchesText != null)
            {
                catchesText.text = $"Captura: {fish.FishName} | {fish.Weight:0.0} kg | Cantidad: {entry.quantity}";
            }
        }

        private void Refresh(PlayerInventory inventory)
        {
            if (catchesText != null)
            {
                catchesText.text = inventory.Catches.Count == 0
                    ? "Capturas: ninguna"
                    : $"Capturas: {inventory.Catches.Count}";
            }
        }

        private void OnDestroy()
        {
            // El inventario se destruye normalmente junto con esta UI en el demo.
        }

        private void EnsureText()
        {
            if (catchesText != null) return;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            GameObject textObject = new GameObject("Catches");
            textObject.transform.SetParent(canvas.transform, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = new Vector2(20f, -130f);
            rect.sizeDelta = new Vector2(600f, 30f);
            catchesText = textObject.AddComponent<Text>();
            catchesText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            catchesText.fontSize = 18; catchesText.color = Color.white;
        }
    }
}
