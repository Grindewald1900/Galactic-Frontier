using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CrosshairAnimator : MonoBehaviour
{
    public RectTransform crosshairImage; // 挂载的UI Image
    public float maxScale = 1.1f; // 最大缩放比例
    public float minScale = 1.0f; // 最小缩放比例
    public float animationSpeed = 1.0f; // 瞄准变化速度

    private float imageWidth = 400f;
    private float imageHeight = 240f;
    private float crossLineScale = 0.3f;
    private float cornerLineScaleHorizontal = 0.42f;
    private float cornerLineScaleVertical = 0.38f;
    private RectTransform crosshairGroup; // 父容器
    private float animationTime = 0f; // 记录动画时间

    void Start()
    {
        if (crosshairImage == null)
        {
            Debug.LogError("请在 Inspector 中指定 Crosshair UI Image！");
            return;
        }

        // 创建准星 UI
        crosshairGroup = new GameObject("CrosshairGroup").AddComponent<RectTransform>();
        crosshairGroup.SetParent(crosshairImage);
        crosshairGroup.localScale = Vector3.one;
        crosshairGroup.anchoredPosition = Vector2.zero;
        crosshairGroup.sizeDelta = crosshairImage.GetComponent<RectTransform>().sizeDelta;

        // 创建中心十字
        CreateLine("HorizontalLine", new Vector2(imageHeight * crossLineScale, 5), Vector2.zero);
        CreateLine("VerticalLine", new Vector2(5, imageHeight * crossLineScale), Vector2.zero);

        // 创建四个角落的短斜线
        CreateCorner("TopLeft", new Vector2(imageWidth * cornerLineScaleHorizontal * -1, imageHeight * cornerLineScaleVertical), 45);
        CreateCorner("TopRight", new Vector2(imageWidth * cornerLineScaleHorizontal, imageHeight * cornerLineScaleVertical), -45);
        CreateCorner("BottomLeft", new Vector2(imageWidth * cornerLineScaleHorizontal * -1, imageHeight * cornerLineScaleVertical * -1), -45);
        CreateCorner("BottomRight", new Vector2(imageWidth * cornerLineScaleHorizontal, imageHeight * cornerLineScaleVertical * -1), 45);
    }

    void Update()
    {
        // **实现周期性放大缩小动画**
        animationTime += Time.deltaTime * animationSpeed;
        float scaleFactor = Mathf.Lerp(minScale, maxScale, Mathf.PingPong(animationTime, 1));
        crosshairGroup.localScale = new Vector3(scaleFactor, scaleFactor, 1);
    }

    // **创建中心十字线**
    void CreateLine(string name, Vector2 size, Vector2 position)
    {
        GameObject line = new GameObject(name);
        RectTransform rect = line.AddComponent<RectTransform>();
        rect.SetParent(crosshairGroup);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;

        Image img = line.AddComponent<Image>();
        img.color = Color.white; // 可以调整颜色
    }

    // **创建四角短斜线**
    void CreateCorner(string name, Vector2 position, float rotation)
    {
        GameObject corner = new GameObject(name);
        RectTransform rect = corner.AddComponent<RectTransform>();
        rect.SetParent(crosshairGroup);
        rect.sizeDelta = new Vector2(40, 5); // 斜线长度和厚度
        rect.anchoredPosition = position;
        rect.localRotation = Quaternion.Euler(0, 0, rotation);
        rect.localScale = Vector3.one;

        Image img = corner.AddComponent<Image>();
        img.color = Color.white; // 可以调整颜色
    }
}