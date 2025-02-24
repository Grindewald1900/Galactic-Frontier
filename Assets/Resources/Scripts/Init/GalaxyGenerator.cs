using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GalaxyGenerator : MonoBehaviour
{
    public RectTransform canvasRect;  // 画布（Canvas）
    public GameObject galaxyPrefab;   // 星系 UI 预制体（一个小圆点）
    public Vector2 canvasCenter;      // 画布中心
    public int galaxyCount = 1024;    // 生成的星系数量
    public int minRadius = 100000;    // 最小半径
    public int maxRadius = 900000;    // 最大半径
    public int radiusStep = 100000;   // 圆环间隔
    public float positionJitter = 10000f;  // 误差范围
    public float scaleFactor = 0.0004f;  // 坐标缩放，使 UI 适配 Canvas
    public static GalaxyGenerator instance;
    private List<GameObject> galaxies = new List<GameObject>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
    void Start()
    {
        GenerateGalaxies();
        HideGalaxies();
    }

    public void GenerateGalaxies()
    {
        // **计算 UI 画布的中心**
        canvasCenter = new Vector2(canvasRect.rect.width / 2, canvasRect.rect.height / 2);

        // **圆环半径**
        List<int> allowedRadii = new List<int>();
        for (int r = maxRadius; r >= minRadius; r -= radiusStep)
        {
            allowedRadii.Add(r);
        }

        int remainingGalaxies = galaxyCount;
        for (int i = 0; i < allowedRadii.Count; i++)
        {
            int radius = allowedRadii[i];
            int galaxiesInRing = remainingGalaxies / 2; // 每层数量减半
            remainingGalaxies -= galaxiesInRing;

            for (int j = 0; j < galaxiesInRing; j++)
            {
                float angle = Random.Range(0f, 360f);  // **随机角度**
                float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
                float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

                // **添加随机误差**
                x += Random.Range(-positionJitter, positionJitter);
                y += Random.Range(-positionJitter, positionJitter);

                // **缩小坐标，使其适配 Canvas**
                Vector2 uiPosition = new Vector2(x, y) * scaleFactor;
                uiPosition.y += 200f;

                // **创建 UI 星系**
                GameObject galaxy = Instantiate(galaxyPrefab, canvasRect);
                galaxy.GetComponent<RectTransform>().anchoredPosition = uiPosition;
                galaxies.Add(galaxy);
            }
        }
    }

    public void ShowGalaxies()
    {
        foreach (GameObject galaxy in galaxies)
        {
            galaxy.SetActive(true);
        }
    }

    public void HideGalaxies()
    {
        foreach (GameObject galaxy in galaxies)
        {
            galaxy.SetActive(false);
        }
    }
}