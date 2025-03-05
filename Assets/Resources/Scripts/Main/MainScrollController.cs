using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Utils;

namespace Assets.Resources.Scripts.Main
{
    public class MainScrollController : MonoBehaviour
    {
        public static MainScrollController Instance;
        public RectTransform content;   // Content 容器
        public GameObject itemPrefab;   // Item 预制体
        public List<GameObject> panels; // 存储所有 Panel
        public List<GameObject> mainButtons;

        public int totalItems = 10;  // 列表中的 UI 元素数量
        public float itemWidth = 200f; // 每个 UI 元素的宽度
        public float scaleFactor = 1.2f; // 点击后放大的大小
        public float scaleDuration = 0.2f; // 放大动画持续时间

        private int selectedIndex = -1; // 当前选中的 Item
        private GameObject selectedPanel = null; // 当前显示的 Panel

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            HideAllPanels();
        }

        void Start()
        {
            HideAllPanels();
            InitializeItems();
            // 默认选中第一个 Item
            ShowPanel((int)MainMenuPanel.CHARACTER);
            if (LogUtil.CheckNull(mainButtons[0], "MainMenu Scroll Content is null.")) return;
            StartCoroutine(ScaleItem(mainButtons[0], scaleFactor));
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (GameStatusManager.Instance.isDrawingCard) return;
                if (selectedIndex == (int)MainMenuPanel.DRAWCARDS)
                {
                    ShowPanel((int)MainMenuPanel.SETTINGS);
                }
            }
        }

        public void InitializeItems()
        {
            mainButtons = new List<GameObject>();
            List<string> itemText = new(){
            "Character",
            "Cards",
            "Battle",
            "Inventory",
            "Building",
            "Shop",
            "Setting"
        };
            if (LogUtil.CheckNull(itemPrefab, "itemPrefab")) return;
            if (LogUtil.CheckNull(content, "content")) return;

            for (int i = 0; i < itemText.Count; i++)
            {
                GameObject newItem = Instantiate(itemPrefab, content);
                if (LogUtil.CheckNull(newItem, "newItem")) continue;
                newItem.transform.localPosition = new Vector3(i * itemWidth, 0, 0);  // 水平排列
                mainButtons.Add(newItem);

                TextMeshProUGUI itemTextComponent = newItem.GetComponentInChildren<TextMeshProUGUI>();
                if (LogUtil.CheckNull(itemTextComponent, "itemTextComponent")) continue;
                itemTextComponent.text = itemText[i];

                Transform targetTransform = newItem.transform.Find("Image");
                if (LogUtil.CheckNull(targetTransform, "targetTransform")) continue;
                Image targetImage = targetTransform.GetComponent<Image>();
                if (LogUtil.CheckNull(targetImage, "targetImage")) continue;
                Sprite sprite = ImageUtil.GetSpriteByName(ImageUtil.UIImagePath, itemText[i]);
                if (LogUtil.CheckNull(sprite, "sprite")) continue;
                targetImage.sprite = sprite;

                Button button = newItem.GetComponent<Button>();
                if (LogUtil.CheckNull(button, "button")) continue;
                int index = i;
                button.onClick.AddListener(() => OnItemClick(index));
            }
        }

        void OnItemClick(int index)
        {
            ShowPanel(index);
        }

        IEnumerator ScaleItem(GameObject item, float targetScale)
        {
            float elapsedTime = 0f;
            Vector3 startScale = item.transform.localScale;
            Vector3 endScale = Vector3.one * targetScale;

            while (elapsedTime < scaleDuration)
            {
                elapsedTime += Time.deltaTime;
                item.transform.localScale = Vector3.Lerp(startScale, endScale, elapsedTime / scaleDuration);
                yield return null;
            }

            item.transform.localScale = endScale;
        }

        public void ShowPanel(int index)
        {
            Debug.Log("ShowPanel: " + index);
            if (index == selectedIndex) return; // 避免重复执行
            if (GameStatusManager.Instance.isDrawingCard) return;

            if (selectedIndex >= 0 && selectedIndex < mainButtons.Count)
            {
                StopAllCoroutines();
                StartCoroutine(ScaleItem(mainButtons[selectedIndex], 1f)); // 还原大小
            }
            selectedIndex = index;
            //TODO: test code
            /**
            if (index == 6)
            {
                GalaxyGenerator.instance.ShowGalaxies();
            }
            else
            {
                GalaxyGenerator.instance.HideGalaxies();
            }
            **/
            HideAllPanels();
            // we have more panels than buttons on home screen menu, e.g. 7 buttons but totally 8 Panels (Draw cards has only one entry in shop panel)
            if (selectedIndex >= 0 && selectedIndex < panels.Count)
            {
                selectedPanel = panels[selectedIndex];
                selectedPanel.SetActive(true);
                if (selectedIndex < mainButtons.Count)
                {
                    if (LogUtil.CheckNull(mainButtons[selectedIndex], "Child is null")) return;
                    StartCoroutine(ScaleItem(mainButtons[selectedIndex], scaleFactor)); // 放大
                }
            }
        }

        void HideAllPanels()
        {
            foreach (var panel in panels)
            {
                panel.SetActive(false);
            }
        }
    }

    public enum MainMenuPanel
    {
        CHARACTER,
        CARDS,
        BATTLE,
        INVENTORY,
        BUILDING,
        SHOP,
        SETTINGS,
        DRAWCARDS
    }
}