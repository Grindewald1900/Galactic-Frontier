using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Planet;
using Assets.Resources.Scripts.Utils.Save;

namespace Assets.Resources.Scripts.UI
{
    public class RadarSystem : MonoBehaviour
    {
        public static RadarSystem Instance;
        public int minCircles = 3; // 最少圆环数
        public int maxCircles = 6; // 最多圆环数
        public float radiusPadding = 10f; // 圆环边距
        public int segments = 100; // 圆的精度
        public Material lineMaterial; // 线条材质
        public GameObject planetPrefab; // 星球预制体
        public GameObject starPrefab; // 恒星预制体（中心点）
        public RectTransform scanLine;  // 扫描线（Image）
        public float imageWidth;
        public float imageHeight;
        public float maxRadius;
        public float scanSpeed = 100f;  // 扫描速度（度/秒）
        public int currentIndex = 0;
        private RectTransform rectTransform;
        private List<float> circleRadii = new(); // 存储圆环半径
        private List<PlanetEntity> planetEntities = new(); // 存储星球
        private List<PlanetRadarItem> planetRadarItems = new(); // 存储星球 RadarItem

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }
        void Update()
        {
            RotateScanLine();
        }

        void Start()
        {
            InitRadarSystem();
            PlanetListManager.Instance.InitFocus();
        }

        private void InitRadarSystem()
        {
            rectTransform = GetComponent<RectTransform>(); // 获取 UI Image 的 RectTransform
            int circleCount = Random.Range(minCircles, maxCircles + 1); // 随机生成 3-6 个圆环

            if (rectTransform == null)
            {
                Debug.LogError("RadarCircles 必须挂载在一个 UI Image 上！");
                return;
            }

            imageWidth = rectTransform.rect.width;
            imageHeight = rectTransform.rect.height;
            maxRadius = Mathf.Min(imageWidth, imageHeight) / 2f - radiusPadding; // 最大可用半径

            float lastRadius = 0; // 记录上一个圆环的半径
            for (int i = 0; i < circleCount; i++)
            {
                // **随机圆环半径间距**
                float radius = lastRadius + Random.Range(maxRadius / (circleCount + 1), maxRadius / circleCount);
                lastRadius = radius;
                circleRadii.Add(radius);
                DrawCircle(radius);
            }

            // Sample planets require Dev Data Mode; production waits on region/static planet tables.
            if (DevData.IsActive)
            {
                foreach (float radius in circleRadii)
                {
                    int planetsInRing = Random.Range(1, 4); // **每个圆环 1-3 颗星球**
                    Debug.Log("Planets in ring: " + planetsInRing + " at radius: " + radius);
                    for (int i = 0; i < planetsInRing; i++)
                    {
                        GeneratePlanetsOnRing(radius);
                    }
                }
            }
            else
            {
                DevData.LogSkipped(nameof(RadarSystem) + ".CreateSamplePlanet");
            }

            // **生成中心恒星**
            GenerateStar();
        }

        // **旋转扫描线**
        void RotateScanLine()
        {
            scanLine?.Rotate(0, 0, -scanSpeed * Time.deltaTime);
        }

        // **绘制圆环**
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

        // **随机生成星球**
        void GeneratePlanetsOnRing(float radius)
        {
            if (planetPrefab == null)
            {
                Debug.LogError("请在 Inspector 中指定 Planet Prefab！");
                return;
            }

            List<float> usedAngles = new(); // 存储已使用的角度

            float angle;
            bool validAngle;

            do
            {
                angle = Random.Range(0f, 360f); // **随机角度**
                validAngle = true;

                // **检查是否与已有角度间隔 >= 60°**
                foreach (float usedAngle in usedAngles)
                {
                    if (Mathf.Abs(Mathf.DeltaAngle(angle, usedAngle)) < 60f)
                    {
                        validAngle = false;
                        break;
                    }
                }
            } while (!validAngle); // **如果角度不合适，则重新随机**

            usedAngles.Add(angle); // 记录使用过的角度

            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

            // **实例化星球**
            GameObject planet = Instantiate(planetPrefab, transform);
            RectTransform planetRect = planet.GetComponent<RectTransform>();
            PlanetRadarItem planetRadarItem = planet.GetComponent<PlanetRadarItem>();
            currentIndex++;
            PlanetEntity planetEntity = DevData.Current.CreateSamplePlanet(currentIndex);
            if (planetEntity == null)
                return;

            planetRect.anchoredPosition = new Vector2(x, y);
            planetEntities.Add(planetEntity);
            PlanetListManager.Instance.AddItem(planetEntity);
            planetRadarItem.InitPlanet(planetEntity.backgroundSprite, planetEntity.planetName);
            planetRadarItems.Add(planetRadarItem);
        }

        // **生成中心恒星**
        void GenerateStar()
        {
            if (starPrefab == null)
            {
                Debug.LogError("请在 Inspector 中指定 Star Prefab！");
                return;
            }

            // **实例化恒星**
            GameObject star = Instantiate(starPrefab, transform);
            RectTransform starRect = star.GetComponent<RectTransform>();
            starRect.anchoredPosition = Vector2.zero; // **中心对齐**
        }

        public void SetRadarItemFocus(string planetName)
        {
            Debug.Log("SetRadarItemFocus: " + planetName);
            foreach (PlanetRadarItem item in planetRadarItems)
            {
                if (item.planetName == planetName)
                {
                    item.Select();
                }
                else
                {
                    item.Deselect();
                }
            }
        }

    }
}