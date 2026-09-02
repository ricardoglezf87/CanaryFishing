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
            GameObject rodObject = Spawn(rodPrefab, "Fishing Rod");
            FishingRodController rod = GetOrAdd<FishingRodController>(rodObject);
            GetOrAdd<FishingLineRenderer>(rodObject);
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

            GameObject fishObject = Spawn(fishPrefab, "Fish");
            FishAI fish = GetOrAdd<FishAI>(fishObject);
            Rigidbody fishBody = fishObject.GetComponent<Rigidbody>();
            if (fishBody == null) fishBody = fishObject.AddComponent<Rigidbody>();
            if (fishBody != null) fishBody.useGravity = false;
            if (fishData == null)
            {
                fishData = FishData.CreateDemo("Demo Bass", 2.5f, 14f, 100f, 2f, 0.8f);
                Debug.LogWarning($"{name}: no hay FishData asignado; se usa Demo Bass.");
            }
            fish?.Initialize(fishData, rod, lureObject.transform, fishBody);

            FishingInputController input = GetOrAdd<FishingInputController>(Spawn(inputPrefab, "Fishing Input"));
            GameObject hudObject = Spawn(hudPrefab, "Fishing HUD");
            EnsureCanvas(hudObject);
            FishingTensionUI tensionUI = GetOrAdd<FishingTensionUI>(hudObject);
            GetOrAdd<FishingInventoryUI>(hudObject);
            FishingInventoryUI inventoryUI = FindObjectOfType<FishingInventoryUI>();
            PlayerInventory inventory = GetOrAdd<PlayerInventory>(Spawn(inventoryPrefab, "Player Inventory"));
            FishingSessionController session = GetOrAdd<FishingSessionController>(Spawn(sessionPrefab, "Fishing Session"));

            session?.Initialize(rod, fish, input, tensionUI, inventoryUI, inventory);
            EnsureVisual(rodObject, PrimitiveType.Cylinder, new Color(0.15f, 0.08f, 0.03f), new Vector3(0.08f, 2f, 0.08f));
            EnsureVisual(lureObject, PrimitiveType.Sphere, Color.yellow, Vector3.one * 0.25f);
            EnsureVisual(fish != null ? fish.gameObject : null, PrimitiveType.Sphere, new Color(0.9f, 0.25f, 0.1f), new Vector3(1.2f, 0.5f, 2f));
            EnsureVisual(water, PrimitiveType.Cube, new Color(0.05f, 0.3f, 0.55f), new Vector3(24f, 0.5f, 24f));
            ConfigureCamera();
        }

        private GameObject Spawn(GameObject prefab, string fallbackName)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"{name}: prefab '{fallbackName}' no está asignado; se crea un fallback de prueba.");
                return CreateRuntimeFallback(fallbackName);
            }

            return Instantiate(prefab);
        }

        private static GameObject CreateRuntimeFallback(string objectName)
        {
            GameObject result = new GameObject(objectName);
            switch (objectName)
            {
                case "Fishing Rod":
                    result.transform.position = new Vector3(0f, 1f, -1f);
                    result.AddComponent<FishingRodController>();
                    result.AddComponent<FishingLineRenderer>();
                    break;
                case "Fishing Lure":
                    result.transform.position = new Vector3(0f, -2f, 4f);
                    Rigidbody lureBody = result.AddComponent<Rigidbody>();
                    lureBody.useGravity = false;
                    break;
                case "Fish":
                    result.transform.position = new Vector3(0f, -2f, 8f);
                    Rigidbody fishBody = result.AddComponent<Rigidbody>();
                    fishBody.useGravity = false;
                    result.AddComponent<FishAI>();
                    break;
                case "Fishing Input": result.AddComponent<FishingInputController>(); break;
                case "Fishing HUD":
                    result.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    result.AddComponent<FishingTensionUI>();
                    result.AddComponent<FishingInventoryUI>();
                    break;
                case "Player Inventory": result.AddComponent<PlayerInventory>(); break;
                case "Fishing Session": result.AddComponent<FishingSessionController>(); break;
                case "Water": result.transform.position = new Vector3(0f, -0.5f, 6f); break;
            }
            return result;
        }

        private static void EnsureVisual(GameObject target, PrimitiveType type, Color color, Vector3 scale)
        {
            if (target == null || target.GetComponentInChildren<MeshRenderer>() != null) return;
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.name = "Visual";
            visual.transform.SetParent(target.transform, false);
            visual.transform.localScale = scale;
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            visual.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void EnsureCanvas(GameObject target)
        {
            Canvas canvas = target.GetComponent<Canvas>();
            if (canvas == null) canvas = target.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            GetOrAdd<CanvasScaler>(target);
            GetOrAdd<GraphicRaycaster>(target);
        }

        private static Transform CreateDefaultCastPoint(Transform rodTransform)
        {
            GameObject point = new GameObject("Cast Point");
            point.transform.SetParent(rodTransform, false);
            point.transform.localPosition = new Vector3(0f, -3f, 1f);
            return point.transform;
        }

        private static void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            camera.transform.position = new Vector3(0f, 3f, -8f);
            camera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        }
    }
}
