using UnityEngine;

namespace CanaryFishing.Fishing
{
    /// <summary>Renderiza la línea entre la punta de la caña y el señuelo.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class FishingLineRenderer : MonoBehaviour
    {
        [SerializeField] private Transform rodTip;
        [SerializeField] private Transform lure;
        [SerializeField, Min(0.001f)] private float lineWidth = 0.015f;
        [SerializeField] private Color lineColor = Color.white;

        private LineRenderer line;

        public void Initialize(Transform tip, Transform targetLure)
        {
            rodTip = tip;
            lure = targetLure;
            EnsureLineRenderer();
        }

        private void Awake() => EnsureLineRenderer();

        private void LateUpdate()
        {
            if (line == null || rodTip == null || lure == null)
            {
                if (line != null) line.enabled = false;
                return;
            }

            line.enabled = true;
            line.SetPosition(0, rodTip.position);
            line.SetPosition(1, lure.position);
        }

        private void EnsureLineRenderer()
        {
            if (line != null) return;
            line = GetComponent<LineRenderer>();
            if (line == null) line = gameObject.AddComponent<LineRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            line.sharedMaterial = new Material(shader);
            line.positionCount = 2;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.startColor = new Color(0.82f, 0.86f, 0.88f, 0.9f);
            line.endColor = new Color(0.65f, 0.7f, 0.72f, 0.75f);
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }
    }
}
