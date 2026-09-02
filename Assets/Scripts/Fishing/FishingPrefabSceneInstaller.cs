using UnityEngine;

namespace CanaryFishing.Fishing
{
    /// <summary>
    /// Instala la escena usando prefabs asignados desde el Inspector. Permite cambiar
    /// caña, señuelo, pez, HUD o inventario sin modificar código.
    /// </summary>
    public sealed class FishingPrefabSceneInstaller : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject rodPrefab;
        [SerializeField] private GameObject lurePrefab;
        [SerializeField] private GameObject fishPrefab;
        [SerializeField] private GameObject inputPrefab;
        [SerializeField] private GameObject hudPrefab;
        [SerializeField] private GameObject inventoryPrefab;
        [SerializeField] private GameObject sessionPrefab;
        [SerializeField] private GameObject waterPrefab;

        [Header("Scene")]
        [SerializeField] private float waterSurfaceY;
        [SerializeField] private FishData fishData;

        private void Start()
        {
            FishingRodController rod = Spawn(rodPrefab, "Fishing Rod").GetComponent<FishingRodController>();
            GameObject lureObject = Spawn(lurePrefab, "Fishing Lure");
            GameObject water = Spawn(waterPrefab, "Water");
            Rigidbody lureBody = lureObject.GetComponent<Rigidbody>();
            if (lureBody == null) lureBody = lureObject.AddComponent<Rigidbody>();
            lureBody.useGravity = false;
            Transform castPoint = rod != null
                ? (rod.CastPoint != null ? rod.CastPoint : CreateDefaultCastPoint(rod.transform))
                : null;
            rod?.Initialize(castPoint, lureBody);
            rod?.GetComponent<FishingLineRenderer>()?.Initialize(rod.transform, lureObject.transform);

            FishAI fish = Spawn(fishPrefab, "Fish").GetComponent<FishAI>();
            Rigidbody fishBody = fish != null ? fish.GetComponent<Rigidbody>() : null;
            if (fishBody == null && fish != null) fishBody = fish.gameObject.AddComponent<Rigidbody>();
            if (fishBody != null) fishBody.useGravity = false;
            fish?.Initialize(fishData, rod, lureObject.transform, fishBody);

            FishingInputController input = Spawn(inputPrefab, "Fishing Input").GetComponent<FishingInputController>();
            FishingTensionUI tensionUI = Spawn(hudPrefab, "Fishing HUD").GetComponent<FishingTensionUI>();
            FishingInventoryUI inventoryUI = FindObjectOfType<FishingInventoryUI>();
            PlayerInventory inventory = Spawn(inventoryPrefab, "Player Inventory").GetComponent<PlayerInventory>();
            FishingSessionController session = Spawn(sessionPrefab, "Fishing Session").GetComponent<FishingSessionController>();

            session?.Initialize(rod, fish, input, tensionUI, inventoryUI, inventory);
            EnsureVisual(lureObject, PrimitiveType.Sphere, Color.yellow, Vector3.one * 0.25f);
            EnsureVisual(fish != null ? fish.gameObject : null, PrimitiveType.Sphere, new Color(0.9f, 0.25f, 0.1f), new Vector3(1.2f, 0.5f, 2f));
            EnsureVisual(water, PrimitiveType.Cube, new Color(0.05f, 0.3f, 0.55f), new Vector3(24f, 0.5f, 24f));
        }

        private GameObject Spawn(GameObject prefab, string fallbackName)
        {
            if (prefab == null)
            {
                Debug.LogError($"{name}: falta asignar el prefab '{fallbackName}' en el Inspector.");
                return new GameObject(fallbackName);
            }

            return Instantiate(prefab);
        }

        private static void EnsureVisual(GameObject target, PrimitiveType type, Color color, Vector3 scale)
        {
            if (target == null || target.GetComponentInChildren<Renderer>() != null) return;
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.name = "Visual";
            visual.transform.SetParent(target.transform, false);
            visual.transform.localScale = scale;
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            visual.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Transform CreateDefaultCastPoint(Transform rodTransform)
        {
            GameObject point = new GameObject("Cast Point");
            point.transform.SetParent(rodTransform, false);
            point.transform.localPosition = new Vector3(0f, -3f, 1f);
            return point.transform;
        }
    }
}
