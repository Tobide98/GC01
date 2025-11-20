using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DrawFPS : MonoBehaviour
{
    [Header("FPS Settings")]
    public float updateInterval = 0.2f;
    public int fontSize = 36;
    public Color fontColor = Color.white;
    public Vector2 anchoredPosition = new Vector2(10, -10);

    private float timer;
    private int frames;
    private TextMeshProUGUI fpsText;

    void Awake()
    {
        CreateFPSDisplay();
    }

    void Update()
    {
        frames++;
        timer += Time.unscaledDeltaTime;

        if (timer >= updateInterval)
        {
            float fps = frames / timer;
            fpsText.text = Mathf.RoundToInt(fps) + " FPS";

            frames = 0;
            timer = 0;
        }
    }

    void CreateFPSDisplay()
    {
        // Create Canvas
        GameObject canvasGO = new GameObject("FPSCanvas");
        canvasGO.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Create Text Object
        GameObject textGO = new GameObject("FPSText");
        textGO.transform.SetParent(canvasGO.transform);

        RectTransform rt = textGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); // top-left
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPosition;

        // Add TextMeshPro
        fpsText = textGO.AddComponent<TextMeshProUGUI>();
        fpsText.fontSize = fontSize;
        fpsText.color = fontColor;
        fpsText.text = "0 FPS";
        fpsText.enableWordWrapping = false;

        // Optional: outline for readability
        fpsText.fontMaterial.EnableKeyword("OUTLINE_ON");
        fpsText.outlineColor = new Color(0, 0, 0, 1f);
        fpsText.outlineWidth = 0.2f;
    }
}
