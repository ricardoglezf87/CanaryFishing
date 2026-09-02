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
            GetOrAdd<TackleLoadout>(rodObject);
            GetOrAdd<FishingLineRenderer>(rodObject);
            GameObject lureObject = Spawn(lurePrefab, "Fishing Lure");
            GameObject water = Spawn(waterPrefab, "Water");
            water.transform.position = new Vector3(0f, -2.85f, 25f);
            // La superficie queda por debajo de la boya y el pez para que la demo
            // sea visible con el material opaco de prueba.
            water.transform.position = new Vector3(0f, -3f, 6f);
            waterSurfaceY = water.transform.position.y;
            Rigidbody lureBody = lureObject.GetComponent<Rigidbody>();
            if (lureBody == null) lureBody = lureObject.AddComponent<Rigidbody>();
            lureBody.useGravity = true;
            lureBody.linearDamping = 0.15f;
            EnsureSphereCollider(lureObject, 0.12f);
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
            EnsureSphereCollider(fishObject, 0.45f);
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
            Transform rodVisual = AttachModel(rodObject.transform, "Fishing/Models/FishingRodModel", "Rod Model");
            if (rodVisual != null)
            {
                ConfigureFirstPersonRod(rodVisual);
            }
            Transform lureVisual = AttachModel(lureObject.transform, "Fishing/Models/FloatModel", "Float Model");
            Transform fishVisual = AttachModel(fishObject.transform, "Fishing/Models/FishModel", "Fish Model");
            Transform waterVisual = AttachModel(water.transform, "Fishing/Models/OceanSurface", "Ocean Model");
            ConfigureImportedVisuals(lureVisual, fishVisual, waterVisual);
            EnsureWaterCollider(water);
            CreateGuaranteedWaterSurface(water.transform);
            CreateBeachEnvironment();
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

        private static Transform AttachModel(Transform parent, string resourcePath, string objectName)
        {
            GameObject modelAsset = Resources.Load<GameObject>(resourcePath);
            if (modelAsset == null)
            {
                Debug.LogError($"No se pudo cargar el modelo 3D: Resources/{resourcePath}");
                return null;
            }

            GameObject instance = Instantiate(modelAsset, parent);
            instance.name = objectName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            foreach (Collider modelCollider in instance.GetComponentsInChildren<Collider>())
            {
                modelCollider.enabled = false;
            }
            return instance.transform;
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
            rodVisual.SetParent(camera.transform, false);
            rodVisual.localPosition = new Vector3(0.48f, -1.0f, 1.5f);
            rodVisual.localRotation = Quaternion.Euler(0f, 180f, 8f);
            rodVisual.localScale = Vector3.one * 0.55f;
            ApplyMaterial(rodVisual, CreateMaterial(new Color(0.055f, 0.07f, 0.065f), null, 1f, 0.65f));
        }

        private static void ConfigureImportedVisuals(Transform lure, Transform fish, Transform water)
        {
            if (lure != null)
            {
                lure.localScale = Vector3.one * 0.45f;
                ApplyMaterial(lure, CreateMaterial(new Color(0.92f, 0.16f, 0.08f), null, 1f, 0.45f));
            }
            if (fish != null)
            {
                fish.localScale = Vector3.one * 0.55f;
                ApplyMaterial(fish, CreateMaterial(new Color(0.22f, 0.42f, 0.34f), null, 1f, 0.35f));
            }
            if (water != null)
            {
                water.localScale = Vector3.one;
                Material waterMaterial = CreateMaterial(new Color(0.35f, 0.68f, 0.82f, 0.82f), "Fishing/Textures/AtlanticWater_Albedo", 3f, 0.82f);
                if (waterMaterial.HasProperty("_Metallic")) waterMaterial.SetFloat("_Metallic", 0.05f);
                if (waterMaterial.HasProperty("_Surface")) waterMaterial.SetFloat("_Surface", 1f);
                if (waterMaterial.HasProperty("_Blend")) waterMaterial.SetFloat("_Blend", 0f);
                if (waterMaterial.HasProperty("_Alpha")) waterMaterial.SetFloat("_Alpha", 0.82f);
                waterMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                waterMaterial.renderQueue = 3000;
                ApplyMaterial(water, waterMaterial);
            }
        }

        private static void CreateBeachEnvironment()
        {
            GameObject beachAsset = Resources.Load<GameObject>("Fishing/Models/BeachTerrain");
            if (beachAsset == null) return;
            GameObject beach = Instantiate(beachAsset);
            beach.name = "Canary Beach Model";
            beach.transform.position = new Vector3(0f, -3.08f, -20f);
            ApplyMaterial(beach.transform, CreateMaterial(Color.white, "Fishing/Textures/CanarySand_Albedo", 4f, 0.18f));
        }

        private static void ApplyMaterial(Transform root, Material material)
        {
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterial = material;
            }
        }

        private static Material CreateMaterial(Color color, string textureResource, float tiling, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader);
            material.color = color;
            Texture2D texture = string.IsNullOrEmpty(textureResource) ? null : Resources.Load<Texture2D>(textureResource);
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Repeat;
                texture.filterMode = FilterMode.Trilinear;
                texture.anisoLevel = 8;
                material.mainTexture = texture;
                material.mainTextureScale = Vector2.one * tiling;
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", texture);
                    material.SetTextureScale("_BaseMap", Vector2.one * tiling);
                }
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        private static void EnsureSphereCollider(GameObject target, float radius)
        {
            SphereCollider collider = target.GetComponent<SphereCollider>();
            if (collider == null) collider = target.AddComponent<SphereCollider>();
            collider.radius = radius;
        }

        private static void EnsureWaterCollider(GameObject water)
        {
            BoxCollider collider = water.GetComponent<BoxCollider>();
            if (collider == null) collider = water.AddComponent<BoxCollider>();
            collider.isTrigger = false;
            collider.center = new Vector3(0f, 0f, 0f);
            collider.size = new Vector3(60f, 0.12f, 70f);
        }

        private static void CreateGuaranteedWaterSurface(Transform water)
        {
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Plane);
            surface.name = "Water Texture Surface";
            surface.transform.SetParent(water, false);
            surface.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            surface.transform.localRotation = Quaternion.identity;
            surface.transform.localScale = new Vector3(3f, 1f, 3.5f);
            Collider surfaceCollider = surface.GetComponent<Collider>();
            if (surfaceCollider != null) Destroy(surfaceCollider);

            Texture2D texture = Resources.Load<Texture2D>("Fishing/Textures/AtlanticWater_Albedo");
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) return;
            Material material = new Material(shader);
            material.color = new Color(0.35f, 0.68f, 0.82f, 0.95f);
            if (texture != null)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                material.mainTextureScale = new Vector2(6f, 6f);
                if (material.HasProperty("_BaseMap")) material.SetTextureScale("_BaseMap", new Vector2(6f, 6f));
            }
            MeshRenderer renderer = surface.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }
    }
}
