using UnityEngine;
using UnityEngine.UI;

namespace CanaryFishing.Fishing
{
    /// <summary>Presentación desacoplada de la tensión y del estado actual de la caña.</summary>
    public sealed class FishingTensionUI : MonoBehaviour
    {
        [SerializeField] private Slider tensionSlider;
        [SerializeField] private Image tensionFill;
        [SerializeField] private Text tensionText;
        [SerializeField] private Text stateText;
        [SerializeField] private GameObject overloadWarning;

        public void Initialize(FishingRodController rod)
        {
            EnsureControls();
            if (rod == null) return;
            rod.OnTensionChanged += UpdateTension;
            rod.OnStateChanged += UpdateState;
            rod.OnLineBreak += ShowLineBreak;
            UpdateState(rod.State);
            UpdateTension(rod.CurrentTension, rod.MaxTension);
        }

        private void UpdateTension(float tension, float maximum)
        {
            if (tensionSlider != null)
            {
                tensionSlider.normalizedValue = maximum > 0f ? tension / maximum : 0f;
            }

            float normalized = maximum > 0f ? Mathf.Clamp01(tension / maximum) : 0f;
            if (tensionFill != null)
            {
                tensionFill.color = normalized >= 0.8f
                    ? Color.red
                    : normalized >= 0.5f
                        ? Color.yellow
                        : Color.green;
            }

            if (tensionText != null)
            {
                tensionText.text = $"Tensión: {tension:0.0} / {maximum:0.0}";
            }

            if (overloadWarning != null)
            {
                overloadWarning.SetActive(maximum > 0f && tension >= maximum * 0.8f);
            }
        }

        private void UpdateState(FishingRodState state)
        {
            if (stateText != null) stateText.text = $"Estado: {state}";
        }

        private void ShowLineBreak()
        {
            if (stateText != null) stateText.text = "Estado: Línea rota";
        }

        private void EnsureControls()
        {
            if (tensionSlider != null && tensionText != null && stateText != null) return;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }

            if (tensionText == null) tensionText = CreateText(canvas.transform, "Tensión: 0 / 20", new Vector2(20f, -20f));
            if (stateText == null) stateText = CreateText(canvas.transform, "Estado: Idle", new Vector2(20f, -55f));
            if (tensionSlider == null)
            {
                GameObject sliderObject = new GameObject("Tension Bar");
                sliderObject.transform.SetParent(canvas.transform, false);
                RectTransform rect = sliderObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = new Vector2(20f, -90f);
                rect.sizeDelta = new Vector2(320f, 24f);
                sliderObject.AddComponent<Image>().color = Color.gray;
                tensionSlider = sliderObject.AddComponent<Slider>();
                GameObject fillObject = new GameObject("Fill");
                fillObject.transform.SetParent(sliderObject.transform, false);
                RectTransform fillRect = fillObject.AddComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
                tensionFill = fillObject.AddComponent<Image>();
                tensionSlider.fillRect = fillRect;
            }
        }

        private static Text CreateText(Transform parent, string value, Vector2 position)
        {
            GameObject textObject = new GameObject(value);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(600f, 30f);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18; text.color = Color.white; text.text = value;
            return text;
        }
    }
}
