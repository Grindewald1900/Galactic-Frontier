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
    /// Coordinates the legacy MainScene navigation strip and its ordered panel collection.
    /// NexusShell delegates existing gameplay screens to this controller so their serialized
    /// references and business logic remain intact.
    /// </summary>
    /// <remarks>
    /// The indices of <see cref="panels"/> and <see cref="mainButtons"/> must match
    /// <see cref="CurrentScene"/> values for menu entries.
    /// When <see cref="NexusShell"/> is present, legacy navigation buttons are skipped.
    /// </remarks>
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
            usesLegacyNavigation ??= FindFirstObjectByType<NexusShell>() == null;

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
                {
                    ShowPanel(CurrentScene.SETTINGS_MENU);
                }
            }
        }

        /// <summary>Builds the legacy navigation buttons and binds each button to its panel index.</summary>
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

        /// <summary>
        /// Activates one MainScene panel and updates global navigation state.
        /// Legacy button scaling runs only when Nexus navigation is not active.
        /// </summary>
        /// <param name="scene">A menu-valued CurrentScene whose numeric value indexes panels.</param>
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

        /// <summary>Hides every legacy MainScene panel without changing navigation state.</summary>
        public void HideAllPanels()
        {
            foreach (var panel in panels)
            {
                if (panel != null)
                    panel.SetActive(false);
            }
        }
    }
}
