using UnityEngine;

namespace CanaryFishing.Fishing
{
    /// <summary>
    /// Simula y renderiza el hilo como una cuerda Verlet.
    /// Los extremos están anclados a rodTip y lure; los puntos intermedios
    /// caen por gravedad y las restricciones mantienen la longitud del hilo.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class FishingLineRenderer : MonoBehaviour
    {
        [SerializeField] private Transform rodTip;
        [SerializeField] private Transform lure;
        [Header("Verlet rope")]
        [SerializeField, Min(2)] private int segmentCount = 24;
        [SerializeField, Min(1)] private int constraintIterations = 8;
        [SerializeField, Min(0f)] private float lineLength;
        [SerializeField, Range(0f, 0.5f)] private float initialSlack = 0.12f;
        [SerializeField] private Vector3 gravity = new Vector3(0f, -9.81f, 0f);
        [SerializeField, Range(0f, 1f)] private float damping = 0.985f;

        [Header("Rendering")]
        [SerializeField, Min(0.001f)] private float lineWidth = 0.015f;
        [SerializeField] private Color lineColor = Color.white;

        private LineRenderer line;
        private Vector3[] points;
        private Vector3[] previousPoints;
        private float segmentLength;
        private bool initialized;
        private bool isTaut;

        public bool IsTaut => isTaut;
        public float Extension { get; private set; }

        public void Initialize(Transform tip, Transform targetLure)
        {
            rodTip = tip;
            lure = targetLure;
            initialized = false;
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
            EnsureSimulation(rodTip.position, lure.position);
            Simulate(Time.deltaTime, rodTip.position, lure.position);
            line.positionCount = points.Length;
            line.SetPositions(points);
        }

        private void EnsureSimulation(Vector3 start, Vector3 end)
        {
            int pointCount = Mathf.Max(2, segmentCount + 1);
            if (points != null && points.Length == pointCount && initialized) return;

            points = new Vector3[pointCount];
            previousPoints = new Vector3[pointCount];
            float distance = Vector3.Distance(start, end);
            float totalLength = lineLength > 0f
                ? Mathf.Max(lineLength, distance)
                : Mathf.Max(0.001f, distance * (1f + initialSlack));
            segmentLength = totalLength / (pointCount - 1);

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                points[i] = Vector3.Lerp(start, end, t);
                previousPoints[i] = points[i];
            }
            initialized = true;
        }

        private void Simulate(float deltaTime, Vector3 start, Vector3 end)
        {
            float step = Mathf.Clamp(deltaTime, 0f, 1f / 30f);
            float totalLength = segmentLength * (points.Length - 1);
            float endpointDistance = Vector3.Distance(start, end);

            // Cuando los anclajes piden más longitud de la disponible, no se
            // permite que la proyección de restricciones genere artefactos:
            // el hilo queda recto y ofrece una resistencia rígida.
            if (endpointDistance >= totalLength)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    float t = i / (float)(points.Length - 1);
                    points[i] = Vector3.Lerp(start, end, t);
                    previousPoints[i] = points[i];
                }
                Extension = 1f;
                isTaut = true;
                return;
            }

            for (int i = 1; i < points.Length - 1; i++)
            {
                Vector3 current = points[i];
                Vector3 velocity = (points[i] - previousPoints[i]) * damping;
                points[i] += velocity + gravity * (step * step);
                previousPoints[i] = current;
            }

            // Proyección iterativa: al consumir el largo disponible, el hilo
            // se comporta como una conexión prácticamente inextensible.
            for (int iteration = 0; iteration < Mathf.Max(1, constraintIterations); iteration++)
            {
                points[0] = start;
                points[points.Length - 1] = end;
                for (int i = 0; i < points.Length - 1; i++)
                {
                    Vector3 delta = points[i + 1] - points[i];
                    float distanceSquared = delta.sqrMagnitude;
                    if (distanceSquared < 0.000001f) continue;

                    float distance = Mathf.Sqrt(distanceSquared);
                    Vector3 correction = delta * ((distance - segmentLength) / distance);
                    bool firstPinned = i == 0;
                    bool secondPinned = i + 1 == points.Length - 1;
                    if (firstPinned)
                        points[i + 1] -= correction;
                    else if (secondPinned)
                        points[i] += correction;
                    else
                    {
                        points[i] += correction * 0.5f;
                        points[i + 1] -= correction * 0.5f;
                    }
                }
            }

            points[0] = start;
            points[points.Length - 1] = end;
            Extension = Mathf.Clamp01(endpointDistance / Mathf.Max(0.001f, totalLength));
            isTaut = Extension >= 0.995f;
        }

        private void EnsureLineRenderer()
        {
            if (line != null) return;
            line = GetComponent<LineRenderer>();
            if (line == null) line = gameObject.AddComponent<LineRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            line.sharedMaterial = new Material(shader);
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.startColor = lineColor;
            line.endColor = lineColor;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }
    }
}
