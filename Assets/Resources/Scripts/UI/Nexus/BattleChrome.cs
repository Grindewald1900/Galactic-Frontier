using System.Collections;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Main;
using Assets.Resources.Scripts.Quest;
using Assets.Resources.Scripts.Scene;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World.Domain;
using Assets.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Figma-style chrome for BattleScene under Option A: keep Unity auto-combat,
    /// add the app frame, round HUD, battle log strip, and return navigation.
    /// </summary>
    public sealed class BattleChrome : MonoBehaviour
    {
        private TextMeshProUGUI roundLabel;
        private TextMeshProUGUI modeLabel;
        private TextMeshProUGUI logLabel;
        private TextMeshProUGUI logHeader;
        private TextMeshProUGUI tabLabel;
        private RectTransform logPanel;
        private ScrollRect logScroll;
        private bool logOpen;
        private bool logDirty;
        private bool scrollToEnd;
        private int unread;
        private int lastRound = -1;

        private const float LogWidth = 340f;
        private const float TabWidth = 40f;
        private const float TabHeight = 104f;
        private const float OpenX = -8f;
        private const float ClosedX = LogWidth;
        private const float LogSlideSeconds = 0.18f;
        private readonly System.Collections.Generic.List<BannerHud> bannerHuds =
            new System.Collections.Generic.List<BannerHud>();

        private struct BannerHud
        {
            public RectTransform Portrait;
            public RectTransform Name;
            public RectTransform Level;
        }

        private IEnumerator Start()
        {
            LocalizationUtil.Initialize();
            yield return null;
            Build();
            LoadingOverlay.NotifySceneReady();
            StartCoroutine(PollRound());
        }

        private void Build()
        {
            Canvas chrome = NexusUiFactory.CreateCanvas("Battle Chrome", 120, true);
            chrome.transform.SetParent(transform, false);

            NexusUiFactory.CreatePanel(
                chrome.transform,
                "Top Bar",
                NexusTheme.Surface,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -NexusTheme.TopBarHeight),
                Vector2.zero,
                true);

            NexusUiFactory.CreatePanel(
                chrome.transform,
                "Status Bar",
                NexusTheme.Surface,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                Vector2.zero,
                new Vector2(0f, NexusTheme.StatusBarHeight),
                true);

            GameObject top = chrome.transform.Find("Top Bar").gameObject;
            int region = PlayerPrefs.GetInt("nexus_last_region", 2);
            NexusUiFactory.CreateText(
                top.transform,
                "Brand",
                UiText.BattleBreadcrumb,
                new Vector2(18f, 8f),
                new Vector2(420f, 20f),
                11f,
                NexusTheme.Gold,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            NexusUiFactory.CreateText(
                top.transform,
                "Region",
                UiText.SectorName(Mathf.Clamp(region, 0, 4)),
                new Vector2(18f, 26f),
                new Vector2(420f, 18f),
                12f,
                NexusTheme.MutedText);

            roundLabel = NexusUiFactory.CreateText(
                top.transform,
                "Round",
                UiText.Turn(0),
                new Vector2(860f, 8f),
                new Vector2(200f, 32f),
                22f,
                NexusTheme.Gold,
                TextAlignmentOptions.Center,
                FontStyles.Bold);

            modeLabel = NexusUiFactory.CreateText(
                top.transform,
                "Mode",
                BattleController.PendingSpectateAutoCombat
                    ? UiText.BattleSpectateAuto
                    : UiText.AutoBattle,
                new Vector2(1100f, 14f),
                new Vector2(200f, 22f),
                12f,
                NexusTheme.Cyan,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            NexusUiFactory.CreateIcon(
                top.transform,
                "BattleIcon",
                NexusCardVisual.UiIcon("Battle"),
                new Vector2(1310f, 8f),
                new Vector2(32f, 32f),
                NexusTheme.Gold);

            NexusUiFactory.CreateButton(
                top.transform,
                "Flee",
                UiText.BattleFlee,
                new Vector2(1470f, 8f),
                new Vector2(200f, 32f),
                FleeBattle,
                NexusTheme.WithAlpha(NexusTheme.Red, 0.18f),
                NexusTheme.Red,
                13f);

            NexusUiFactory.CreateButton(
                top.transform,
                "Pause Return",
                UiText.ReturnToBridge,
                new Vector2(1680f, 8f),
                new Vector2(200f, 32f),
                ReturnToBridge,
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.14f),
                NexusTheme.Gold,
                13f);

            GameObject status = chrome.transform.Find("Status Bar").gameObject;
            NexusUiFactory.CreateText(
                status.transform,
                "Hints",
                UiText.BattleStatusHint,
                new Vector2(16f, 2f),
                new Vector2(1400f, 18f),
                10f,
                NexusTheme.DimText);

            BuildLogSidebar(chrome.transform);
            string formationHint = QuestService.FormationBattleHint();
            if (!string.IsNullOrEmpty(formationHint))
                BattleLog.Add($"<color=#E8A832>{formationHint}</color>");

            StyleBattleInfoIfPresent();
            FillSideBanners(chrome.transform);
        }

        /// <summary>
        /// Replaces the scene's leftover "Lv 120" and "+" portrait slots with the live commander and encounter.
        /// Banner art is a temporary placeholder the player can swap later.
        /// </summary>
        private void FillSideBanners(Transform chrome)
        {
            var player = DataUtil.Instance != null ? DataUtil.Instance.GetCurrentPlayer() : null;
            string playerName = player != null && !string.IsNullOrEmpty(player.playerName) ? player.playerName : "Player";
            int playerLevel = player != null ? Mathf.Max(1, player.level) : 1;

            var encounter = EncounterCatalog.Get(BattleController.PendingEncounterId);
            string enemyName = encounter == null
                ? UiText.T("Enemy", "敌方")
                : UiText.T(
                    string.IsNullOrEmpty(encounter.displayName) ? "Enemy" : encounter.displayName,
                    string.IsNullOrEmpty(encounter.displayNameZh) ? encounter.displayName : encounter.displayNameZh);
            int enemyLevel = 1;
            if (encounter?.enemies != null)
            {
                foreach (var slot in encounter.enemies)
                {
                    if (slot != null && slot.level > enemyLevel)
                        enemyLevel = slot.level;
                }
            }

            ApplySideBanner(chrome, GameObject.Find("PlayerInfoPanel"), playerName, playerLevel, "PlayerBannerPlaceholder");
            ApplySideBanner(chrome, GameObject.Find("EnermyInfoPanel"), enemyName, enemyLevel, "EnemyBannerPlaceholder");
        }

        private void ApplySideBanner(Transform chrome, GameObject panel, string title, int level, string portraitName)
        {
            if (panel == null)
                return;

            var background = panel.GetComponent<Image>();
            if (background != null)
                background.color = new Color(0.05f, 0.07f, 0.14f, 0.92f);

            // Pack from the panel's left. The enemy panel's right edge sits under the battle log.
            var portraitRect = panel.transform.Find("PortraitItem") as RectTransform;
            if (portraitRect != null)
            {
                portraitRect.anchorMin = new Vector2(0f, 0.5f);
                portraitRect.anchorMax = new Vector2(0f, 0.5f);
                portraitRect.pivot = new Vector2(0.5f, 0.5f);
                portraitRect.sizeDelta = new Vector2(72f, 72f);
                portraitRect.anchoredPosition = new Vector2(52f, 6f);
            }

            HideLegacyLabel(panel.transform.Find("ID"));
            HideLegacyLabel(panel.transform.Find("Level"));
            // The info panels live on a world-space canvas. TMP there drops CJK glyphs and
            // sits under the overlay top bar. Draw the labels on the overlay chrome instead.
            var name = NexusUiFactory.CreateText(
                chrome,
                "BannerName",
                title ?? "",
                new Vector2(0f, 0f),
                new Vector2(220f, 28f),
                20f,
                Color.white,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            // Ellipsis drops CJK on the first mesh build, before the dynamic fallback atlas has the glyphs.
            name.overflowMode = TextOverflowModes.Overflow;
            name.ForceMeshUpdate();
            var levelLabel = NexusUiFactory.CreateText(
                chrome,
                "BannerLevel",
                "Lv." + level,
                new Vector2(0f, 0f),
                new Vector2(160f, 22f),
                16f,
                NexusTheme.Gold);
            bannerHuds.Add(new BannerHud
            {
                Portrait = portraitRect,
                Name = name.rectTransform,
                Level = levelLabel.rectTransform
            });
            PlaceBannerHud(bannerHuds[bannerHuds.Count - 1]);

            var portrait = UnityEngine.Resources.Load<Sprite>(ImageUtil.UIImagePath + portraitName);
            if (portrait == null)
                return;

            foreach (var image in panel.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null || !image.sprite.name.StartsWith("default_portrait"))
                    continue;
                image.sprite = portrait;
                image.preserveAspect = true;
                image.color = Color.white;
            }
        }

        private static void HideLegacyLabel(Transform target)
        {
            if (target != null)
                target.gameObject.SetActive(false);
        }

        private static void PlaceBannerHud(BannerHud hud)
        {
            if (hud.Portrait == null || hud.Name == null)
                return;

            var camera = Camera.main;
            if (camera == null)
                return;

            var corners = new Vector3[4];
            hud.Portrait.GetWorldCorners(corners);
            Vector3 topRight = camera.WorldToScreenPoint(corners[2]);
            float x = topRight.x + 12f;
            float y = Screen.height - topRight.y;
            hud.Name.anchoredPosition = new Vector2(x, -y);
            if (hud.Level != null)
                hud.Level.anchoredPosition = new Vector2(x, -(y + 26f));
        }

        private static void StyleBattleInfoIfPresent()
        {
            if (BattleInfo.Instance == null)
                return;

            foreach (TextMeshProUGUI text in BattleInfo.Instance.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.font = NexusUiFactory.GetRuntimeFont();
                text.color = NexusTheme.Gold;
            }
        }

        /// <summary>Keeps polling after a fight ends: idle spectate starts the next fight in the same scene.</summary>
        private IEnumerator PollRound()
        {
            bool ended = false;
            while (enabled)
            {
                bool inBattle = GameStatusManager.Instance != null && GameStatusManager.Instance.IsBattle;
                if (ended && inBattle)
                {
                    ended = false;
                    lastRound = -1;
                    if (modeLabel != null)
                        modeLabel.text = BattleController.PendingSpectateAutoCombat
                            ? UiText.BattleSpectateAuto
                            : UiText.AutoBattle;
                }

                int round = BattleController.Instance != null ? BattleController.Instance.CurrentRound : 0;
                if (!ended && round != lastRound && round > 0)
                {
                    lastRound = round;
                    if (roundLabel != null)
                        roundLabel.text = UiText.Turn(round);
                }

                if (!ended && !inBattle && GameStatusManager.Instance != null && lastRound > 0)
                {
                    ended = true;
                    if (modeLabel != null)
                        modeLabel.text = UiText.BattleEnd;
                }

                yield return new WaitForSeconds(0.25f);
            }
        }

        /// <summary>
        /// Right-edge log drawer. The tab stays on screen; the panel slides off the right edge when closed.
        /// Starts open unless that would cover a living enemy card; the tab shows an unread count while closed.
        /// </summary>
        private void BuildLogSidebar(Transform chrome)
        {
            float top = NexusTheme.TopBarHeight + 8f;
            // BattleScene's SpeedControlPanel occupies the bottom-right corner up to ~70px.
            float bottom = Mathf.Max(NexusTheme.StatusBarHeight + 8f, 80f);

            var panelObject = NexusUiFactory.CreatePanel(
                chrome,
                "Battle Log",
                NexusTheme.WithAlpha(NexusTheme.Surface, 0.94f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                Vector2.zero,
                Vector2.zero,
                true);
            logPanel = panelObject.GetComponent<RectTransform>();
            logPanel.pivot = new Vector2(1f, 0.5f);
            logPanel.sizeDelta = new Vector2(LogWidth, -(top + bottom));
            var outline = panelObject.AddComponent<Outline>();
            outline.effectColor = NexusTheme.Border;
            outline.effectDistance = new Vector2(1f, -1f);

            logHeader = NexusUiFactory.CreateText(
                panelObject.transform,
                "Header",
                UiText.BattleLogHeader,
                new Vector2(14f, 10f),
                new Vector2(LogWidth - 28f, 22f),
                13f,
                NexusTheme.Gold,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            var viewport = new GameObject("LogScroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(panelObject.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(12f, 10f);
            viewportRect.offsetMax = new Vector2(-8f, -40f);
            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = true;

            logLabel = NexusUiFactory.CreateText(
                viewport.transform,
                "Body",
                "",
                Vector2.zero,
                new Vector2(LogWidth - 24f, 0f),
                13f,
                NexusTheme.Text);
            logLabel.richText = true;
            logLabel.textWrappingMode = TextWrappingModes.Normal;
            logLabel.overflowMode = TextOverflowModes.Overflow;
            logLabel.lineSpacing = 6f;
            var bodyRect = logLabel.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = Vector2.zero;
            bodyRect.sizeDelta = new Vector2(-6f, 0f);
            var fitter = logLabel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            logScroll = viewport.GetComponent<ScrollRect>();
            logScroll.content = bodyRect;
            logScroll.viewport = viewportRect;
            logScroll.horizontal = false;
            logScroll.vertical = true;
            logScroll.movementType = ScrollRect.MovementType.Clamped;
            logScroll.scrollSensitivity = 28f;

            var tab = NexusUiFactory.CreateButton(
                panelObject.transform,
                "Log Toggle",
                "",
                Vector2.zero,
                new Vector2(TabWidth, TabHeight),
                ToggleLog,
                NexusTheme.WithAlpha(NexusTheme.Surface, 0.94f),
                NexusTheme.Gold,
                14f);
            var tabRect = tab.GetComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(0f, 1f);
            tabRect.anchorMax = new Vector2(0f, 1f);
            tabRect.pivot = new Vector2(1f, 1f);
            // Below the enemy banner so the tab never sits on the encounter name.
            tabRect.anchoredPosition = new Vector2(0f, -124f);
            tabLabel = tab.GetComponentInChildren<TextMeshProUGUI>();
            tabLabel.textWrappingMode = TextWrappingModes.Normal;
            tabLabel.overflowMode = TextOverflowModes.Overflow;
            tabLabel.richText = true;

            logOpen = !LogCoversEnemyCard(chrome.GetComponent<Canvas>());
            logPanel.anchoredPosition = new Vector2(logOpen ? OpenX : ClosedX, (bottom - top) * 0.5f);

            BattleLog.Appended += OnLogAppended;
            BattleLog.Cleared += OnLogCleared;
            logDirty = true;
            scrollToEnd = true;
            RefreshTab();
        }

        private void ToggleLog()
        {
            logOpen = !logOpen;
            if (logOpen)
            {
                unread = 0;
                scrollToEnd = true;
                logDirty = true;
            }

            RefreshTab();
        }

        private void OnLogAppended(string line)
        {
            if (logOpen && IsLogNearBottom())
                scrollToEnd = true;
            if (!logOpen)
                unread++;
            logDirty = true;
            RefreshTab();
        }

        /// <summary>True when a living enemy card reaches under the open drawer (enemy back row at 16:9).</summary>
        private bool LogCoversEnemyCard(Canvas canvas)
        {
            var camera = Camera.main;
            if (camera == null) return false;
            float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            float panelLeft = Screen.width - (LogWidth - OpenX) * scale;
            var corners = new Vector3[4];
            foreach (var card in FindObjectsByType<Card>(FindObjectsSortMode.None))
            {
                if (card == null || card.isPlayerCard || !card.IsAlive()) continue;
                if (!(card.transform is RectTransform rect)) continue;
                rect.GetWorldCorners(corners);
                if (camera.WorldToScreenPoint(corners[2]).x > panelLeft)
                    return true;
            }

            return false;
        }

        /// <summary>Follow new lines unless the player scrolled up to read older ones.</summary>
        private bool IsLogNearBottom()
        {
            if (logScroll == null || logScroll.content == null || logScroll.viewport == null)
                return true;
            float overflow = logScroll.content.rect.height - logScroll.viewport.rect.height;
            return overflow <= 1f || logScroll.verticalNormalizedPosition * overflow <= 24f;
        }

        private void OnLogCleared()
        {
            unread = 0;
            logDirty = true;
            scrollToEnd = true;
            RefreshTab();
        }

        private void RefreshTab()
        {
            if (tabLabel == null) return;
            string arrow = logOpen ? ">" : "<";
            string title = UiText.T("L\nO\nG", "日\n志");
            string badge = !logOpen && unread > 0
                ? $"\n<size=11><color=#E8A832>+{Mathf.Min(unread, 99)}</color></size>"
                : "";
            tabLabel.text = $"{title}\n{arrow}{badge}";
            if (logHeader != null)
                logHeader.text = $"{UiText.BattleLogHeader}  <size=11><color=#64748B>{BattleLog.Entries.Count}/{BattleLog.MaxEntries}</color></size>";
        }

        private void RedrawLog()
        {
            logDirty = false;
            if (logLabel == null) return;
            var entries = BattleLog.Entries;
            logLabel.text = entries.Count == 0 ? UiText.BattleLogStart : string.Join("\n", entries);
            if (!scrollToEnd || logScroll == null) return;
            scrollToEnd = false;
            LayoutRebuilder.ForceRebuildLayoutImmediate(logLabel.rectTransform);
            logScroll.verticalNormalizedPosition = 0f;
        }

        private void AnimateLogPanel()
        {
            if (logPanel == null) return;
            float target = logOpen ? OpenX : ClosedX;
            var position = logPanel.anchoredPosition;
            if (Mathf.Approximately(position.x, target)) return;
            float speed = (ClosedX - OpenX) / LogSlideSeconds;
            position.x = Mathf.MoveTowards(position.x, target, speed * Time.unscaledDeltaTime);
            logPanel.anchoredPosition = position;
        }

        private void OnDestroy()
        {
            BattleLog.Appended -= OnLogAppended;
            BattleLog.Cleared -= OnLogCleared;
        }

        private static void ReturnToBridge()
        {
            BattleSceneExit.ReturnToBridge();
        }

        private static void FleeBattle()
        {
            BattleSceneExit.Flee();
        }

        private void Update()
        {
            for (int i = 0; i < bannerHuds.Count; i++)
                PlaceBannerHud(bannerHuds[i]);

            AnimateLogPanel();
            if (logDirty && (logOpen || scrollToEnd))
                RedrawLog();

            if (Input.GetKeyDown(KeyCode.Escape))
                FleeBattle();
        }
    }
}
