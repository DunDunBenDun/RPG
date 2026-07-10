using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ChineseGameUI : MonoBehaviour
{
    private const string CanvasObjectName = "中文界面画布";

    [Header("中文界面文本")]
    [SerializeField] private string titleText = "我的 RPG";
    [SerializeField] private string statusText = "探索世界，开始冒险";
    [SerializeField] private string controlText = "WASD 移动  |  空格跳跃";

    [Header("界面样式")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.45f);

    private Font uiFont;

    private void Awake()
    {
        BuildInterface();
    }

    private void BuildInterface()
    {
        if (transform.Find(CanvasObjectName) != null)
        {
            return;
        }

        uiFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei", "Microsoft JhengHei", "SimHei", "Arial" },
            28);

        var canvasObject = new GameObject(CanvasObjectName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        CreateTopPanel(canvasObject.transform);
        CreateBottomHint(canvasObject.transform);
    }

    private void CreateTopPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "顶部信息面板", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(420f, 118f));

        CreateText(panel.transform, "标题", titleText, 34, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -16f), new Vector2(376f, 42f));
        CreateText(panel.transform, "状态", statusText, 22, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -65f), new Vector2(376f, 32f));
    }

    private void CreateBottomHint(Transform parent)
    {
        var panel = CreatePanel(parent, "底部操作提示", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(520f, 58f));
        CreateText(panel.transform, "操作说明", controlText, 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 40f));
    }

    private GameObject CreatePanel(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var panelObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        var rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        var image = panelObject.GetComponent<Image>();
        image.color = panelColor;

        return panelObject;
    }

    private void CreateText(Transform parent, string objectName, string value, int fontSize, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        var rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        var text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = textColor;
        text.raycastTarget = false;
    }
}
