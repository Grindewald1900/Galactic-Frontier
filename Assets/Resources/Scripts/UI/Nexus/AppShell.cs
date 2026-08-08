using System.Collections;
using System.Collections.Generic;
using Assets.Resources.Scripts.Main;
using Assets.Resources.Scripts.Utils;
using Assets.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// First-class application shell: owns chrome and native screens.
    /// Legacy prefab panels are only used through <see cref="LegacyPanelAdapter"/> for unfinished pages.
    /// </summary>
    public sealed class AppShell : MonoBehaviour
    {
        public static AppShell Instance { get; private set; }

        private readonly Dictionary<AppScreen, Button> navigationButtons = new();
        private readonly Dictionary<AppScreen, TextMeshProUGUI> navigationLabels = new();
        private readonly Dictionary<AppScreen, Image> navigationIcons = new();

        private Canvas contentCanvas;
        private RectTransform contentHost;
        private GameObject bridgeRoot;
        private GameObject missionsRoot;
        private GameObject settingsRoot;
        private GameObject exploreRoot;
        private FormationScreen formationScreen;
        private TextMeshProUGUI breadcrumbTitle;
        private TextMeshProUGUI statusShortcuts;
        private TextMeshProUGUI statusVersion;
        private MainScrollController mainController;
        private AppScreen activeScreen = AppScreen.Bridge;
        private bool mainReady;
        private bool navExpanded;
        private float navWidth = NexusTheme.NavCollapsedWidth;
        private Transform chromeRoot;
        private Transform backdropRoot;
        private Transform menuBrandRoot;
        private RectTransform navigationRect;
        private RectTransform topBarRect;
        private RectTransform breadcrumbRect;
        private RectTransform statusBarRect;
        private RectTransform navHeaderRect;
        private RectTransform brandGroupRect;
        private RectTransform toggleRect;
        private TextMeshProUGUI brandTitle;
        private TextMeshProUGUI toggleLabel;
        private Image brandIcon;

        private static AppScreen PendingScreen { get; set; } = AppScreen.Bridge;

        private IEnumerator Start()
        {
            Instance = this;
            LocalizationUtil.Initialize();
            LocalizationUtil.LanguageChanged += OnLanguageChanged;
            yield return null;

            string sceneName = SceneManager.GetActiveScene().name;
            BuildBackdrop();

            switch (sceneName)
            {
                case "MainMenuScene":
                    BuildMainMenuBranding();
                    break;
                case "MainScene":
                    BuildChrome(true);
                    PrepareMainScene();
                    AppScreen initial = PendingScreen;
                    PendingScreen = AppScreen.Bridge;
                    ShowScreen(initial);
                    mainReady = true;
                    break;
            }
        }

        private void OnDestroy()
        {
            LocalizationUtil.LanguageChanged -= OnLanguageChanged;
            if (Instance == this)
                Instance = null;
        }

        private void OnLanguageChanged()
        {
            if (!mainReady && SceneManager.GetActiveScene().name != "MainMenuScene")
                return;

            ReloadLocalizedUi();
        }

        /// <summary>Rebuilds chrome and the active content screen after a locale change.</summary>
        public void ReloadLocalizedUi()
        {
            AppScreen restore = activeScreen;
            if (chromeRoot != null) Destroy(chromeRoot.gameObject);
            if (contentCanvas != null) Destroy(contentCanvas.gameObject);
            if (menuBrandRoot != null) Destroy(menuBrandRoot.gameObject);

            contentCanvas = null;
            contentHost = null;
            bridgeRoot = null;
            missionsRoot = null;
            settingsRoot = null;
            exploreRoot = null;
            formationScreen = null;
            navigationButtons.Clear();
            navigationLabels.Clear();
            navigationIcons.Clear();
            navigationRect = null;
            topBarRect = null;
            breadcrumbRect = null;
            statusBarRect = null;
            navHeaderRect = null;
            brandGroupRect = null;
            toggleRect = null;
            brandTitle = null;
            toggleLabel = null;
            brandIcon = null;
            breadcrumbTitle = null;
            statusShortcuts = null;
            statusVersion = null;

            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "MainMenuScene")
            {
                BuildMainMenuBranding();
                return;
            }

            if (sceneName != "MainScene")
                return;

            BuildChrome(true);
            ShowScreen(restore == AppScreen.Battle ? AppScreen.Bridge : restore);
        }

        private void Update()
        {
            if (!mainReady)
                return;

            if (Input.GetKeyDown(KeyCode.F1)) ShowScreen(AppScreen.Bridge);
            else if (Input.GetKeyDown(KeyCode.F2)) ShowScreen(AppScreen.Battle);
            else if (Input.GetKeyDown(KeyCode.F3)) ShowScreen(AppScreen.Formation);
            else if (Input.GetKeyDown(KeyCode.F4)) ShowScreen(AppScreen.Characters);
            else if (Input.GetKeyDown(KeyCode.F5)) ShowScreen(AppScreen.Inventory);
            else if (Input.GetKeyDown(KeyCode.F6)) ShowScreen(AppScreen.Crafting);
            else if (Input.GetKeyDown(KeyCode.F7)) ShowScreen(AppScreen.Market);
            else if (Input.GetKeyDown(KeyCode.F8)) ShowScreen(AppScreen.Missions);
        }

        public static void RequestScreen(AppScreen screen)
        {
            PendingScreen = screen;
        }

        public void Navigate(AppScreen screen)
        {
            if (SceneManager.GetActiveScene().name == "MainScene")
                ShowScreen(screen);
            else
            {
                PendingScreen = screen;
                SceneManager.LoadScene("MainScene");
            }
        }

        private void ShowScreen(AppScreen screen)
        {
            activeScreen = screen;
            SetActiveNavigation(screen);
            SetBreadcrumb(screen);

            if (screen == AppScreen.Battle)
            {
                // Product: explore/select region first, then fully automatic BattleScene.
                EnsureContentCanvas();
                contentCanvas.gameObject.SetActive(true);
                HideNativeRoots();
                LegacyPanelAdapter.Hide(mainController);
                if (exploreRoot == null)
                    exploreRoot = ExploreScreen.Build(ContentRoot(), () => ShowScreen(AppScreen.Formation));
                exploreRoot.SetActive(true);
                return;
            }

            EnsureContentCanvas();
            contentCanvas.gameObject.SetActive(true);
            HideNativeRoots();
            LegacyPanelAdapter.Hide(mainController);

            switch (screen)
            {
                case AppScreen.Bridge:
                    if (bridgeRoot == null)
                    {
                        bridgeRoot = BridgeScreen.Build(
                            ContentRoot(),
                            () => ShowScreen(AppScreen.Missions),
                            () => ShowScreen(AppScreen.Formation),
                            () => ShowScreen(AppScreen.Battle));
                    }
                    bridgeRoot.SetActive(true);
                    break;
                case AppScreen.Formation:
                    if (formationScreen == null)
                        formationScreen = FormationScreen.Build(ContentRoot());
                    else
                        formationScreen.Rebuild();
                    formationScreen.Root.SetActive(true);
                    break;
                case AppScreen.Settings:
                    if (settingsRoot == null)
                        settingsRoot = SettingsScreen.Build(ContentRoot());
                    settingsRoot.SetActive(true);
                    break;
                case AppScreen.Missions:
                    if (missionsRoot == null)
                        missionsRoot = BuildMissionsPlaceholder();
                    missionsRoot.SetActive(true);
                    break;
                default:
                    if (contentCanvas != null)
                        contentCanvas.gameObject.SetActive(false);
                    if (!LegacyPanelAdapter.TryShow(screen, mainController))
                        Debug.LogWarning($"AppShell: no handler for {screen}");
                    break;
            }
        }

        private void HideNativeRoots()
        {
            if (bridgeRoot != null) bridgeRoot.SetActive(false);
            if (missionsRoot != null) missionsRoot.SetActive(false);
            if (settingsRoot != null) settingsRoot.SetActive(false);
            if (exploreRoot != null) exploreRoot.SetActive(false);
            if (formationScreen != null) formationScreen.Root.SetActive(false);
        }

        private void PrepareMainScene()
        {
            mainController = MainScrollController.Instance ?? FindFirstObjectByType<MainScrollController>();
            GameObject oldNavigation = GameObject.Find("MainScrollView");
            if (oldNavigation != null)
                oldNavigation.SetActive(false);

            LegacyPanelAdapter.FitToContentArea(mainController, navWidth);
            mainController?.HideAllPanels();
        }

        private void BuildBackdrop()
        {
            if (backdropRoot != null)
                return;

            Canvas canvas = NexusUiFactory.CreateCanvas("App Backdrop", -100, false);
            canvas.transform.SetParent(transform, false);
            backdropRoot = canvas.transform;
            NexusUiFactory.CreatePanel(
                canvas.transform,
                "Background",
                NexusTheme.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
        }

        private void BuildChrome(bool fullNavigation)
        {
            Canvas chrome = NexusUiFactory.CreateCanvas("App Chrome", 100, true);
            chrome.transform.SetParent(transform, false);
            chromeRoot = chrome.transform;

            GameObject navigation = NexusUiFactory.CreatePanel(
                chrome.transform,
                "Navigation Rail",
                NexusTheme.Surface,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                Vector2.zero,
                new Vector2(navWidth, 0f));
            navigationRect = navigation.GetComponent<RectTransform>();

            GameObject topBar = NexusUiFactory.CreatePanel(
                chrome.transform,
                "Top Bar",
                NexusTheme.Surface,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(navWidth, -NexusTheme.TopBarHeight),
                Vector2.zero);
            topBarRect = topBar.GetComponent<RectTransform>();

            GameObject breadcrumb = NexusUiFactory.CreatePanel(
                chrome.transform,
                "Breadcrumb",
                NexusTheme.WithAlpha(NexusTheme.Surface, 0.96f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(navWidth, -(NexusTheme.TopBarHeight + NexusTheme.BreadcrumbHeight)),
                new Vector2(0f, -NexusTheme.TopBarHeight));
            breadcrumbRect = breadcrumb.GetComponent<RectTransform>();

            GameObject statusBar = NexusUiFactory.CreatePanel(
                chrome.transform,
                "Status Bar",
                NexusTheme.Surface,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(navWidth, 0f),
                new Vector2(0f, NexusTheme.StatusBarHeight));
            statusBarRect = statusBar.GetComponent<RectTransform>();

            BuildTopBar(topBar.transform);
            BuildBreadcrumb(breadcrumb.transform);
            BuildStatusBar(statusBar.transform);
            BuildNavigation(navigation.transform, fullNavigation);
            ApplyNavigationLayout();
        }

        private void BuildTopBar(Transform parent)
        {
            string commanderName = DataUtil.Instance?.currentPlayer?.playerName;
            if (string.IsNullOrWhiteSpace(commanderName)) commanderName = "COMMANDER";
            int level = DataUtil.Instance?.currentPlayer?.level ?? 1;
            int credits = DataUtil.Instance?.currentPlayer?.creditPoints ?? 0;

            NexusUiFactory.CreateText(
                parent,
                "Commander",
                $"{commanderName}  <size=11><color=#{ColorUtility.ToHtmlStringRGB(NexusTheme.Gold)}>LV.{level}</color></size>",
                new Vector2(18f, 12f),
                new Vector2(280f, 28f),
                15f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            NexusUiFactory.CreateText(
                parent,
                "Credits",
                $"₵ {credits:N0}",
                new Vector2(320f, 14f),
                new Vector2(180f, 24f),
                14f,
                NexusTheme.Gold,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
        }

        private void BuildBreadcrumb(Transform parent)
        {
            NexusUiFactory.CreateText(parent, "Brand", "NEXUS ›", new Vector2(18f, 6f), new Vector2(90f, 20f), 11f, NexusTheme.DimText);
            breadcrumbTitle = NexusUiFactory.CreateText(
                parent,
                "Current",
                UiText.Breadcrumb(AppScreen.Bridge),
                new Vector2(110f, 6f),
                new Vector2(640f, 20f),
                12f,
                NexusTheme.Gold,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
        }

        private void BuildStatusBar(Transform parent)
        {
            statusShortcuts = NexusUiFactory.CreateText(
                parent,
                "Shortcuts",
                UiText.StatusShortcuts,
                new Vector2(16f, 2f),
                new Vector2(1100f, 18f),
                10f,
                NexusTheme.DimText);
            statusVersion = NexusUiFactory.CreateText(
                parent,
                "Version",
                UiText.StatusVersion,
                new Vector2(1450f, 2f),
                new Vector2(360f, 18f),
                10f,
                NexusTheme.DimText,
                TextAlignmentOptions.Right);
        }

        private void BuildNavigation(Transform parent, bool fullNavigation)
        {
            BuildNavHeader(parent);

            float y = 70f;
            AddNav(parent, AppScreen.Bridge, y); y += 52f;
            AddNav(parent, AppScreen.Battle, y); y += 52f;
            AddNav(parent, AppScreen.Formation, y); y += 52f;
            AddNav(parent, AppScreen.Characters, y); y += 52f;
            AddNav(parent, AppScreen.Cards, y); y += 52f;
            AddNav(parent, AppScreen.Inventory, y); y += 52f;
            AddNav(parent, AppScreen.Crafting, y); y += 52f;
            AddNav(parent, AppScreen.Market, y); y += 52f;
            AddNav(parent, AppScreen.Missions, y);

            AddNav(parent, AppScreen.Settings, 1000f);

            if (!fullNavigation)
            {
                foreach (var pair in navigationButtons)
                {
                    AppScreen target = pair.Key;
                    pair.Value.onClick.RemoveAllListeners();
                    if (target != AppScreen.Battle)
                        pair.Value.onClick.AddListener(() => Navigate(target));
                }
            }
        }

        private void BuildNavHeader(Transform parent)
        {
            GameObject header = NexusUiFactory.CreatePanel(
                parent,
                "Nav Header",
                NexusTheme.Surface,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -56f),
                Vector2.zero,
                true);
            navHeaderRect = header.GetComponent<RectTransform>();
            var line = header.AddComponent<Outline>();
            line.effectColor = NexusTheme.BorderSoft;
            line.effectDistance = new Vector2(0f, -1f);

            // Expanded: game icon + title. Collapsed: hidden.
            GameObject brand = new GameObject("Brand Group", typeof(RectTransform));
            brand.transform.SetParent(header.transform, false);
            brandGroupRect = brand.GetComponent<RectTransform>();
            brandGroupRect.anchorMin = new Vector2(0f, 0f);
            brandGroupRect.anchorMax = new Vector2(1f, 1f);
            brandGroupRect.offsetMin = new Vector2(10f, 6f);
            brandGroupRect.offsetMax = new Vector2(-44f, -6f);

            brandIcon = NexusUiFactory.CreateIcon(
                brand.transform,
                "Brand Icon",
                NexusCardVisual.UiIcon("Battle"),
                new Vector2(0f, 4f),
                new Vector2(32f, 32f),
                NexusTheme.Gold);
            brandTitle = NexusUiFactory.CreateText(
                brand.transform,
                "Brand Title",
                "NEXUS",
                new Vector2(40f, 6f),
                new Vector2(140f, 28f),
                18f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            // Toggle: › when collapsed, ‹ when expanded (matches Figma screenshots).
            Button toggle = NexusUiFactory.CreateButton(
                header.transform,
                "Nav Toggle",
                "›",
                new Vector2(14f, 10f),
                new Vector2(36f, 36f),
                ToggleNavigation,
                NexusTheme.SurfaceRaised,
                NexusTheme.MutedText,
                18f);
            toggleRect = toggle.GetComponent<RectTransform>();
            toggleLabel = toggle.GetComponentInChildren<TextMeshProUGUI>();
            if (toggleLabel != null)
                toggleLabel.fontStyle = FontStyles.Bold;
        }

        private void AddNav(Transform parent, AppScreen screen, float y)
        {
            float width = NavButtonWidth();
            GameObject buttonObject = NexusUiFactory.CreateBox(
                parent,
                $"Nav {screen}",
                new Vector2(6f, y),
                new Vector2(width, 44f),
                NexusTheme.Surface,
                NexusTheme.BorderSoft);
            var image = buttonObject.GetComponent<Image>();
            image.raycastTarget = true;
            var button = buttonObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = NexusTheme.Surface;
            colors.highlightedColor = NexusTheme.SurfaceHover;
            colors.pressedColor = NexusTheme.WithAlpha(NexusTheme.Gold, 0.45f);
            colors.selectedColor = NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f);
            button.colors = colors;
            button.onClick.AddListener(() => Navigate(screen));

            Image icon = NexusUiFactory.CreateIcon(
                buttonObject.transform,
                "Icon",
                NavIconSprite(screen),
                new Vector2(12f, 8f),
                new Vector2(28f, 28f),
                NexusTheme.MutedText);

            TextMeshProUGUI label = NexusUiFactory.CreateText(
                buttonObject.transform,
                "Label",
                NavTitle(screen),
                new Vector2(48f, 8f),
                new Vector2(150f, 28f),
                14f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            navigationButtons[screen] = button;
            navigationIcons[screen] = icon;
            navigationLabels[screen] = label;
        }

        private static Sprite NavIconSprite(AppScreen screen)
        {
            string iconName = screen switch
            {
                AppScreen.Bridge => "Bell",
                AppScreen.Battle => "Battle",
                AppScreen.Formation => "Character",
                AppScreen.Characters => "Character",
                AppScreen.Cards => "Cards",
                AppScreen.Inventory => "Inventory",
                AppScreen.Crafting => "Building",
                AppScreen.Market => "Shop",
                AppScreen.Missions => "Add Icon",
                AppScreen.Settings => "Settings",
                _ => "circle"
            };
            return NexusCardVisual.UiIcon(iconName);
        }

        private static string NavTitle(AppScreen screen) => screen switch
        {
            AppScreen.Bridge => UiText.ScreenBridge,
            AppScreen.Battle => UiText.ScreenBattle,
            AppScreen.Formation => UiText.ScreenFormation,
            AppScreen.Characters => UiText.ScreenCharacters,
            AppScreen.Cards => UiText.ScreenCards,
            AppScreen.Inventory => UiText.ScreenInventory,
            AppScreen.Crafting => UiText.ScreenCrafting,
            AppScreen.Market => UiText.ScreenMarket,
            AppScreen.Missions => UiText.ScreenMissions,
            AppScreen.Settings => UiText.ScreenSettings,
            _ => screen.ToString()
        };

        private float NavButtonWidth() => Mathf.Max(52f, navWidth - 12f);

        private void ToggleNavigation()
        {
            navExpanded = !navExpanded;
            navWidth = navExpanded ? NexusTheme.NavExpandedWidth : NexusTheme.NavCollapsedWidth;
            ApplyNavigationLayout();
        }

        private void ApplyNavigationLayout()
        {
            if (navigationRect != null)
                navigationRect.offsetMax = new Vector2(navWidth, 0f);

            if (topBarRect != null)
                topBarRect.offsetMin = new Vector2(navWidth, -NexusTheme.TopBarHeight);

            if (breadcrumbRect != null)
            {
                breadcrumbRect.offsetMin = new Vector2(navWidth, -(NexusTheme.TopBarHeight + NexusTheme.BreadcrumbHeight));
                breadcrumbRect.offsetMax = new Vector2(0f, -NexusTheme.TopBarHeight);
            }

            if (statusBarRect != null)
                statusBarRect.offsetMin = new Vector2(navWidth, 0f);

            // Right-side pages shrink / expand with the rail.
            // Must use a child host: ScreenSpaceOverlay CanvasScaler overwrites root canvas offsets.
            if (contentHost != null)
            {
                contentHost.anchorMin = Vector2.zero;
                contentHost.anchorMax = Vector2.one;
                contentHost.pivot = new Vector2(0.5f, 0.5f);
                contentHost.offsetMin = new Vector2(navWidth, NexusTheme.StatusBarHeight);
                contentHost.offsetMax = new Vector2(0f, -(NexusTheme.TopBarHeight + NexusTheme.BreadcrumbHeight));
            }

            ApplyNavHeaderLayout();
            ApplyNavItemLayout();
            LegacyPanelAdapter.FitToContentArea(mainController, navWidth);
            SetActiveNavigation(activeScreen);
            Canvas.ForceUpdateCanvases();
        }

        private void ApplyNavHeaderLayout()
        {
            if (brandGroupRect != null)
                brandGroupRect.gameObject.SetActive(navExpanded);

            if (brandTitle != null)
                brandTitle.text = "NEXUS";

            if (toggleRect == null || toggleLabel == null)
                return;

            if (navExpanded)
            {
                // Arrow on the right of the brand header (‹ collapses).
                toggleRect.anchorMin = new Vector2(1f, 0.5f);
                toggleRect.anchorMax = new Vector2(1f, 0.5f);
                toggleRect.pivot = new Vector2(1f, 0.5f);
                toggleRect.anchoredPosition = new Vector2(-8f, 0f);
                toggleRect.sizeDelta = new Vector2(32f, 32f);
                toggleLabel.text = "‹";
            }
            else
            {
                // Collapsed top-left is only the expand arrow (›), not a player avatar.
                toggleRect.anchorMin = new Vector2(0.5f, 0.5f);
                toggleRect.anchorMax = new Vector2(0.5f, 0.5f);
                toggleRect.pivot = new Vector2(0.5f, 0.5f);
                toggleRect.anchoredPosition = Vector2.zero;
                toggleRect.sizeDelta = new Vector2(40f, 40f);
                toggleLabel.text = "›";
            }

            toggleLabel.alignment = TextAlignmentOptions.Center;
            toggleLabel.color = NexusTheme.MutedText;
        }

        private void ApplyNavItemLayout()
        {
            float buttonWidth = NavButtonWidth();
            foreach (var pair in navigationButtons)
            {
                var rect = pair.Value.GetComponent<RectTransform>();
                if (rect != null)
                    rect.sizeDelta = new Vector2(buttonWidth, rect.sizeDelta.y);

                if (navigationIcons.TryGetValue(pair.Key, out Image icon) && icon != null)
                {
                    var iconRect = icon.rectTransform;
                    if (navExpanded)
                    {
                        iconRect.anchoredPosition = new Vector2(12f, -8f);
                    }
                    else
                    {
                        // Center icon in collapsed rail.
                        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                        iconRect.pivot = new Vector2(0.5f, 0.5f);
                        iconRect.anchoredPosition = Vector2.zero;
                        iconRect.sizeDelta = new Vector2(28f, 28f);
                    }

                    if (navExpanded)
                    {
                        iconRect.anchorMin = new Vector2(0f, 1f);
                        iconRect.anchorMax = new Vector2(0f, 1f);
                        iconRect.pivot = new Vector2(0f, 1f);
                        iconRect.sizeDelta = new Vector2(28f, 28f);
                    }
                }

                if (navigationLabels.TryGetValue(pair.Key, out TextMeshProUGUI label) && label != null)
                {
                    label.text = NavTitle(pair.Key);
                    label.gameObject.SetActive(navExpanded);
                    if (navExpanded)
                    {
                        label.rectTransform.anchoredPosition = new Vector2(48f, -8f);
                        label.rectTransform.sizeDelta = new Vector2(buttonWidth - 56f, 28f);
                    }
                }
            }
        }

        private void SetActiveNavigation(AppScreen screen)
        {
            foreach (var pair in navigationButtons)
            {
                bool active = pair.Key == screen;
                pair.Value.image.color = active
                    ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f)
                    : NexusTheme.Surface;

                if (navigationIcons.TryGetValue(pair.Key, out Image icon) && icon != null)
                    icon.color = active ? NexusTheme.Gold : NexusTheme.MutedText;

                if (navigationLabels.TryGetValue(pair.Key, out TextMeshProUGUI label) && label != null)
                    label.color = active ? NexusTheme.Gold : NexusTheme.MutedText;
            }
        }

        private void SetBreadcrumb(AppScreen screen)
        {
            if (breadcrumbTitle != null)
                breadcrumbTitle.text = UiText.Breadcrumb(screen);
        }

        private void EnsureContentCanvas()
        {
            if (contentCanvas == null)
            {
                contentCanvas = NexusUiFactory.CreateCanvas("App Content", 40, true);
                contentCanvas.transform.SetParent(transform, false);
            }

            if (contentHost == null)
            {
                var hostObject = new GameObject("Content Host", typeof(RectTransform));
                hostObject.transform.SetParent(contentCanvas.transform, false);
                contentHost = hostObject.GetComponent<RectTransform>();

                // Move any screens already parented to the canvas root into the host.
                for (int i = contentCanvas.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = contentCanvas.transform.GetChild(i);
                    if (child == contentHost)
                        continue;
                    child.SetParent(contentHost, false);
                }
            }

            ApplyNavigationLayout();
        }

        private Transform ContentRoot()
        {
            EnsureContentCanvas();
            return contentHost;
        }

        private GameObject BuildMissionsPlaceholder()
        {
            GameObject root = NexusUiFactory.CreatePanel(
                ContentRoot(),
                "Missions Placeholder",
                NexusTheme.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            NexusUiFactory.CreateText(
                root.transform,
                "Title",
                UiText.MissionsTitle,
                new Vector2(28f, 24f),
                new Vector2(520f, 40f),
                22f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            var body = NexusUiFactory.CreateText(
                root.transform,
                "Body",
                UiText.MissionsBody,
                new Vector2(28f, 80f),
                new Vector2(800f, 80f),
                14f,
                NexusTheme.MutedText);
            body.textWrappingMode = TextWrappingModes.Normal;
            NexusUiFactory.CreateButton(
                root.transform,
                "Explore",
                UiText.EnterExploreBattle,
                new Vector2(28f, 180f),
                new Vector2(220f, 44f),
                () => ShowScreen(AppScreen.Battle),
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f),
                NexusTheme.Gold,
                13f);
            return root;
        }

        private void BuildMainMenuBranding()
        {
            Canvas canvas = NexusUiFactory.CreateCanvas("App Main Menu Branding", 40, false);
            canvas.transform.SetParent(transform, false);
            menuBrandRoot = canvas.transform;
            NexusUiFactory.CreateText(canvas.transform, "Nexus", "NEXUS", new Vector2(116f, 104f), new Vector2(700f, 90f), 58f, NexusTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(canvas.transform, "Command", "GALACTIC FRONTIER COMMAND", new Vector2(122f, 194f), new Vector2(760f, 42f), 21f, NexusTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(canvas.transform, "Build", UiText.MainMenuTagline, new Vector2(122f, 244f), new Vector2(760f, 28f), 12f, NexusTheme.MutedText);
        }
    }
}
