using UnityEngine;
using UnityEngine.UI;

namespace CanaryFishing.Fishing
{
    /// <summary>Presentación desacoplada de la tensión y del estado actual de la caña.</summary>
    public sealed class FishingTensionUI : MonoBehaviour
    {
        [SerializeField] private Slider tensionSlider;
        [SerializeField] private Text tensionText;
        [SerializeField] private Text stateText;
        [SerializeField] private GameObject overloadWarning;

        public void Initialize(FishingRodController rod)
        {
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
    }
}
