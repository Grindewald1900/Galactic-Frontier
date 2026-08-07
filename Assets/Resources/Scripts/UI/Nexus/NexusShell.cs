using System.Collections;
using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Main;
using Assets.Resources.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Assets.Resources.Scripts.Main.GameStatusManager;

namespace Assets.Resources.Scripts.UI.Nexus
{
    internal enum NexusScreen
    {
        Bridge,
        Battle,
        Formation,
        Characters,
        Cards,
        Inventory,
        Crafting,
        Market,
        Missions,
        Settings
    }

    /// <summary>
    /// Runtime shell that maps the Figma desktop layout onto the existing scene controllers.
    /// It owns global chrome and the new Bridge/Missions views, while delegating legacy gameplay
    /// panels to MainScrollController instead of duplicating their business logic.
    /// </summary>
    /// <remarks>
    /// NexusUiBootstrap installs one shell after supported scenes load. PendingScreen preserves the
    /// requested destination when navigation crosses between BattleScene and MainScene.
    /// </remarks>
    public sealed class NexusShell : MonoBehaviour
    {
        private readonly Dictionary<NexusScreen, Button> navigationButtons = new();
        private readonly Dictionary<NexusScreen, string> screenTitles = new()
        {
            { NexusScreen.Bridge, "BRIDGE" },
            { NexusScreen.Battle, "EXPLORE / CARD BATTLE" },
            { NexusScreen.Formation, "FORMATION" },
            { NexusScreen.Characters, "CHARACTERS" },
            { NexusScreen.Cards, "CARDS" },
            { NexusScreen.Inventory, "INVENTORY" },
            { NexusScreen.Crafting, "CRAFTING" },
            { NexusScreen.Market, "MARKETPLACE" },
            { NexusScreen.Missions, "MISSIONS" },
            { NexusScreen.Settings, "SETTINGS" }
        };

        private Canvas contentCanvas;
        private GameObject dashboardRoot;
        private GameObject missionsRoot;
        private TextMeshProUGUI breadcrumbTitle;
        private MainScrollController mainController;
        private NexusScreen activeScreen;
        private bool mainSceneReady;

        private IEnumerator Start()
        {
            yield return null;

            string sceneName = SceneManager.GetActiveScene().name;
            BuildBackdrop();
            ApplyThemeToScene();

            switch (sceneName)
            {
                case "MainMenuScene":
                    BuildMainMenuBranding();
                    break;
                case "MainScene":
                    BuildChrome(true);
                    PrepareMainScene();
                    NexusScreen initialScreen = PendingScreen.Value;
                    PendingScreen.Value = NexusScreen.Bridge;
                    ShowScreen(initialScreen);
                    mainSceneReady = true;
                    break;
                case "BattleScene":
                    BuildChrome(false);
                    SetActiveNavigation(NexusScreen.Battle);
                    SetBreadcrumb(NexusScreen.Battle);
                    break;
            }
        }

        private void Update()
        {
            if (!mainSceneReady)
                return;

            if (Input.GetKeyDown(KeyCode.F1)) ShowScreen(NexusScreen.Bridge);
            else if (Input.GetKeyDown(KeyCode.F2)) ShowScreen(NexusScreen.Battle);
            else if (Input.GetKeyDown(KeyCode.F3)) ShowScreen(NexusScreen.Formation);
            else if (Input.GetKeyDown(KeyCode.F4)) ShowScreen(NexusScreen.Characters);
            else if (Input.GetKeyDown(KeyCode.F5)) ShowScreen(NexusScreen.Inventory);
            else if (Input.GetKeyDown(KeyCode.F6)) ShowScreen(NexusScreen.Crafting);
            else if (Input.GetKeyDown(KeyCode.F7)) ShowScreen(NexusScreen.Market);
            else if (Input.GetKeyDown(KeyCode.F8)) ShowScreen(NexusScreen.Missions);
        }

        private void BuildBackdrop()
        {
            Canvas canvas = NexusUiFactory.CreateCanvas("Nexus Backdrop", -100, false);
            canvas.transform.SetParent(transform, false);
            NexusUiFactory.CreatePanel(
                canvas.transform,
                "Background",
                NexusTheme.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            var glow = NexusUiFactory.CreatePanel(
                canvas.transform,
                "Top Glow",
                NexusTheme.WithAlpha(NexusTheme.Purple, 0.035f),
                new Vector2(0f, 0.76f),
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            glow.GetComponent<Image>().raycastTarget = false;
        }

        private void BuildChrome(bool fullNavigation)
        {
            Canvas chrome = NexusUiFactory.CreateCanvas("Nexus Chrome", 100, true);
            chrome.transform.SetParent(transform, false);

            GameObject navigation = NexusUiFactory.CreatePanel(
                chrome.transform,
                "Navigation Rail",
                NexusTheme.Surface,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                Vector2.zero,
                new Vector2(NexusTheme.NavigationWidth, 0f));

            GameObject topBar = NexusUiFactory.CreatePanel(
                chrome.transform,
                "Top Bar",
                NexusTheme.Surface,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(NexusTheme.NavigationWidth, -NexusTheme.TopBarHeight),
                Vector2.zero);

            GameObject breadcrumb = NexusUiFactory.CreatePanel(
                chrome.transform,
                "Breadcrumb",
                NexusTheme.WithAlpha(NexusTheme.Surface, 0.96f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(
                    NexusTheme.NavigationWidth,
                    -NexusTheme.TopBarHeight - NexusTheme.BreadcrumbHeight),
                new Vector2(0f, -NexusTheme.TopBarHeight));

            GameObject statusBar = NexusUiFactory.CreatePanel(
                chrome.transform,
                "Status Bar",
                NexusTheme.Surface,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(NexusTheme.NavigationWidth, 0f),
                new Vector2(0f, NexusTheme.StatusBarHeight));

            AddChromeBorders(navigation, topBar, breadcrumb, statusBar);
            BuildTopBar(topBar.transform);
            BuildBreadcrumb(breadcrumb.transform);
            BuildStatusBar(statusBar.transform);
            BuildNavigation(navigation.transform, fullNavigation);
        }

        private static void AddChromeBorders(params GameObject[] objects)
        {
            foreach (GameObject target in objects)
            {
                var outline = target.AddComponent<Outline>();
                outline.effectColor = NexusTheme.BorderSoft;
                outline.effectDistance = new Vector2(1f, -1f);
                outline.useGraphicAlpha = false;
            }
        }

        private void BuildTopBar(Transform parent)
        {
            GameObject portrait = NexusUiFactory.CreateBox(
                parent,
                "Commander Mark",
                new Vector2(18f, 10f),
                new Vector2(36f, 36f),
                NexusTheme.WithAlpha(NexusTheme.Purple, 0.42f),
                NexusTheme.Purple);
            NexusUiFactory.CreateText(
                portrait.transform,
                "Mark",
                "NX",
                Vector2.zero,
                new Vector2(36f, 36f),
                19f,
                NexusTheme.Text,
                TextAlignmentOptions.Center,
                FontStyles.Bold);

            string commanderName = DataUtil.Instance?.currentPlayer?.playerName;
            if (string.IsNullOrWhiteSpace(commanderName)) commanderName = "COMMANDER";
            int commanderLevel = DataUtil.Instance?.currentPlayer?.level ?? 42;
            NexusUiFactory.CreateText(
                parent,
                "Commander",
                $"{commanderName}\n<size=11><color=#F0B429>LV.{commanderLevel}</color></size>",
                new Vector2(64f, 8f),
                new Vector2(170f, 44f),
                15f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            int credits = DataUtil.Instance?.currentPlayer?.creditPoints ?? 42850;
            AddResource(parent, "⚡", "148", "/200", new Vector2(250f, 17f), NexusTheme.Cyan);
            AddResource(parent, "₵", credits.ToString("N0"), string.Empty, new Vector2(405f, 17f), NexusTheme.Gold);
            AddResource(parent, "⬡", "1,240", string.Empty, new Vector2(570f, 17f), NexusTheme.MutedText);
            AddResource(parent, "◆", "320", string.Empty, new Vector2(720f, 17f), NexusTheme.Purple);

            NexusUiFactory.CreateText(
                parent,
                "Clock",
                "STARDATE 2341.08.02",
                new Vector2(1450f, 18f),
                new Vector2(300f, 24f),
                12f,
                NexusTheme.DimText,
                TextAlignmentOptions.Right);
        }

        private static void AddResource(
            Transform parent,
            string icon,
            string value,
            string suffix,
            Vector2 position,
            Color color)
        {
            NexusUiFactory.CreateText(
                parent,
                $"Resource {icon}",
                $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{icon}  {value}</color> <size=11><color=#535E78>{suffix}</color></size>",
                position,
                new Vector2(145f, 24f),
                14f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
        }

        private void BuildBreadcrumb(Transform parent)
        {
            NexusUiFactory.CreateText(
                parent,
                "Brand",
                "NEXUS  ›",
                new Vector2(18f, 8f),
                new Vector2(110f, 22f),
                11f,
                NexusTheme.DimText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            breadcrumbTitle = NexusUiFactory.CreateText(
                parent,
                "Current Screen",
                "BRIDGE",
                new Vector2(128f, 8f),
                new Vector2(720f, 22f),
                11f,
                NexusTheme.Gold,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
        }

        private static void BuildStatusBar(Transform parent)
        {
            NexusUiFactory.CreateText(
                parent,
                "Shortcuts",
                "F1 BRIDGE   F2 EXPLORE   F3 FORMATION   F4 CHARACTERS   F5 INVENTORY   F6 CRAFTING   F7 MARKET   F8 MISSIONS",
                new Vector2(18f, 4f),
                new Vector2(900f, 18f),
                10f,
                NexusTheme.DimText);
            NexusUiFactory.CreateText(
                parent,
                "Version",
                "NEXUS COMMAND v2.4.1",
                new Vector2(1450f, 4f),
                new Vector2(360f, 18f),
                10f,
                NexusTheme.DimText,
                TextAlignmentOptions.Right);
        }

        private void BuildNavigation(Transform parent, bool fullNavigation)
        {
            NexusUiFactory.CreateText(
                parent,
                "Logo",
                "N",
                new Vector2(10f, 14f),
                new Vector2(52f, 52f),
                22f,
                NexusTheme.Gold,
                TextAlignmentOptions.Center,
                FontStyles.Bold);

            AddNavigationButton(parent, NexusScreen.Bridge, "BR", 86f);
            AddNavigationButton(parent, NexusScreen.Battle, "EX", 144f);
            AddNavigationButton(parent, NexusScreen.Formation, "FM", 202f);
            AddNavigationButton(parent, NexusScreen.Characters, "CH", 260f);
            AddNavigationButton(parent, NexusScreen.Cards, "CD", 318f);
            AddNavigationButton(parent, NexusScreen.Inventory, "IN", 376f);
            AddNavigationButton(parent, NexusScreen.Crafting, "CR", 434f);
            AddNavigationButton(parent, NexusScreen.Market, "MK", 492f);
            AddNavigationButton(parent, NexusScreen.Missions, "MS", 550f);

            Button settings = NexusUiFactory.CreateButton(
                parent,
                "Navigation Settings",
                "⚙",
                new Vector2(10f, 998f),
                new Vector2(52f, 44f),
                () => Navigate(NexusScreen.Settings),
                NexusTheme.Surface,
                NexusTheme.MutedText,
                18f);
            navigationButtons[NexusScreen.Settings] = settings;

            if (!fullNavigation)
            {
                foreach (KeyValuePair<NexusScreen, Button> item in navigationButtons)
                {
                    NexusScreen target = item.Key;
                    item.Value.onClick.RemoveAllListeners();
                    if (target != NexusScreen.Battle)
                        item.Value.onClick.AddListener(() => ReturnToMain(target));
                }
            }
        }

        private void AddNavigationButton(Transform parent, NexusScreen screen, string glyph, float y)
        {
            Button button = NexusUiFactory.CreateButton(
                parent,
                $"Navigation {screen}",
                glyph,
                new Vector2(10f, y),
                new Vector2(52f, 48f),
                () => Navigate(screen),
                NexusTheme.Surface,
                NexusTheme.MutedText,
                17f);
            navigationButtons[screen] = button;
        }

        private void PrepareMainScene()
        {
            mainController = MainScrollController.Instance ?? FindFirstObjectByType<MainScrollController>();
            GameObject oldNavigation = GameObject.Find("MainScrollView");
            if (oldNavigation != null)
                oldNavigation.SetActive(false);

            if (mainController?.panels == null)
                return;

            foreach (GameObject panel in mainController.panels)
            {
                if (panel == null || panel.transform is not RectTransform rect)
                    continue;

                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(NexusTheme.NavigationWidth + 18f, NexusTheme.StatusBarHeight + 12f);
                rect.offsetMax = new Vector2(-16f, -NexusTheme.TopBarHeight - NexusTheme.BreadcrumbHeight - 10f);
            }
        }

        private void Navigate(NexusScreen screen)
        {
            if (SceneManager.GetActiveScene().name == "MainScene")
                ShowScreen(screen);
            else
                ReturnToMain(screen);
        }

        private void ShowScreen(NexusScreen screen)
        {
            activeScreen = screen;
            SetActiveNavigation(screen);
            SetBreadcrumb(screen);

            if (screen == NexusScreen.Battle)
            {
                SceneManager.LoadScene("BattleScene");
                return;
            }

            EnsureContentCanvas();
            if (dashboardRoot != null) dashboardRoot.SetActive(false);
            if (missionsRoot != null) missionsRoot.SetActive(false);

            if (screen == NexusScreen.Bridge)
            {
                HideExistingPanels();
                if (dashboardRoot == null) dashboardRoot = BuildDashboard();
                dashboardRoot.SetActive(true);
                return;
            }

            if (screen == NexusScreen.Missions)
            {
                HideExistingPanels();
                if (missionsRoot == null) missionsRoot = BuildMissions();
                missionsRoot.SetActive(true);
                return;
            }

            if (TryMapScreen(screen, out CurrentScene target))
                ShowExistingPanel(target);
        }

        private static bool TryMapScreen(NexusScreen screen, out CurrentScene scene)
        {
            switch (screen)
            {
                case NexusScreen.Formation:
                    scene = CurrentScene.BATTLE_MENU;
                    return true;
                case NexusScreen.Characters:
                    scene = CurrentScene.CHARACTER_MENU;
                    return true;
                case NexusScreen.Cards:
                    scene = CurrentScene.CARDS_MENU;
                    return true;
                case NexusScreen.Inventory:
                    scene = CurrentScene.INVENTORY_MENU;
                    return true;
                case NexusScreen.Crafting:
                    scene = CurrentScene.BUILDING_MENU;
                    return true;
                case NexusScreen.Market:
                    scene = CurrentScene.SHOP_MENU;
                    return true;
                case NexusScreen.Settings:
                    scene = CurrentScene.SETTINGS_MENU;
                    return true;
                default:
                    scene = CurrentScene.MAIN_SCENE;
                    return false;
            }
        }

        private void ShowExistingPanel(CurrentScene scene)
        {
            if (mainController == null)
                return;

            mainController.ShowPanel(scene);
            StartCoroutine(RestyleNextFrame());
        }

        private IEnumerator RestyleNextFrame()
        {
            yield return null;
            ApplyThemeToScene();
        }

        private void HideExistingPanels()
        {
            mainController?.HideAllPanels();
        }

        private void ReturnToMain(NexusScreen target)
        {
            PendingScreen.Value = target;
            SceneManager.LoadScene("MainScene");
        }

        private void SetActiveNavigation(NexusScreen screen)
        {
            foreach (KeyValuePair<NexusScreen, Button> pair in navigationButtons)
            {
                bool active = pair.Key == screen;
                pair.Value.image.color = active
                    ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f)
                    : NexusTheme.Surface;

                TextMeshProUGUI label = pair.Value.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.color = active ? NexusTheme.Gold : NexusTheme.MutedText;
            }
        }

        private void SetBreadcrumb(NexusScreen screen)
        {
            if (breadcrumbTitle != null && screenTitles.TryGetValue(screen, out string title))
                breadcrumbTitle.text = title;
        }

        private void EnsureContentCanvas()
        {
            if (contentCanvas != null)
                return;

            contentCanvas = NexusUiFactory.CreateCanvas("Nexus Content", 40, true);
            contentCanvas.transform.SetParent(transform, false);
        }

        private GameObject BuildDashboard()
        {
            GameObject root = NexusUiFactory.CreatePanel(
                contentCanvas.transform,
                "Bridge Dashboard",
                NexusTheme.Background,
                Vector2.zero,
                Vector2.one,
                new Vector2(NexusTheme.NavigationWidth, NexusTheme.StatusBarHeight),
                new Vector2(0f, -NexusTheme.TopBarHeight - NexusTheme.BreadcrumbHeight));

            NexusUiFactory.CreateText(root.transform, "Title", "BRIDGE", new Vector2(28f, 24f), new Vector2(520f, 44f), 30f, NexusTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(root.transform, "Date", "STARDATE 2341.08.02 — COMMAND DAY 14", new Vector2(28f, 68f), new Vector2(520f, 22f), 13f, NexusTheme.MutedText);
            NexusUiFactory.CreateText(root.transform, "Status", "● ALL SYSTEMS NOMINAL", new Vector2(1460f, 34f), new Vector2(310f, 24f), 13f, NexusTheme.Green, TextAlignmentOptions.Right);

            GameObject banner = NexusUiFactory.CreateBox(root.transform, "Event Banner", new Vector2(28f, 104f), new Vector2(1740f, 48f), NexusTheme.WithAlpha(NexusTheme.Gold, 0.06f), NexusTheme.WithAlpha(NexusTheme.Gold, 0.38f));
            NexusUiFactory.CreateText(banner.transform, "Text", "ⓘ  LIMITED EVENT: DIMENSIONAL STORM ENDS IN 47 HOURS. Complete it to earn an exclusive ship blueprint.", new Vector2(18f, 13f), new Vector2(1350f, 24f), 13f, NexusTheme.Gold);
            NexusUiFactory.CreateButton(banner.transform, "Details", "VIEW DETAILS", new Vector2(1560f, 8f), new Vector2(150f, 32f), null, NexusTheme.WithAlpha(NexusTheme.Gold, 0.12f), NexusTheme.Gold, 12f);

            int power = DataUtil.Instance?.currentPlayer?.combatPower ?? 92480;
            int credits = DataUtil.Instance?.currentPlayer?.creditPoints ?? 284000;
            int progress = DataUtil.Instance?.currentPlayer?.explorationProgress ?? 47;
            int cards = CardListManager.Instance?.GetCardEntities()?.Count ?? 8;
            AddStatCard(root.transform, new Vector2(28f, 176f), "COMBAT POWER", power.ToString("N0"), "+1,240", NexusTheme.Gold);
            AddStatCard(root.transform, new Vector2(372f, 176f), "EXPLORATION", $"{progress}%", "+12% THIS WEEK", NexusTheme.Cyan);
            AddStatCard(root.transform, new Vector2(716f, 176f), "TOTAL ASSETS", $"{credits / 1000f:0.#}K₵", "+6,400₵", NexusTheme.Purple);
            AddStatCard(root.transform, new Vector2(1060f, 176f), "DAILY ACTIONS", $"{Mathf.Min(cards, 12)} / 12", $"{Mathf.Min(cards, 12)} COMPLETE", NexusTheme.Green);

            GameObject missions = NexusUiFactory.CreateBox(root.transform, "Mission Overview", new Vector2(28f, 328f), new Vector2(1040f, 490f), NexusTheme.Surface, NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(missions.transform, "Heading", "MISSION OVERVIEW", new Vector2(22f, 18f), new Vector2(300f, 32f), 20f, NexusTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(missions.transform, "Eyebrow", "MISSIONS", new Vector2(770f, 22f), new Vector2(230f, 24f), 11f, NexusTheme.DimText, TextAlignmentOptions.Right, FontStyles.Bold);
            AddMissionRow(missions.transform, 74f, "QUANTUM RIFT · FRONTLINE", "72%", "ACTIVE", NexusTheme.Cyan);
            AddMissionRow(missions.transform, 164f, "ABANDONED MINE PURGE", "100%", "COMPLETE", NexusTheme.Green);
            AddMissionRow(missions.transform, 254f, "ABYSSAL FRONTIER RECON", "38%", "1 HOUR", NexusTheme.Cyan);
            AddMissionRow(missions.transform, 344f, "CONVOY ESCORT", "0%", "STANDBY", NexusTheme.MutedText);

            GameObject log = NexusUiFactory.CreateBox(root.transform, "Event Log", new Vector2(1090f, 328f), new Vector2(678f, 490f), NexusTheme.Surface, NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(log.transform, "Heading", "EVENT LOG", new Vector2(22f, 18f), new Vector2(300f, 32f), 20f, NexusTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(log.transform, "Eyebrow", "EVENT LOG", new Vector2(430f, 22f), new Vector2(220f, 24f), 11f, NexusTheme.DimText, TextAlignmentOptions.Right, FontStyles.Bold);
            AddLogRow(log.transform, 80f, "Night Owl reached level 38", "09:41", NexusTheme.Gold);
            AddLogRow(log.transform, 150f, "Sector IV-A explored; received ×12 upgrade stones", "09:38", NexusTheme.Green);
            AddLogRow(log.transform, 220f, "Market order sold for +6,400₵", "09:21", NexusTheme.Cyan);
            AddLogRow(log.transform, 290f, "New limited challenge: Dimensional Storm", "08:55", NexusTheme.Purple);
            AddLogRow(log.transform, 360f, "Daily reset complete; energy restored", "08:30", NexusTheme.MutedText);

            NexusUiFactory.CreateButton(root.transform, "Collect", "COLLECT ALL", new Vector2(1090f, 842f), new Vector2(210f, 44f), null, NexusTheme.WithAlpha(NexusTheme.Gold, 0.14f), NexusTheme.Gold, 13f);
            NexusUiFactory.CreateButton(root.transform, "Energy", "RESTORE ENERGY", new Vector2(1314f, 842f), new Vector2(210f, 44f), null, NexusTheme.SurfaceRaised, NexusTheme.Cyan, 13f);
            NexusUiFactory.CreateButton(root.transform, "Today", "TODAY'S MISSIONS", new Vector2(1538f, 842f), new Vector2(210f, 44f), () => ShowScreen(NexusScreen.Missions), NexusTheme.SurfaceRaised, NexusTheme.Text, 13f);
            return root;
        }

        private static void AddStatCard(Transform parent, Vector2 position, string label, string value, string delta, Color accent)
        {
            GameObject card = NexusUiFactory.CreateBox(parent, $"Stat {label}", position, new Vector2(320f, 128f), NexusTheme.SurfaceRaised, NexusTheme.BorderSoft);
            NexusUiFactory.CreatePanel(card.transform, "Accent", accent, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(3f, 0f));
            NexusUiFactory.CreateText(card.transform, "Value", value, new Vector2(20f, 22f), new Vector2(270f, 38f), 27f, NexusTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(card.transform, "Label", label, new Vector2(20f, 64f), new Vector2(270f, 22f), 12f, NexusTheme.MutedText);
            NexusUiFactory.CreateText(card.transform, "Delta", delta, new Vector2(20f, 94f), new Vector2(270f, 20f), 11f, accent);
        }

        private static void AddMissionRow(Transform parent, float y, string title, string progress, string status, Color accent)
        {
            GameObject row = NexusUiFactory.CreateBox(parent, $"Mission {title}", new Vector2(18f, y), new Vector2(1004f, 74f), NexusTheme.SurfaceRaised, NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(row.transform, "Title", title, new Vector2(18f, 12f), new Vector2(610f, 24f), 14f, NexusTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(row.transform, "Status", status, new Vector2(815f, 12f), new Vector2(160f, 22f), 11f, accent, TextAlignmentOptions.Right);
            GameObject track = NexusUiFactory.CreateBox(row.transform, "Track", new Vector2(18f, 48f), new Vector2(930f, 4f), NexusTheme.BorderSoft);
            float parsed = float.TryParse(progress.TrimEnd('%'), out float result) ? Mathf.Clamp01(result / 100f) : 0f;
            NexusUiFactory.CreateBox(track.transform, "Fill", Vector2.zero, new Vector2(930f * parsed, 4f), accent);
            NexusUiFactory.CreateText(row.transform, "Progress", progress, new Vector2(930f, 40f), new Vector2(55f, 20f), 10f, NexusTheme.DimText, TextAlignmentOptions.Right);
        }

        private static void AddLogRow(Transform parent, float y, string message, string time, Color accent)
        {
            NexusUiFactory.CreateText(parent, "Bullet", "●", new Vector2(22f, y + 3f), new Vector2(22f, 22f), 10f, accent);
            NexusUiFactory.CreateText(parent, "Message", message, new Vector2(50f, y), new Vector2(500f, 40f), 12f, NexusTheme.Text);
            NexusUiFactory.CreateText(parent, "Time", time, new Vector2(560f, y), new Vector2(90f, 22f), 10f, NexusTheme.DimText, TextAlignmentOptions.Right);
        }

        private GameObject BuildMissions()
        {
            GameObject root = NexusUiFactory.CreatePanel(
                contentCanvas.transform,
                "Missions Screen",
                NexusTheme.Background,
                Vector2.zero,
                Vector2.one,
                new Vector2(NexusTheme.NavigationWidth, NexusTheme.StatusBarHeight),
                new Vector2(0f, -NexusTheme.TopBarHeight - NexusTheme.BreadcrumbHeight));

            NexusUiFactory.CreateText(root.transform, "Title", "MISSIONS", new Vector2(28f, 24f), new Vector2(600f, 44f), 30f, NexusTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            string[] tabs = { "MAIN", "SIDE", "DAILY", "EVENT" };
            for (int i = 0; i < tabs.Length; i++)
            {
                bool active = i == 0;
                NexusUiFactory.CreateButton(root.transform, $"Tab {tabs[i]}", tabs[i], new Vector2(28f + i * 130f, 82f), new Vector2(118f, 38f), null, active ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.15f) : NexusTheme.Surface, active ? NexusTheme.Gold : NexusTheme.MutedText, 12f);
            }

            GameObject list = NexusUiFactory.CreateBox(root.transform, "Mission List", new Vector2(28f, 140f), new Vector2(940f, 720f), NexusTheme.Surface, NexusTheme.BorderSoft);
            AddMissionCard(list.transform, 22f, "QUANTUM RIFT: FRONTLINE BREAK", "ELITE · CHAPTER 3 · MAIN", "BLUEPRINT SHARD ×3  ·  CREDITS 8,000  ·  EXP 1,200", NexusTheme.Gold, true);
            AddMissionCard(list.transform, 206f, "ABYSSAL FRONTIER: RECON", "NORMAL · CHAPTER 3 · MAIN", "ALLOY ×20  ·  CREDITS 3,200", NexusTheme.Cyan, false);
            AddMissionCard(list.transform, 390f, "CONVOY ESCORT: LOST ROUTE", "NORMAL · CHAPTER 2 · SIDE", "CREDITS 2,400  ·  UPGRADE STONE ×6", NexusTheme.Green, false);

            GameObject details = NexusUiFactory.CreateBox(root.transform, "Mission Details", new Vector2(990f, 140f), new Vector2(778f, 720f), NexusTheme.Surface, NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(details.transform, "Name", "QUANTUM RIFT: FRONTLINE BREAK", new Vector2(28f, 28f), new Vector2(700f, 38f), 23f, NexusTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(details.transform, "Chapter", "CHAPTER 3", new Vector2(28f, 70f), new Vector2(300f, 22f), 12f, NexusTheme.Gold);
            NexusUiFactory.CreateText(details.transform, "Objective Title", "OBJECTIVES", new Vector2(28f, 128f), new Vector2(300f, 28f), 15f, NexusTheme.MutedText, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(details.transform, "Objectives", "□ Defeat the Fission Commander\n\n□ Protect the data terminal for 3 minutes\n\n□ Evacuate allied personnel", new Vector2(28f, 176f), new Vector2(680f, 180f), 14f, NexusTheme.Text);
            NexusUiFactory.CreateText(details.transform, "Reward Title", "REWARD PREVIEW", new Vector2(28f, 390f), new Vector2(300f, 28f), 15f, NexusTheme.MutedText, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(details.transform, "Rewards", "◆ BLUEPRINT SHARD ×3     ₵ 8,000     EXP 1,200", new Vector2(28f, 438f), new Vector2(680f, 38f), 15f, NexusTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateButton(details.transform, "Continue", "CONTINUE MISSION", new Vector2(28f, 626f), new Vector2(700f, 50f), () => ShowScreen(NexusScreen.Battle), NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f), NexusTheme.Gold, 15f);
            return root;
        }

        private static void AddMissionCard(Transform parent, float y, string title, string meta, string rewards, Color accent, bool active)
        {
            GameObject card = NexusUiFactory.CreateBox(parent, $"Mission {title}", new Vector2(20f, y), new Vector2(900f, 160f), active ? NexusTheme.SurfaceHover : NexusTheme.SurfaceRaised, active ? accent : NexusTheme.BorderSoft);
            NexusUiFactory.CreatePanel(card.transform, "Accent", accent, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(3f, 0f));
            NexusUiFactory.CreateText(card.transform, "Title", title, new Vector2(22f, 20f), new Vector2(620f, 30f), 18f, NexusTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(card.transform, "Meta", meta, new Vector2(22f, 58f), new Vector2(620f, 22f), 12f, accent);
            NexusUiFactory.CreateText(card.transform, "Rewards", rewards, new Vector2(22f, 108f), new Vector2(820f, 22f), 12f, NexusTheme.MutedText);
            NexusUiFactory.CreateText(card.transform, "State", active ? "ACTIVE" : "STANDBY", new Vector2(730f, 22f), new Vector2(130f, 24f), 11f, active ? NexusTheme.Cyan : NexusTheme.DimText, TextAlignmentOptions.Right, FontStyles.Bold);
        }

        private void BuildMainMenuBranding()
        {
            Canvas canvas = NexusUiFactory.CreateCanvas("Nexus Main Menu Branding", 40, false);
            canvas.transform.SetParent(transform, false);
            NexusUiFactory.CreateText(canvas.transform, "Nexus", "NEXUS", new Vector2(116f, 104f), new Vector2(700f, 90f), 58f, NexusTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(canvas.transform, "Command", "GALACTIC FRONTIER COMMAND", new Vector2(122f, 194f), new Vector2(760f, 42f), 21f, NexusTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(canvas.transform, "Build", "TACTICAL CARD-BASED IDLE RPG  ·  BUILD 2.4.1", new Vector2(122f, 244f), new Vector2(760f, 28f), 12f, NexusTheme.MutedText);
        }

        private void ApplyThemeToScene()
        {
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.name.StartsWith("Nexus"))
                    continue;

                foreach (TextMeshProUGUI text in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    text.font = NexusUiFactory.GetRuntimeFont();
                    text.color = IsMutedText(text.name) ? NexusTheme.MutedText : NexusTheme.Text;
                    if (text.GetComponentInParent<Button>() == null &&
                        text.GetComponentInParent<TMP_InputField>() == null)
                    {
                        text.raycastTarget = false;
                    }
                }

                foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
                    StyleButton(button);

                foreach (Image image in canvas.GetComponentsInChildren<Image>(true))
                {
                    if (IsStructuralImage(image))
                        image.color = image.name.Contains("Background") || image.name.Contains("BK")
                            ? NexusTheme.Background
                            : NexusTheme.SurfaceRaised;
                }

                foreach (TMP_InputField input in canvas.GetComponentsInChildren<TMP_InputField>(true))
                {
                    if (input.targetGraphic != null)
                        input.targetGraphic.color = NexusTheme.SurfaceRaised;
                }

                foreach (Scrollbar scrollbar in canvas.GetComponentsInChildren<Scrollbar>(true))
                {
                    if (scrollbar.targetGraphic != null) scrollbar.targetGraphic.color = NexusTheme.DimText;
                    if (scrollbar.handleRect != null && scrollbar.handleRect.TryGetComponent(out Image handle))
                        handle.color = NexusTheme.Cyan;
                }
            }
        }

        private static void StyleButton(Button button)
        {
            if (button.targetGraphic != null && IsStructuralImage(button.targetGraphic as Image))
                button.targetGraphic.color = NexusTheme.SurfaceRaised;

            ColorBlock colors = button.colors;
            colors.normalColor = NexusTheme.SurfaceRaised;
            colors.highlightedColor = NexusTheme.SurfaceHover;
            colors.pressedColor = NexusTheme.WithAlpha(NexusTheme.Gold, 0.7f);
            colors.selectedColor = NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f);
            colors.disabledColor = NexusTheme.WithAlpha(NexusTheme.Surface, 0.65f);
            button.colors = colors;
        }

        private static bool IsStructuralImage(Image image)
        {
            if (image == null)
                return false;

            string value = image.name.ToLowerInvariant();
            if (value.Contains("portrait") || value.Contains("planet") || value.Contains("itemimage") ||
                value.Contains("cardimage") || value.Contains("icon") || value.Contains("spell") ||
                value.Contains("pet") || value.Contains("progress") || value.Contains("scanline"))
            {
                return false;
            }

            return image.sprite == null || value.Contains("panel") || value.Contains("background") ||
                   value.Contains("window") || value.Contains("roundcorner") || value.Contains("frame") ||
                   value.Contains("content") || value.Contains("scroll view") || value.Contains("bk");
        }

        private static bool IsMutedText(string objectName)
        {
            string value = objectName.ToLowerInvariant();
            return value.Contains("description") || value.Contains("placeholder") ||
                   value.Contains("hint") || value.Contains("label");
        }

        private static class PendingScreen
        {
            public static NexusScreen Value = NexusScreen.Bridge;
        }
    }
}
