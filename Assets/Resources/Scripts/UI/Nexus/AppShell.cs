using System.Collections;
using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Market;
using Assets.Resources.Scripts.Main;
using Assets.Resources.Scripts.Scene;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.DebugTools;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.Cosmetics;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Progression;
using Assets.Resources.Scripts.Unlock;
using Assets.Resources.Scripts.UI.Nexus.Tutorial;
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
        private readonly Dictionary<NavGroup, Button> groupNavButtons = new();
        private readonly Dictionary<NavGroup, TextMeshProUGUI> groupNavLabels = new();
        private readonly Dictionary<AppScreen, Button> subNavButtons = new();
        private Transform subNavHost;
        private QuestTrackerDrawer questTracker;
        private TextMeshProUGUI compactStatusLabel;

        private Canvas contentCanvas;
        private RectTransform contentHost;
        private BridgeScreen bridgeScreen;
        private MissionsScreen missionsScreen;
        private GameObject settingsRoot;
        private ExploreScreen exploreScreen;
        private DebugScreen debugScreen;
        private FormationScreen formationScreen;
        private ShipScreen shipScreen;
        private CraftingScreen craftingScreen;
        private MarketScreen marketScreen;
        private RecruitScreen recruitScreen;
        private InventoryScreen inventoryScreen;
        private CharactersScreen charactersScreen;
        private CardsScreen cardsScreen;
        private TextMeshProUGUI breadcrumbTitle;
        private TextMeshProUGUI statusShortcuts;
        private TextMeshProUGUI statusVersion;
        private MainScrollController mainController;
        private AppScreen activeScreen = AppScreen.Bridge;
        private bool mainReady;
        private bool navExpanded = true;
        private float navWidth = NexusTheme.NavExpandedWidth;
        private Transform chromeRoot;
        private Transform backdropRoot;
        private Transform menuBrandRoot;
        private RectTransform navigationRect;
        private RectTransform topBarRect;
        private RectTransform breadcrumbRect;
        private RectTransform notificationRect;
        private RectTransform statusBarRect;
        private RectTransform navHeaderRect;
        private RectTransform brandGroupRect;
        private RectTransform toggleRect;
        private TextMeshProUGUI brandTitle;
        private TextMeshProUGUI toggleLabel;
        private TextMeshProUGUI creditsLabel;
        private TextMeshProUGUI commanderLabel;
        private TextMeshProUGUI commanderIdLabel;
        private TextMeshProUGUI commanderLevelLabel;
        private TextMeshProUGUI deckPowerLabel;
        private NexusProgressUi.XpBar commanderXpBar;
        private Image commanderPortrait;
        private Image commanderFrame;
        private Image brandIcon;

        private static AppScreen PendingScreen { get; set; } = AppScreen.Bridge;

        private IEnumerator Start()
        {
            Instance = this;
            LocalizationUtil.Initialize();
            LocalizationUtil.LanguageChanged += OnLanguageChanged;
            CurrencyService.Changed += OnCreditsChanged;
            FeatureUnlockService.UnlocksChanged += OnUnlocksChanged;
            AvatarFrameService.UnlocksChanged += OnUnlocksChanged;
            ProgressionService.ProgressChanged += OnProgressChanged;
            BackgroundBattleHost.FinishedTick += OnBackgroundBattleFinished;
            if (DebugModeController.Instance != null)
                DebugModeController.Instance.Changed += OnDebugModeChanged;
            yield return null;

            string sceneName = SceneManager.GetActiveScene().name;
            BuildBackdrop();

            switch (sceneName)
            {
                case "MainMenuScene":
                    BuildMainMenuBranding();
                    LoadingOverlay.NotifySceneReady();
                    break;
                case "MainScene":
                    BuildChrome(true);
                    PrepareMainScene();
                    if (GetComponent<IdleEconomyTicker>() == null)
                        gameObject.AddComponent<IdleEconomyTicker>();
                    BackgroundBattleHost.Ensure();
                    // Scene reload can interrupt a coroutine mid-frame — restart if a fight is live.
                    // Also auto-start a farm fight when the combat deck is already AutoCombat.
                    BackgroundBattleHost.Ensure().TryBootstrapIdleCombat();
                    if (DataUtil.Instance != null)
                    {
                        WorldService.EnsureLoaded(DataUtil.Instance);
                        ShipService.EnsureLoaded(DataUtil.Instance);
                        IdleSettlementService.EnsureLoaded(DataUtil.Instance);
                        OnboardingService.EnsureLoaded(DataUtil.Instance);
                        ChapterQuestService.EnsureLoaded(DataUtil.Instance);
                        FeatureUnlockService.EnsureLoaded(DataUtil.Instance);
                        AvatarFrameService.EnsurePlayer();
                        AvatarFrameService.Evaluate();
                        Assets.Resources.Scripts.Gacha.GachaService.EnsureLoaded(DataUtil.Instance);
                        IdleSettlementService.OnAppResume(CardListManager.Instance?.cardEntities);
                    }
                    AppScreen initial = PendingScreen;
                    PendingScreen = AppScreen.Bridge;
                    ShowScreen(initial);
                    mainReady = true;
                    PushStartupNotifications();
                    FeatureUnlockUi.PresentPending(ShowScreen);
                    ChapterQuestService.TryBeginEntryFlow(null);
                    LoadingOverlay.NotifySceneReady();
                    break;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!mainReady) return;
            if (pauseStatus)
                IdleSettlementService.OnAppPause();
            else
                IdleSettlementService.OnAppResume(CardListManager.Instance?.cardEntities);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!mainReady) return;
            if (!hasFocus)
                IdleSettlementService.OnAppPause();
            else
                IdleSettlementService.OnAppResume(CardListManager.Instance?.cardEntities);
        }

        private void OnDestroy()
        {
            LocalizationUtil.LanguageChanged -= OnLanguageChanged;
            CurrencyService.Changed -= OnCreditsChanged;
            FeatureUnlockService.UnlocksChanged -= OnUnlocksChanged;
            AvatarFrameService.UnlocksChanged -= OnUnlocksChanged;
            ProgressionService.ProgressChanged -= OnProgressChanged;
            BackgroundBattleHost.FinishedTick -= OnBackgroundBattleFinished;
            if (DebugModeController.Instance != null)
                DebugModeController.Instance.Changed -= OnDebugModeChanged;
            if (Instance == this)
                Instance = null;
        }

        private void OnDebugModeChanged(bool enabled)
        {
            NotifyDebugModeChanged();
        }

        /// <summary>Rebuilds chrome when Debug Mode is toggled so the nav entry appears/hides.</summary>
        public void NotifyDebugModeChanged()
        {
            if (!mainReady && SceneManager.GetActiveScene().name != "MainScene")
                return;

            RewardPopup.Close();
            NexusDialog.Close();
            NexusSnackbar.Close();
            AppScreen restore = activeScreen;
            if (restore == AppScreen.Debug && DebugModeController.Instance?.IsEnabled != true)
                restore = AppScreen.Settings;

            if (chromeRoot != null) Destroy(chromeRoot.gameObject);
            if (contentCanvas != null) Destroy(contentCanvas.gameObject);
            contentCanvas = null;
            contentHost = null;
            bridgeScreen = null;
            missionsScreen = null;
            settingsRoot = null;
            exploreScreen = null;
            debugScreen = null;
            formationScreen = null;
            shipScreen = null;
            craftingScreen = null;
            marketScreen = null;
            recruitScreen = null;
            inventoryScreen = null;
            charactersScreen = null;
            cardsScreen = null;
            navigationButtons.Clear();
            navigationLabels.Clear();
            navigationIcons.Clear();
            groupNavButtons.Clear();
            groupNavLabels.Clear();
            subNavButtons.Clear();

            BuildChrome(true);
            ShowScreen(restore);
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
            RewardPopup.Close();
            NexusDialog.Close();
            NexusSnackbar.Close();
            AppScreen restore = activeScreen;
            if (chromeRoot != null) Destroy(chromeRoot.gameObject);
            if (contentCanvas != null) Destroy(contentCanvas.gameObject);
            if (menuBrandRoot != null) Destroy(menuBrandRoot.gameObject);

            contentCanvas = null;
            contentHost = null;
            bridgeScreen = null;
            missionsScreen = null;
            settingsRoot = null;
            exploreScreen = null;
            debugScreen = null;
            formationScreen = null;
            shipScreen = null;
            craftingScreen = null;
            marketScreen = null;
            recruitScreen = null;
            inventoryScreen = null;
            charactersScreen = null;
            cardsScreen = null;
            navigationButtons.Clear();
            navigationLabels.Clear();
            navigationIcons.Clear();
            groupNavButtons.Clear();
            groupNavLabels.Clear();
            subNavButtons.Clear();
            navigationRect = null;
            topBarRect = null;
            breadcrumbRect = null;
            notificationRect = null;
            statusBarRect = null;
            navHeaderRect = null;
            brandGroupRect = null;
            toggleRect = null;
            brandTitle = null;
            toggleLabel = null;
            creditsLabel = null;
            commanderLabel = null;
            commanderIdLabel = null;
            commanderLevelLabel = null;
            deckPowerLabel = null;
            commanderXpBar = null;
            commanderPortrait = null;
            commanderFrame = null;
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

            if (Input.GetKeyDown(KeyCode.F1)) Navigate(AppScreen.Bridge);
            else if (Input.GetKeyDown(KeyCode.F2)) Navigate(AppScreen.Battle);
            else if (Input.GetKeyDown(KeyCode.F3)) Navigate(AppScreen.Formation);
            else if (Input.GetKeyDown(KeyCode.F4)) Navigate(AppScreen.Characters);
            else if (Input.GetKeyDown(KeyCode.F5)) Navigate(AppScreen.Inventory);
            else if (Input.GetKeyDown(KeyCode.F6)) Navigate(AppScreen.Crafting);
            else if (Input.GetKeyDown(KeyCode.F7)) Navigate(AppScreen.Market);
            else if (Input.GetKeyDown(KeyCode.F8)) Navigate(AppScreen.Missions);
            else if (Input.GetKeyDown(KeyCode.F9)) Navigate(AppScreen.Recruit);
        }

        public static void RequestScreen(AppScreen screen)
        {
            PendingScreen = screen;
        }

        public void Navigate(AppScreen screen)
        {
            if (SceneManager.GetActiveScene().name == "MainScene")
            {
                if (!FeatureUnlockUi.CanOpen(screen))
                {
                    FeatureUnlockUi.ShowLocked(screen);
                    return;
                }

                ShowScreen(screen);
                TutorialGuideService.NotifyNavigated(screen);
            }
            else
            {
                PendingScreen = screen;
                LoadingOverlay.LoadScene(nameof(SceneLoader.SceneName.MainScene));
            }
        }

        private void ShowScreen(AppScreen screen)
        {
            if (screen == AppScreen.Debug && DebugModeController.Instance?.IsEnabled != true)
                screen = AppScreen.Settings;

            if (!FeatureUnlockUi.CanOpen(screen))
            {
                if (!mainReady)
                    screen = AppScreen.Bridge;
                else
                {
                    FeatureUnlockUi.ShowLocked(screen);
                    return;
                }
            }

            activeScreen = screen;
            SetActiveNavigation(screen);
            SetBreadcrumb(screen);
            RefreshCompactStatus();

            if (screen == AppScreen.Battle)
            {
                // Product: explore/select region first, then fully automatic BattleScene.
                EnsureContentCanvas();
                contentCanvas.gameObject.SetActive(true);
                HideNativeRoots();
                LegacyPanelAdapter.Hide(mainController);
                if (exploreScreen == null)
                {
                    exploreScreen = ExploreScreen.Build(
                        ContentRoot(),
                        () => ShowScreen(AppScreen.Formation),
                        () => ShowScreen(AppScreen.Ship),
                        () => ShowScreen(AppScreen.Market));
                }
                else
                {
                    exploreScreen.Rebuild();
                }

                exploreScreen.Root.SetActive(true);
                if (mainReady)
                {
                    FeatureUnlockService.Evaluate();
                    AvatarFrameService.Evaluate();
                    RefreshNavLockStyles();
                    FeatureUnlockUi.PresentPending(ShowScreen);
                    TutorialGuideService.PresentForScreen(activeScreen, this);
                }
                return;
            }

            EnsureContentCanvas();
            contentCanvas.gameObject.SetActive(true);
            HideNativeRoots();
            LegacyPanelAdapter.Hide(mainController);

            switch (screen)
            {
                case AppScreen.Bridge:
                    if (bridgeScreen == null)
                    {
                        bridgeScreen = BridgeScreen.Build(
                            ContentRoot(),
                            () => ShowScreen(AppScreen.Missions),
                            () => ShowScreen(AppScreen.Formation),
                            () => ShowScreen(AppScreen.Battle),
                            () => ShowScreen(AppScreen.Ship),
                            ShowScreen);
                    }
                    else
                    {
                        bridgeScreen.Rebuild();
                    }
                    bridgeScreen.Root.SetActive(true);
                    RefreshCommanderLabel();
                    BackgroundBattleHost.Ensure().TryBootstrapIdleCombat();
                    break;
                case AppScreen.Formation:
                    if (formationScreen == null)
                        formationScreen = FormationScreen.Build(ContentRoot());
                    else
                        formationScreen.Rebuild();
                    formationScreen.Root.SetActive(true);
                    break;
                case AppScreen.Ship:
                    if (shipScreen == null)
                        shipScreen = ShipScreen.Build(ContentRoot(), () => ShowScreen(AppScreen.Bridge));
                    else
                        shipScreen.Rebuild();
                    shipScreen.Root.SetActive(true);
                    break;
                case AppScreen.Crafting:
                    if (craftingScreen == null)
                        craftingScreen = CraftingScreen.Build(ContentRoot());
                    else
                        craftingScreen.Rebuild();
                    craftingScreen.Root.SetActive(true);
                    break;
                case AppScreen.Market:
                    if (marketScreen == null)
                        marketScreen = MarketScreen.Build(ContentRoot());
                    else
                        marketScreen.Rebuild();
                    marketScreen.Root.SetActive(true);
                    break;
                case AppScreen.Recruit:
                    if (recruitScreen == null)
                        recruitScreen = RecruitScreen.Build(ContentRoot(), ShowScreen);
                    else
                        recruitScreen.Rebuild();
                    recruitScreen.Root.SetActive(true);
                    break;
                case AppScreen.Inventory:
                    if (inventoryScreen == null)
                        inventoryScreen = InventoryScreen.Build(ContentRoot());
                    else
                        inventoryScreen.Rebuild();
                    inventoryScreen.Root.SetActive(true);
                    break;
                case AppScreen.Characters:
                    if (charactersScreen == null)
                        charactersScreen = CharactersScreen.Build(ContentRoot());
                    else
                        charactersScreen.Rebuild();
                    charactersScreen.Root.SetActive(true);
                    break;
                case AppScreen.Cards:
                    if (cardsScreen == null)
                        cardsScreen = CardsScreen.Build(ContentRoot());
                    else
                        cardsScreen.Rebuild();
                    cardsScreen.Root.SetActive(true);
                    break;
                case AppScreen.Settings:
                    if (settingsRoot != null)
                        Destroy(settingsRoot);
                    settingsRoot = SettingsScreen.Build(ContentRoot());
                    settingsRoot.SetActive(true);
                    break;
                case AppScreen.Debug:
                    if (debugScreen == null)
                        debugScreen = DebugScreen.Build(ContentRoot(), () => exploreScreen?.Rebuild());
                    else
                        debugScreen.Rebuild();
                    debugScreen.Root.SetActive(true);
                    break;
                case AppScreen.Missions:
                    if (missionsScreen == null)
                        missionsScreen = MissionsScreen.Build(ContentRoot(), ShowScreen);
                    else
                        missionsScreen.Rebuild();
                    missionsScreen.Root.SetActive(true);
                    break;
                default:
                    if (contentCanvas != null)
                        contentCanvas.gameObject.SetActive(false);
                    if (!LegacyPanelAdapter.TryShow(screen, mainController))
                        Debug.LogWarning($"AppShell: no handler for {screen}");
                    break;
            }

            if (mainReady)
            {
                FeatureUnlockService.Evaluate();
                AvatarFrameService.Evaluate();
                RefreshNavLockStyles();
                FeatureUnlockUi.PresentPending(ShowScreen);
                TutorialGuideService.PresentForScreen(activeScreen, this);
            }
        }

        private void HideNativeRoots()
        {
            if (bridgeScreen != null) bridgeScreen.Root.SetActive(false);
            if (missionsScreen != null) missionsScreen.Root.SetActive(false);
            if (settingsRoot != null) settingsRoot.SetActive(false);
            if (exploreScreen != null) exploreScreen.Root.SetActive(false);
            if (debugScreen != null) debugScreen.Root.SetActive(false);
            if (formationScreen != null) formationScreen.Root.SetActive(false);
            if (shipScreen != null) shipScreen.Root.SetActive(false);
            if (craftingScreen != null) craftingScreen.Root.SetActive(false);
            if (marketScreen != null) marketScreen.Root.SetActive(false);
            if (recruitScreen != null) recruitScreen.Root.SetActive(false);
            if (inventoryScreen != null) inventoryScreen.Root.SetActive(false);
            if (charactersScreen != null) charactersScreen.Root.SetActive(false);
            if (cardsScreen != null) cardsScreen.Root.SetActive(false);
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

            notificationRect = null;
            // Notification strip is gated by NexusTheme.NotificationBarEnabled (currently off).

            GameObject breadcrumb = NexusUiFactory.CreatePanel(
                chrome.transform,
                "Breadcrumb",
                NexusTheme.WithAlpha(NexusTheme.Surface, 0.96f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(navWidth, -NexusTheme.ChromeHeaderHeight),
                new Vector2(0f, -(NexusTheme.TopBarHeight + NexusTheme.NotificationBarHeight)));
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
            questTracker = new QuestTrackerDrawer();
            questTracker.Attach(chrome.transform, ShowScreen);
            ApplyNavigationLayout();
        }

        private void BuildTopBar(Transform parent)
        {
            var player = DataUtil.Instance?.currentPlayer;
            string commanderName = player?.playerName;
            if (string.IsNullOrWhiteSpace(commanderName)) commanderName = "COMMANDER";
            int level = player?.level ?? 1;
            int credits = player?.creditPoints ?? 0;
            string id = TruncatePlayerId(player?.playerID);

            AvatarFrameService.EnsurePlayer();
            const float portrait = 72f;
            var portraitImage = ProfilePortrait.Draw(parent, "Commander", new Vector2(8f, 4f), portrait);
            commanderPortrait = portraitImage;
            commanderFrame = portraitImage != null ? portraitImage.transform.parent.GetComponent<Image>() : null;

            float textX = 8f + portrait + 12f;
            commanderLabel = NexusUiFactory.CreateText(
                parent,
                "Commander",
                commanderName,
                new Vector2(textX, 6f),
                new Vector2(360f, 28f),
                18f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            commanderIdLabel = NexusUiFactory.CreateText(
                parent,
                "Commander Id",
                UiText.ProfileIdLabel(id),
                new Vector2(textX, 36f),
                new Vector2(280f, 20f),
                14f,
                NexusTheme.Cyan);
            commanderLevelLabel = NexusUiFactory.CreateText(
                parent,
                "Commander Level",
                $"LV.{level}",
                new Vector2(textX + 286f, 6f),
                new Vector2(72f, 22f),
                16f,
                NexusTheme.Gold,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            deckPowerLabel = NexusUiFactory.CreateText(
                parent,
                "DeckPower",
                UiText.DeckPowerLabel(NexusProgressUi.ActiveCombatPower()),
                new Vector2(textX + 360f, 6f),
                new Vector2(200f, 22f),
                14f,
                NexusTheme.Cyan,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            var xpRatio = NexusProgressUi.CommanderXpRatio(player, out var xpCur, out var xpNeed);
            commanderXpBar = NexusProgressUi.DrawBar(
                parent,
                "CommanderXp",
                new Vector2(textX + 286f, 36f),
                new Vector2(240f, 8f),
                NexusProgressUi.FormatCommanderXpLabel(level, xpCur, xpNeed),
                NexusTheme.Gold,
                xpRatio);
            creditsLabel = NexusUiFactory.CreateText(
                parent,
                "Credits",
                $"₵ {credits:N0}",
                new Vector2(880f, 24f),
                new Vector2(160f, 32f),
                16f,
                NexusTheme.Gold,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            NexusUiFactory.CreateButton(
                parent, "QuestToggle", UiText.QuestToggle,
                new Vector2(1060f, 20f), new Vector2(88f, 36f),
                () => questTracker?.Toggle(),
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.14f), NexusTheme.Cyan, 12f);

            var portraitBtn = portraitImage != null
                ? portraitImage.transform.parent.gameObject.GetComponent<Button>()
                : null;
            if (portraitBtn == null && portraitImage != null)
            {
                portraitBtn = portraitImage.transform.parent.gameObject.AddComponent<Button>();
                var pImg = portraitImage.transform.parent.GetComponent<Image>();
                if (pImg != null)
                {
                    pImg.raycastTarget = true;
                    var pc = portraitBtn.colors;
                    pc.normalColor = Color.white;
                    portraitBtn.colors = pc;
                }
            }
            if (portraitBtn != null)
            {
                portraitBtn.onClick.RemoveAllListeners();
                portraitBtn.onClick.AddListener(OpenAvatarMenu);
            }
        }

        private void OpenAvatarMenu()
        {
            string body = UiText.ProfileIdLabel(TruncatePlayerId(DataUtil.Instance?.currentPlayer?.playerID));
            if (DebugModeController.Instance != null && DebugModeController.Instance.IsEnabled)
            {
                NexusDialog.Show(
                    UiText.MenuSettings,
                    body,
                    UiText.MenuSettings,
                    () => Navigate(AppScreen.Settings),
                    UiText.MenuDebug,
                    () => Navigate(AppScreen.Debug));
            }
            else
            {
                NexusDialog.Show(
                    UiText.MenuSettings,
                    body,
                    UiText.MenuSettings,
                    () => Navigate(AppScreen.Settings),
                    UiText.Back,
                    null);
            }
        }

        private static string TruncatePlayerId(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "—";
            return raw.Length > 12 ? raw.Substring(0, 12) + "…" : raw;
        }

        private void OnCreditsChanged()
        {
            RefreshCreditsLabel();
        }

        /// <summary>
        /// Fired by <see cref="ProgressionService.ProgressChanged"/> — i.e. whenever XP/rewards are
        /// actually granted (idle gather success, farm/battle settlement, manual grants). This is the
        /// event-driven replacement for the old 0.5s poll, so UI only updates on real data changes.
        /// </summary>
        private void OnProgressChanged()
        {
            RefreshBattleProgressUi();
        }

        /// <summary>Fired once when a background (idle) battle settles.</summary>
        private void OnBackgroundBattleFinished()
        {
            RefreshBattleProgressUi();
        }

        /// <summary>
        /// Refreshes only the widgets whose data actually changed — the shell commander XP bar
        /// (in-place via <see cref="NexusProgressUi.ApplyBar"/>) and the Formation card XP bars
        /// (in-place via <see cref="NexusProgressUi.ApplyCardXpBars"/>). It never destroys/recreates
        /// any panel, so buttons never flicker on XP ticks. Screens like Bridge refresh their
        /// text/log panels on navigation (their <c>Rebuild</c>), not on every progression event.
        /// </summary>
        private void RefreshBattleProgressUi()
        {
            RefreshCommanderLabel();
            if (!mainReady)
                return;
            if (activeScreen == AppScreen.Formation)
                formationScreen?.RefreshProgressBars();
        }

        public void RefreshCreditsLabel()
        {
            if (creditsLabel == null) return;
            int credits = DataUtil.Instance?.currentPlayer?.creditPoints ?? 0;
            creditsLabel.text = $"₵ {credits:N0}";
        }

        private void BuildBreadcrumb(Transform parent)
        {
            NexusUiFactory.CreateText(parent, "Brand", $"{UiText.BrandTitle} ›", new Vector2(18f, 6f), new Vector2(180f, 20f), 11f, NexusTheme.DimText);
            breadcrumbTitle = NexusUiFactory.CreateText(
                parent,
                "Current",
                UiText.Breadcrumb(AppScreen.Bridge),
                new Vector2(206f, 6f),
                new Vector2(420f, 20f),
                12f,
                NexusTheme.Gold,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            var subHostGo = new GameObject("Sub Nav Host", typeof(RectTransform));
            subHostGo.transform.SetParent(parent, false);
            subNavHost = subHostGo.transform;
            var subRect = subHostGo.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 1f);
            subRect.anchorMax = new Vector2(1f, 1f);
            subRect.pivot = new Vector2(0f, 1f);
            subRect.anchoredPosition = new Vector2(640f, -4f);
            subRect.sizeDelta = new Vector2(-660f, 24f);
        }

        private void BuildStatusBar(Transform parent)
        {
            compactStatusLabel = NexusUiFactory.CreateText(
                parent,
                "CompactStatus",
                CompactStatusBar.BuildSummary(),
                new Vector2(16f, 2f),
                new Vector2(1180f, 18f),
                10f,
                NexusTheme.Text);
            statusShortcuts = NexusUiFactory.CreateText(
                parent,
                "Shortcuts",
                UiText.StatusShortcuts,
                new Vector2(1200f, 2f),
                new Vector2(240f, 18f),
                9f,
                NexusTheme.DimText,
                TextAlignmentOptions.Right);
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

        public void RefreshCompactStatus()
        {
            CompactStatusBar.Apply(compactStatusLabel);
            RefreshCommanderLabel();
        }

        private void BuildNavigation(Transform parent, bool fullNavigation)
        {
            BuildNavHeader(parent);

            float y = 70f;
            foreach (NavGroup group in System.Enum.GetValues(typeof(NavGroup)))
            {
                AddGroupNav(parent, group, y);
                y += 52f;
            }

            const float utilityY = 1000f;
            if (DebugModeController.Instance != null && DebugModeController.Instance.IsEnabled)
                AddNav(parent, AppScreen.Debug, utilityY - 52f);
            AddNav(parent, AppScreen.Settings, utilityY);

            if (!fullNavigation)
            {
                foreach (var pair in groupNavButtons)
                {
                    NavGroup g = pair.Key;
                    pair.Value.onClick.RemoveAllListeners();
                    if (g != NavGroup.StarMap)
                        pair.Value.onClick.AddListener(() => Navigate(NavGroupRules.DefaultScreen(g)));
                }

                foreach (var pair in navigationButtons)
                {
                    AppScreen target = pair.Key;
                    pair.Value.onClick.RemoveAllListeners();
                    pair.Value.onClick.AddListener(() => Navigate(target));
                }
            }
        }

        private void AddGroupNav(Transform parent, NavGroup group, float y)
        {
            float width = NavButtonWidth();
            AppScreen defaultScreen = NavGroupRules.DefaultScreen(group);
            GameObject buttonObject = NexusUiFactory.CreateBox(
                parent,
                $"NavGroup {group}",
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
            button.onClick.AddListener(() => Navigate(defaultScreen));

            Image icon = NexusUiFactory.CreateIcon(
                buttonObject.transform,
                "Icon",
                GroupIconSprite(group),
                new Vector2(12f, 8f),
                new Vector2(28f, 28f),
                NexusTheme.MutedText);

            TextMeshProUGUI label = NexusUiFactory.CreateText(
                buttonObject.transform,
                "Label",
                UiText.NavGroupLabel(group),
                new Vector2(48f, 8f),
                new Vector2(150f, 28f),
                14f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            groupNavButtons[group] = button;
            groupNavLabels[group] = label;
            navigationIcons[defaultScreen] = icon;
        }

        private static Sprite GroupIconSprite(NavGroup group) => group switch
        {
            NavGroup.Bridge => NexusCardVisual.UiIcon("Bell"),
            NavGroup.StarMap => NexusCardVisual.UiIcon("Battle"),
            NavGroup.Fleet => NexusCardVisual.UiIcon("Character"),
            NavGroup.Industry => NexusCardVisual.UiIcon("Building"),
            NavGroup.Starport => NexusCardVisual.UiIcon("Shop"),
            _ => NexusCardVisual.UiIcon("circle")
        };

        private void RebuildSubNav(NavGroup group)
        {
            subNavButtons.Clear();
            if (subNavHost == null) return;
            for (int i = subNavHost.childCount - 1; i >= 0; i--)
                Destroy(subNavHost.GetChild(i).gameObject);

            if (!NavGroupRules.HasSubNav(group))
                return;

            float x = 0f;
            foreach (var screen in NavGroupRules.SubScreens(group))
            {
                var btn = NexusUiFactory.CreateButton(
                    subNavHost, "Sub_" + screen, NavTitle(screen),
                    new Vector2(x, 0f), new Vector2(110f, 24f),
                    () => Navigate(screen),
                    NexusTheme.SurfaceRaised, NexusTheme.MutedText, 11f);
                subNavButtons[screen] = btn;
                x += 116f;
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
                UiText.BrandTitle,
                new Vector2(40f, 6f),
                new Vector2(126f, 28f),
                16f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            // The English name is far longer than the Chinese one, so let it shrink into the rail.
            brandTitle.textWrappingMode = TextWrappingModes.NoWrap;
            brandTitle.enableAutoSizing = true;
            brandTitle.fontSizeMin = 10f;
            brandTitle.fontSizeMax = 18f;

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
                AppScreen.Ship => "World",
                AppScreen.Formation => "Character",
                AppScreen.Characters => "Character",
                AppScreen.Cards => "Cards",
                AppScreen.Inventory => "Inventory",
                AppScreen.Crafting => "Building",
                AppScreen.Market => "Shop",
                AppScreen.Recruit => "Cards",
                AppScreen.Missions => "Add Icon",
                AppScreen.Settings => "Settings",
                AppScreen.Debug => "Settings",
                _ => "circle"
            };
            return NexusCardVisual.UiIcon(iconName);
        }

        private static string NavTitle(AppScreen screen) => screen switch
        {
            AppScreen.Bridge => UiText.ScreenBridge,
            AppScreen.Battle => UiText.ScreenBattle,
            AppScreen.Ship => UiText.ScreenShip,
            AppScreen.Formation => UiText.ScreenFormation,
            AppScreen.Characters => UiText.ScreenCharacters,
            AppScreen.Cards => UiText.ScreenCards,
            AppScreen.Inventory => UiText.ScreenInventory,
            AppScreen.Crafting => UiText.ScreenCrafting,
            AppScreen.Market => UiText.ScreenMarket,
            AppScreen.Recruit => UiText.ScreenRecruit,
            AppScreen.Missions => UiText.ScreenMissions,
            AppScreen.Settings => UiText.ScreenSettings,
            AppScreen.Debug => UiText.ScreenDebug,
            _ => UiText.ScreenBridge
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

            if (notificationRect != null)
            {
                notificationRect.offsetMin = new Vector2(navWidth, -(NexusTheme.TopBarHeight + NexusTheme.NotificationBarHeight));
                notificationRect.offsetMax = new Vector2(0f, -NexusTheme.TopBarHeight);
            }

            if (breadcrumbRect != null)
            {
                breadcrumbRect.offsetMin = new Vector2(navWidth, -NexusTheme.ChromeHeaderHeight);
                breadcrumbRect.offsetMax = new Vector2(0f, -(NexusTheme.TopBarHeight + NexusTheme.NotificationBarHeight));
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
                contentHost.offsetMax = new Vector2(0f, -NexusTheme.ChromeHeaderHeight);
            }

            ApplyNavHeaderLayout();
            ApplyNavItemLayout();
            RefreshNavLockStyles();
            LegacyPanelAdapter.FitToContentArea(mainController, navWidth);
            SetActiveNavigation(activeScreen);
            Canvas.ForceUpdateCanvases();
        }

        private void ApplyNavHeaderLayout()
        {
            if (brandGroupRect != null)
                brandGroupRect.gameObject.SetActive(navExpanded);

            if (brandTitle != null)
                brandTitle.text = UiText.BrandTitle;

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
            foreach (var pair in groupNavButtons)
            {
                var rect = pair.Value.GetComponent<RectTransform>();
                if (rect != null)
                    rect.sizeDelta = new Vector2(buttonWidth, rect.sizeDelta.y);

                if (groupNavLabels.TryGetValue(pair.Key, out TextMeshProUGUI label) && label != null)
                {
                    label.text = UiText.NavGroupLabel(pair.Key);
                    label.gameObject.SetActive(navExpanded);
                    if (navExpanded)
                    {
                        label.rectTransform.anchoredPosition = new Vector2(48f, -8f);
                        label.rectTransform.sizeDelta = new Vector2(buttonWidth - 56f, 28f);
                    }
                }
            }

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
                        iconRect.anchorMin = new Vector2(0f, 1f);
                        iconRect.anchorMax = new Vector2(0f, 1f);
                        iconRect.pivot = new Vector2(0f, 1f);
                        iconRect.anchoredPosition = new Vector2(12f, -8f);
                        iconRect.sizeDelta = new Vector2(28f, 28f);
                    }
                    else
                    {
                        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                        iconRect.pivot = new Vector2(0.5f, 0.5f);
                        iconRect.anchoredPosition = Vector2.zero;
                        iconRect.sizeDelta = new Vector2(28f, 28f);
                    }
                }

                if (navigationLabels.TryGetValue(pair.Key, out TextMeshProUGUI label) && label != null)
                {
                    label.text = NavTitle(pair.Key);
                    label.gameObject.SetActive(navExpanded);
                }
            }
        }

        private void SetActiveNavigation(AppScreen screen)
        {
            RefreshNavLockStyles();
            bool utilityScreen = screen == AppScreen.Settings || screen == AppScreen.Debug;
            NavGroup group = NavGroupRules.GroupOf(screen);

            if (utilityScreen)
            {
                if (subNavHost != null)
                {
                    for (int i = subNavHost.childCount - 1; i >= 0; i--)
                        Destroy(subNavHost.GetChild(i).gameObject);
                }

                subNavButtons.Clear();
                foreach (var pair in groupNavButtons)
                {
                    pair.Value.image.color = NexusTheme.Surface;
                    if (groupNavLabels.TryGetValue(pair.Key, out var gl) && gl != null)
                        gl.color = NexusTheme.MutedText;
                }
            }
            else
            {
                RebuildSubNav(group);
                foreach (var pair in groupNavButtons)
                {
                    bool active = pair.Key == group;
                    pair.Value.image.color = active
                        ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f)
                        : NexusTheme.Surface;
                    if (groupNavLabels.TryGetValue(pair.Key, out var gl) && gl != null)
                        gl.color = active ? NexusTheme.Gold : NexusTheme.MutedText;
                }

                foreach (var pair in subNavButtons)
                {
                    bool active = pair.Key == screen;
                    pair.Value.image.color = active
                        ? NexusTheme.WithAlpha(NexusTheme.Cyan, 0.2f)
                        : NexusTheme.SurfaceRaised;
                    var lbl = pair.Value.GetComponentInChildren<TextMeshProUGUI>();
                    if (lbl != null)
                        lbl.color = active ? NexusTheme.Cyan : NexusTheme.MutedText;
                }
            }

            foreach (var pair in navigationButtons)
            {
                bool locked = !FeatureUnlockUi.CanOpen(pair.Key);
                bool active = pair.Key == screen && !locked;
                if (locked)
                    continue;

                pair.Value.image.color = active
                    ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f)
                    : NexusTheme.Surface;

                if (navigationIcons.TryGetValue(pair.Key, out Image icon) && icon != null)
                    icon.color = active ? NexusTheme.Gold : NexusTheme.MutedText;

                if (navigationLabels.TryGetValue(pair.Key, out TextMeshProUGUI label) && label != null)
                    label.color = active ? NexusTheme.Gold : NexusTheme.MutedText;
            }
        }

        private void RefreshNavLockStyles()
        {
            foreach (var pair in groupNavButtons)
            {
                AppScreen probe = NavGroupRules.DefaultScreen(pair.Key);
                bool locked = !FeatureUnlockUi.CanOpen(probe);
                bool active = !IsUtilityScreen(activeScreen)
                    && NavGroupRules.GroupOf(activeScreen) == pair.Key
                    && !locked;
                if (!locked)
                {
                    pair.Value.image.color = active
                        ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f)
                        : NexusTheme.Surface;
                    if (groupNavLabels.TryGetValue(pair.Key, out var label) && label != null)
                        label.color = active ? NexusTheme.Gold : NexusTheme.MutedText;
                    continue;
                }

                pair.Value.image.color = NexusTheme.WithAlpha(NexusTheme.Surface, 0.55f);
                if (groupNavLabels.TryGetValue(pair.Key, out var lockLabel) && lockLabel != null)
                    lockLabel.color = NexusTheme.DimText;
            }

            foreach (var pair in navigationButtons)
            {
                bool locked = !FeatureUnlockUi.CanOpen(pair.Key);
                bool active = pair.Key == activeScreen && !locked;
                if (!locked)
                {
                    pair.Value.image.color = active
                        ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f)
                        : NexusTheme.Surface;
                    if (navigationIcons.TryGetValue(pair.Key, out Image icon) && icon != null)
                        icon.color = active ? NexusTheme.Gold : NexusTheme.MutedText;
                    if (navigationLabels.TryGetValue(pair.Key, out TextMeshProUGUI label) && label != null)
                        label.color = active ? NexusTheme.Gold : NexusTheme.MutedText;
                    continue;
                }

                pair.Value.image.color = NexusTheme.WithAlpha(NexusTheme.Surface, 0.55f);
                if (navigationIcons.TryGetValue(pair.Key, out Image lockIcon) && lockIcon != null)
                    lockIcon.color = NexusTheme.DimText;
                if (navigationLabels.TryGetValue(pair.Key, out TextMeshProUGUI lockLabel) && lockLabel != null)
                    lockLabel.color = NexusTheme.DimText;
            }
        }

        private static bool IsUtilityScreen(AppScreen screen) =>
            screen == AppScreen.Settings || screen == AppScreen.Debug;

        public void RefreshCommanderLabel()
        {
            var player = DataUtil.Instance?.currentPlayer;
            string commanderName = player?.playerName;
            if (string.IsNullOrWhiteSpace(commanderName)) commanderName = "COMMANDER";
            int level = Mathf.Max(1, player?.level ?? 1);
            if (commanderLabel != null)
                commanderLabel.text = commanderName;
            if (commanderIdLabel != null)
                commanderIdLabel.text = UiText.ProfileIdLabel(TruncatePlayerId(player?.playerID));
            if (commanderLevelLabel != null)
                commanderLevelLabel.text = $"LV.{level}";
            if (deckPowerLabel != null)
                deckPowerLabel.text = UiText.DeckPowerLabel(NexusProgressUi.ActiveCombatPower());
            var ratio = NexusProgressUi.CommanderXpRatio(player, out var cur, out var need);
            NexusProgressUi.ApplyBar(
                commanderXpBar,
                NexusProgressUi.FormatCommanderXpLabel(level, cur, need),
                ratio);
            if (commanderXpBar == null)
                Debug.LogWarning("[XP-UI] commanderXpBar is null — cannot refresh shell XP bar.");
        }

        public void RefreshProfileChrome()
        {
            RefreshCommanderLabel();
            ProfilePortrait.Apply(commanderFrame, commanderPortrait);
        }

        public void RebuildSettingsIfActive()
        {
            if (activeScreen != AppScreen.Settings) return;
            if (settingsRoot != null) Destroy(settingsRoot);
            settingsRoot = SettingsScreen.Build(ContentRoot());
            settingsRoot.SetActive(true);
        }

        private void OnUnlocksChanged()
        {
            RefreshNavLockStyles();
            RefreshProfileChrome();
            if (mainReady)
                FeatureUnlockUi.PresentPending(ShowScreen);
        }

        private static void PushStartupNotifications()
        {
            if (IdleSettlementService.PendingCount > 0)
                NexusNotificationBar.Push(UiText.NotificationPendingLoot(IdleSettlementService.PendingCount));
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

        private void BuildMainMenuBranding()
        {
            Canvas canvas = NexusUiFactory.CreateCanvas("App Main Menu Branding", 40, false);
            canvas.transform.SetParent(transform, false);
            menuBrandRoot = canvas.transform;
            NexusUiFactory.CreateText(canvas.transform, "Brand Title", UiText.BrandTitle, new Vector2(116f, 104f), new Vector2(700f, 90f), 58f, NexusTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(canvas.transform, "Brand Title Alt", UiText.BrandTitleAlt, new Vector2(122f, 194f), new Vector2(760f, 42f), 24f, NexusTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(canvas.transform, "Build", UiText.MainMenuTagline, new Vector2(122f, 244f), new Vector2(760f, 28f), 12f, NexusTheme.MutedText);
        }

        internal Button TryGetGroupNavButton(NavGroup group) =>
            groupNavButtons.TryGetValue(group, out var button) ? button : null;

        internal Button TryGetSubNavButton(AppScreen screen) =>
            subNavButtons.TryGetValue(screen, out var button) ? button : null;
    }
}
