using UnityEngine;
using UnityEngine.UI;

public class RadarCircles : MonoBehaviour
{
    public int circleCount = 5;  // 圆环数量
    public float radiusPadding = 10f;  // 圆环与 Image 边界的间距
    public int segments = 100;  // 每个圆的精度
    public Material lineMaterial;  // 线条材质

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();  // 获取 Image 的 RectTransform

        if (rectTransform == null)
        {
            Debug.LogError("RadarCircles 必须挂载在一个 UI Image 上！");
            return;
        }

        float imageWidth = rectTransform.rect.width;
        float imageHeight = rectTransform.rect.height;
        float minDimension = Mathf.Min(imageWidth, imageHeight) / 2f - radiusPadding; // 保证圆环不超出边界

        for (int i = 0; i < circleCount; i++)
        {
            float radius = (minDimension / circleCount) * (i + 1);  // 按比例绘制
            DrawCircle(radius);
        }
    }

    void DrawCircle(float radius)
    {
        GameObject circleObj = new GameObject("Circle_" + radius);
        circleObj.transform.SetParent(transform);  // 让 Circle 挂载在 Image 下面
        circleObj.transform.localPosition = Vector3.zero;  // 确保圆心对齐
        circleObj.transform.localScale = Vector3.one;  // 确保缩放正确
        circleObj.transform.localRotation = Quaternion.identity;

        LineRenderer lineRenderer = circleObj.AddComponent<LineRenderer>();

        lineRenderer.positionCount = segments + 1;  // +1 是为了闭合圆
        lineRenderer.useWorldSpace = false;  // 使用局部坐标
        lineRenderer.loop = true;  // 让线条闭合成一个圆
        lineRenderer.startWidth = 2f;  // UI 线条宽度
        lineRenderer.endWidth = 2f;
        lineRenderer.material = lineMaterial;

        // **设置 LineRenderer 适用于 UI**
        lineRenderer.sortingOrder = 10; // 让它在 UI 上层
        lineRenderer.alignment = LineAlignment.TransformZ; // UI 友好显示

        float angleStep = 360f / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Deg2Rad * i * angleStep;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }
}