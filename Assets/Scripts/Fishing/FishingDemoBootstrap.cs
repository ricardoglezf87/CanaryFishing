using UnityEngine;
using UnityEngine.UI;

namespace CanaryFishing.Fishing
{
    /// <summary>
    /// Monta una escena mínima de prueba al pulsar Play. Sirve como sandbox del MVP;
    /// en producción estos objetos se sustituyen por prefabs y assets del diseñador.
    /// </summary>
    public sealed class FishingDemoBootstrap : MonoBehaviour
    {
        private void Start()
        {
            if (FindObjectOfType<FishingSessionController>() != null)
            {
                return;
            }

            CreateWater();
            FishingRodController rod = CreateRod(out Transform castPoint);
            Rigidbody lureBody = CreateLure();
            FishAI fish = CreateFish(rod, lureBody.transform);
            FishingInputController input = new GameObject("Fishing Input").AddComponent<FishingInputController>();
            PlayerInventory inventory = new GameObject("Player Inventory").AddComponent<PlayerInventory>();
            FishingTensionUI ui = CreateUI();

            FishingSessionController session = new GameObject("Fishing Session").AddComponent<FishingSessionController>();
            rod.Initialize(castPoint, lureBody);
            session.Initialize(rod, fish, input, ui, inventory);
            ConfigureCamera();
        }

        private static void CreateWater()
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "Demo Water";
            water.transform.position = new Vector3(0f, -0.5f, 6f);
            water.transform.localScale = new Vector3(24f, 0.5f, 24f);
            SetMaterial(water, new Color(0.05f, 0.3f, 0.55f, 1f));
        }

        private static FishingRodController CreateRod(out Transform castPoint)
        {
            GameObject rodObject = new GameObject("Fishing Rod");
            rodObject.transform.position = new Vector3(0f, 1f, -1f);
            FishingRodController rod = rodObject.AddComponent<FishingRodController>();
            GameObject point = new GameObject("Cast Point");
            point.transform.SetParent(rodObject.transform);
            point.transform.localPosition = new Vector3(0f, 0f, 1f);
            castPoint = point.transform;
            return rod;
        }

        private static Rigidbody CreateLure()
        {
            GameObject lure = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lure.name = "Demo Lure";
            lure.transform.position = new Vector3(0f, -2f, 4f);
            lure.transform.localScale = Vector3.one * 0.25f;
            SetMaterial(lure, Color.yellow);
            Rigidbody body = lure.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.drag = 0.5f;
            return body;
        }

        private static FishAI CreateFish(FishingRodController rod, Transform lure)
        {
            GameObject fishObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fishObject.name = "Demo Fish";
            fishObject.transform.position = new Vector3(0f, -2f, 8f);
            fishObject.transform.localScale = new Vector3(1.2f, 0.5f, 2f);
            SetMaterial(fishObject, new Color(0.9f, 0.25f, 0.1f, 1f));
            Rigidbody body = fishObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            FishAI fish = fishObject.AddComponent<FishAI>();
            FishData data = FishData.CreateDemo("Demo Bass", 2.5f, 14f, 100f, 2f, 0.8f);
            fish.Initialize(data, rod, lure, body);
            return fish;
        }

        private static FishingTensionUI CreateUI()
        {
            GameObject canvasObject = new GameObject("Fishing HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            Text tension = CreateText(canvasObject.transform, "Tensión: 0 / 20", new Vector2(20f, -20f));
            Text state = CreateText(canvasObject.transform, "Estado: Idle", new Vector2(20f, -55f));
            Text controls = CreateText(canvasObject.transform, "Mantén SPACE y suelta para lanzar | Mantén R para recoger", new Vector2(20f, -90f));
            controls.color = Color.white;

            GameObject sliderObject = new GameObject("Tension Bar");
            sliderObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = sliderObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -125f);
            rect.sizeDelta = new Vector2(320f, 24f);
            Image background = sliderObject.AddComponent<Image>();
            background.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(sliderObject.transform, false);
            RectTransform fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fill = fillObject.AddComponent<Image>();
            fill.color = Color.green;
            slider.fillRect = fillRect;
            slider.direction = Slider.Direction.LeftToRight;

            GameObject warning = CreateText(canvasObject.transform, "¡TENSIÓN ALTA!", new Vector2(20f, -160f)).gameObject;
            warning.GetComponent<Text>().color = Color.red;
            FishingTensionUI ui = canvasObject.AddComponent<FishingTensionUI>();
            SetPrivateField(ui, "tensionSlider", slider);
            SetPrivateField(ui, "tensionText", tension);
            SetPrivateField(ui, "stateText", state);
            SetPrivateField(ui, "overloadWarning", warning);
            warning.SetActive(false);
            return ui;
        }

        private static Text CreateText(Transform parent, string value, Vector2 position)
        {
            GameObject textObject = new GameObject(value);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(600f, 30f);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 18;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        private static void SetMaterial(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            renderer.sharedMaterial = material;
        }

        private static void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            camera.transform.position = new Vector3(0f, 3f, -8f);
            camera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        }

        // Solo se usa para montar el sandbox en runtime sin exponer API de configuración
        // específica del demo en los componentes de producción.
        private static void SetPrivateField(Object target, string fieldName, Object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
