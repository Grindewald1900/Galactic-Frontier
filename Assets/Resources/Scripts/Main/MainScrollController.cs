using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Assets.Resources.Scripts.UI.Nexus;
using Assets.Resources.Scripts.Utils;
using static Assets.Resources.Scripts.Main.GameStatusManager;

namespace Assets.Resources.Scripts.Main
{
    /// <summary>
    /// Legacy MainScene panel host. When <see cref="AppShell"/> is present it skips building
    /// the old nav strip and only activates panels requested by <see cref="LegacyPanelAdapter"/>.
    /// </summary>
    public class MainScrollController : MonoBehaviour
    {
        public static MainScrollController Instance;
        public RectTransform content;
        public GameObject itemPrefab;
        public List<GameObject> panels;
        public List<GameObject> mainButtons;

        public int totalItems = 10;
        public float itemWidth = 200f;
        public float scaleFactor = 1.2f;
        public float scaleDuration = 0.2f;

        private int selectedIndex = -1;
        private GameObject selectedPanel;
        private bool? usesLegacyNavigation;

        private bool UsesLegacyNavigation =>
            usesLegacyNavigation ??= FindFirstObjectByType<AppShell>() == null;

        void Awake()
        {
            if (Instance == null)
                Instance = this;

            HideAllPanels();
        }

        void Start()
        {
            HideAllPanels();

            if (UsesLegacyNavigation)
            {
                InitializeItems();
                ShowPanel(CurrentScene.CHARACTER_MENU);
                if (LogUtil.CheckNull(mainButtons[0], "MainMenu Scroll Content is null.")) return;
                StartCoroutine(ScaleItem(mainButtons[0], scaleFactor));
            }
        }

        void OnEnable()
        {
            GameStatusManager.Instance.CurrentScene = CurrentScene.MAIN_SCENE;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (GameStatusManager.Instance.IsDrawingCard) return;
                if (selectedIndex == (int)CurrentScene.DRAWCARDS_MENU)
                    ShowPanel(CurrentScene.SETTINGS_MENU);
            }
        }

        public void InitializeItems()
        {
            mainButtons = new List<GameObject>();
            List<string> itemText = new()
            {
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
                newItem.transform.localPosition = new Vector3(i * itemWidth, 0, 0);
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
            ShowPanel((CurrentScene)index);
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

        public void ShowPanel(CurrentScene scene)
        {
            var index = (int)scene;
            if (index == selectedIndex) return;
            if (GameStatusManager.Instance.IsDrawingCard) return;

            GameStatusManager.Instance.CurrentScene = scene;

            if (UsesLegacyNavigation && selectedIndex >= 0 && mainButtons != null && selectedIndex < mainButtons.Count)
            {
                StopAllCoroutines();
                StartCoroutine(ScaleItem(mainButtons[selectedIndex], 1f));
            }

            selectedIndex = index;
            HideAllPanels();
            selectedIndex = index;

            if (selectedIndex >= 0 && selectedIndex < panels.Count)
            {
                selectedPanel = panels[selectedIndex];
                selectedPanel.SetActive(true);

                if (UsesLegacyNavigation && mainButtons != null && selectedIndex < mainButtons.Count)
                {
                    if (LogUtil.CheckNull(mainButtons[selectedIndex], "Child is null")) return;
                    StartCoroutine(ScaleItem(mainButtons[selectedIndex], scaleFactor));
                }
            }
        }

        public void HideAllPanels()
        {
            if (panels == null) return;
            foreach (var panel in panels)
            {
                if (panel != null)
                    panel.SetActive(false);
            }

            selectedIndex = -1;
            selectedPanel = null;
        }
    }
}
