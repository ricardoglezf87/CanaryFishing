using UnityEngine;
using UnityEngine.UI;

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
            water.transform.position = new Vector3(0f, -3f, 6f);
            CreateBeachEnvironment(water);
            // La superficie queda por debajo de la boya y el pez para que la demo
            // sea visible con el material opaco de prueba.
            water.transform.position = new Vector3(0f, -3f, 6f);
            Rigidbody lureBody = lureObject.GetComponent<Rigidbody>();
            if (lureBody == null) lureBody = lureObject.AddComponent<Rigidbody>();
            lureBody.useGravity = false;
            Transform castPoint = rod != null
                ? (rod.CastPoint != null ? rod.CastPoint : CreateDefaultCastPoint(rod.transform))
                : null;
            rod?.Initialize(castPoint, lureBody);
            rod?.GetComponent<FishingLineRenderer>()?.Initialize(castPoint, lureObject.transform);

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
            hudObject.SetActive(true);
            EnsureCanvas(hudObject);
            FishingTensionUI tensionUI = GetOrAdd<FishingTensionUI>(hudObject);
            GetOrAdd<FishingInventoryUI>(hudObject);
            FishingInventoryUI inventoryUI = FindAnyObjectByType<FishingInventoryUI>();
            PlayerInventory inventory = GetOrAdd<PlayerInventory>(Spawn(inventoryPrefab, "Player Inventory"));
            FishingSessionController session = GetOrAdd<FishingSessionController>(Spawn(sessionPrefab, "Fishing Session"));
            FishingRuntimeHUD runtimeHUD = GetOrAdd<FishingRuntimeHUD>(gameObject);

            session?.Initialize(rod, fish, input, tensionUI, inventoryUI, inventory);
            EnsureVisual(rodObject, PrimitiveType.Cylinder, new Color(0.15f, 0.08f, 0.03f), new Vector3(0.08f, 2f, 0.08f));
            Transform rodVisual = rodObject.transform.Find("Visual");
            if (rodVisual != null)
            {
                ConfigureFirstPersonRod(rodVisual);
            }
            EnsureVisual(lureObject, PrimitiveType.Sphere, Color.yellow, Vector3.one * 0.25f);
            EnsureVisual(fish != null ? fish.gameObject : null, PrimitiveType.Sphere, new Color(0.9f, 0.25f, 0.1f), new Vector3(1.2f, 0.5f, 2f));
            PolishLureVisual(lureObject);
            PolishFishVisual(fishObject);
            EnsureVisual(water, PrimitiveType.Cube, new Color(0.05f, 0.3f, 0.55f), new Vector3(24f, 0.5f, 24f));
            ConfigureCamera();
            ConfigureFirstPersonPoint(castPoint);
            tensionUI?.Initialize(rod);
            inventoryUI?.Initialize(inventory);
            runtimeHUD.Initialize(rod, inventory);
            Canvas hudCanvas = hudObject.GetComponent<Canvas>();
            if (hudCanvas != null) hudCanvas.enabled = false;
        }

        private GameObject Spawn(GameObject prefab, string fallbackName)
        {
#if UNITY_EDITOR
            if (prefab == null)
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(GetEditorPrefabPath(fallbackName));
            }
#endif
            if (prefab == null)
            {
                return CreateRuntimeFallback(fallbackName);
            }

            return Instantiate(prefab);
        }

#if UNITY_EDITOR
        private static string GetEditorPrefabPath(string fallbackName)
        {
            switch (fallbackName)
            {
                case "Fishing Rod": return "Assets/Prefabs/FishingRod.prefab";
                case "Fishing Lure": return "Assets/Prefabs/FishingLure.prefab";
                case "Fish": return "Assets/Prefabs/Fish.prefab";
                case "Fishing Input": return "Assets/Prefabs/FishingInput.prefab";
                case "Fishing HUD": return "Assets/Prefabs/FishingHUD.prefab";
                case "Player Inventory": return "Assets/Prefabs/PlayerInventory.prefab";
                case "Fishing Session": return "Assets/Prefabs/FishingSession.prefab";
                case "Water": return "Assets/Prefabs/Water.prefab";
                default: return string.Empty;
            }
        }
#endif

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
            target.transform.position = Vector3.zero;
            target.transform.rotation = Quaternion.identity;
            target.transform.localScale = Vector3.one;
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
            camera.transform.position = new Vector3(0f, 1.75f, -7f);
            camera.transform.LookAt(new Vector3(0f, 0.1f, 10f));
            camera.fieldOfView = 70f;
            RenderSettings.ambientLight = new Color(0.45f, 0.55f, 0.7f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.55f, 0.72f, 0.82f);
            RenderSettings.fogDensity = 0.008f;
        }

        private static void ConfigureFirstPersonPoint(Transform castPoint)
        {
            Camera camera = Camera.main;
            if (camera == null || castPoint == null) return;
            castPoint.SetParent(camera.transform, false);
            castPoint.localPosition = new Vector3(0.08f, 1.9f, 1.35f);
            castPoint.localRotation = Quaternion.identity;
        }

        private static void ConfigureFirstPersonRod(Transform rodVisual)
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            // Los cilindros de Unity crecen sobre su eje Y. Por eso una caña
            // visualmente vertical usa Y=180 y una ligera inclinación en Z,
            // en lugar de girarla 90 grados y dejarla horizontal.
            rodVisual.SetParent(camera.transform, false);
            rodVisual.name = "Rod Handle";
            rodVisual.localPosition = new Vector3(0.55f, -0.48f, 0.95f);
            rodVisual.localRotation = Quaternion.Euler(0f, 180f, 8f);
            rodVisual.localScale = new Vector3(0.055f, 0.45f, 0.055f);
            SetMaterial(rodVisual.gameObject, new Color(0.16f, 0.07f, 0.025f));

            CreateVisualPart(camera.transform, PrimitiveType.Cylinder, "Rod Lower Section",
                new Vector3(0.43f, 0.28f, 1.08f), new Vector3(0.03f, 0.75f, 0.03f),
                Quaternion.Euler(0f, 180f, 8f), new Color(0.055f, 0.075f, 0.065f));
            CreateVisualPart(camera.transform, PrimitiveType.Cylinder, "Rod Upper Section",
                new Vector3(0.23f, 1.48f, 1.25f), new Vector3(0.016f, 0.62f, 0.016f),
                Quaternion.Euler(0f, 180f, 10f), new Color(0.035f, 0.05f, 0.045f));

            CreateVisualPart(camera.transform, PrimitiveType.Cylinder, "Reel Spool",
                new Vector3(0.65f, -0.13f, 0.9f), new Vector3(0.11f, 0.055f, 0.11f),
                Quaternion.Euler(90f, 0f, 0f), new Color(0.08f, 0.1f, 0.12f));
            CreateVisualPart(camera.transform, PrimitiveType.Cube, "Reel Arm",
                new Vector3(0.75f, -0.12f, 0.88f), new Vector3(0.16f, 0.025f, 0.025f),
                Quaternion.Euler(0f, 0f, -20f), new Color(0.18f, 0.2f, 0.22f));
        }

        private static void PolishLureVisual(GameObject lure)
        {
            if (lure == null) return;
            Transform body = lure.transform.Find("Visual");
            if (body != null)
            {
                body.localScale = new Vector3(0.12f, 0.16f, 0.12f);
                SetMaterial(body.gameObject, new Color(0.92f, 0.16f, 0.08f));
            }

            CreateVisualPart(lure.transform, PrimitiveType.Cylinder, "Float Stem",
                new Vector3(0f, 0.2f, 0f), new Vector3(0.025f, 0.12f, 0.025f),
                Quaternion.identity, Color.white);
        }

        private static void PolishFishVisual(GameObject fish)
        {
            if (fish == null) return;
            Transform body = fish.transform.Find("Visual");
            if (body != null)
            {
                body.localScale = new Vector3(0.42f, 0.22f, 0.7f);
                SetMaterial(body.gameObject, new Color(0.18f, 0.38f, 0.32f));
            }

            CreateVisualPart(fish.transform, PrimitiveType.Cube, "Fish Tail",
                new Vector3(0f, 0f, -0.78f), new Vector3(0.42f, 0.055f, 0.32f),
                Quaternion.Euler(0f, 45f, 0f), new Color(0.12f, 0.3f, 0.26f));
            CreateVisualPart(fish.transform, PrimitiveType.Sphere, "Fish Eye",
                new Vector3(0.25f, 0.1f, 0.42f), Vector3.one * 0.045f,
                Quaternion.identity, Color.black);
        }

        private static GameObject CreateVisualPart(Transform parent, PrimitiveType type, string partName,
            Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRotation;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            SetMaterial(part, color);
            return part;
        }

        private static void CreateBeachEnvironment(GameObject water)
        {
            GameObject beach = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beach.name = "Demo Beach";
            beach.transform.position = new Vector3(0f, -3.35f, 10f);
            beach.transform.localScale = new Vector3(50f, 0.2f, 50f);
            SetMaterial(beach, new Color(0.76f, 0.58f, 0.34f));

            if (water != null)
            {
                Transform visual = water.transform.Find("Visual");
                if (visual != null) visual.localScale = new Vector3(30f, 0.25f, 30f);
                MeshRenderer renderer = water.GetComponentInChildren<MeshRenderer>();
                if (renderer != null) renderer.sharedMaterial = CreateMaterial(new Color(0.025f, 0.22f, 0.42f));
            }

            GameObject shore = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shore.name = "Beach Shoreline";
            shore.transform.position = new Vector3(0f, -3.16f, -1f);
            shore.transform.localScale = new Vector3(50f, 0.12f, 8f);
            SetMaterial(shore, new Color(0.9f, 0.72f, 0.45f));
        }

        private static void SetMaterial(GameObject target, Color color)
        {
            MeshRenderer renderer = target.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = CreateMaterial(color);
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.55f);
            return material;
        }
    }
}
