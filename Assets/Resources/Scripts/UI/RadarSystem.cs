using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RadarSystem : MonoBehaviour
{
    public RectTransform radarImage;  // 雷达背景（Image）
    public RectTransform scanLine;  // 扫描线（Image）
    public GameObject targetPrefab;  // 目标点的 UI 预制体
    // public Transform radarCenter;  // 雷达中心点
    public float scanSpeed = 100f;  // 扫描速度（度/秒）

    private List<GameObject> targets = new List<GameObject>();  // 目标点列表

    void Update()
    {
        RotateScanLine();
    }

    // **旋转扫描线**
    void RotateScanLine()
    {
        if (scanLine != null)
        {
            scanLine.Rotate(0, 0, -scanSpeed * Time.deltaTime);
        }
    }

    // **添加目标点**
    public void AddTarget(float worldX, float worldY)
    {
        if (radarImage == null || targetPrefab == null) return;

        GameObject newTarget = Instantiate(targetPrefab, radarImage);  // 目标点 UI 挂在雷达上
        RectTransform targetRect = newTarget.GetComponent<RectTransform>();

        // **将世界坐标转换为雷达坐标**
        Vector2 radarPos = ConvertWorldToRadar(worldX, worldY);
        targetRect.anchoredPosition = radarPos;
        targets.Add(newTarget);
    }

    // **世界坐标转换为雷达坐标**
    Vector2 ConvertWorldToRadar(float worldX, float worldY)
    {
        float radarWidth = radarImage.rect.width;
        float radarHeight = radarImage.rect.height;

        float normalizedX = worldX / 50f;  // 假设世界最大范围 ±50
        float normalizedY = worldY / 50f;

        float radarPosX = normalizedX * (radarWidth / 2f);
        float radarPosY = normalizedY * (radarHeight / 2f);

        return new Vector2(radarPosX, radarPosY);
    }
}