using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Onboarding.Domain;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Bridge command dashboard: active combat deck, running ops / parallel caps, sector strip.
    /// </summary>
    internal sealed class BridgeScreen
    {
        private readonly Transform root;
        private readonly System.Action openMissions;
        private readonly System.Action openFormation;
        private readonly System.Action openExplore;
        private readonly System.Action openShip;
        private readonly System.Action<AppScreen> navigate;
        private string pendingStopDeckId = "";

        private BridgeScreen(
            Transform root,
            System.Action openMissions,
            System.Action openFormation,
            System.Action openExplore,
            System.Action openShip,
            System.Action<AppScreen> navigate)
        {
            this.root = root;
            this.openMissions = openMissions;
            this.openFormation = openFormation;
            this.openExplore = openExplore;
            this.openShip = openShip;
            this.navigate = navigate;
        }

        public GameObject Root => root.gameObject;

        public static BridgeScreen Build(
            Transform parent,
            System.Action openMissions,
            System.Action openFormation,
            System.Action openExplore,
            System.Action openShip = null,
            System.Action<AppScreen> navigate = null)
        {
            GameObject root = NexusUiFactory.CreatePanel(
                parent,
                "Bridge Screen",
                NexusTheme.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            var screen = new BridgeScreen(
                root.transform, openMissions, openFormation, openExplore, openShip, navigate);
            screen.Rebuild();
            return screen;
        }

        public void Rebuild()
        {
            ClearDynamicChildren(root);

            List<CardEntity> all = CardListManager.Instance?.GetCardEntities() ?? new List<CardEntity>();
            if (DataUtil.Instance != null)
            {
                DeckService.EnsureLoaded(DataUtil.Instance, all);
                WorldService.EnsureLoaded(DataUtil.Instance);
                ShipService.EnsureLoaded(DataUtil.Instance);
                IdleSettlementService.EnsureLoaded(DataUtil.Instance);
                OnboardingService.EnsureLoaded(DataUtil.Instance);
            }

            NexusUiFactory.CreateText(
                root,
                "Title",
                UiText.BridgeTitle,
                new Vector2(28f, 16f),
                new Vector2(520f, 36f),
                22f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            string commander = DataUtil.Instance?.currentPlayer?.playerName ?? "COMMANDER";
            NexusUiFactory.CreateText(
                root,
                "Subtitle",
                UiText.BridgeSubtitle(commander),
                new Vector2(28f, 52f),
                new Vector2(720f, 22f),
                12f,
                NexusTheme.MutedText);

            BuildOnboardingCta();

            NexusUiFactory.CreateText(
                root,
                "AutoBadge",
                UiText.BridgeAutoCombatBadge,
                new Vector2(1460f, 20f),
                new Vector2(280f, 28f),
                12f,
                NexusTheme.Cyan,
                TextAlignmentOptions.Right,
                FontStyles.Bold);

            GameObject banner = NexusUiFactory.CreateBox(
                root,
                "Event Banner",
                new Vector2(28f, 76f),
                new Vector2(1740f, 40f),
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.08f),
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.25f));
            NexusUiFactory.CreateIcon(
                banner.transform,
                "Icon",
                NexusCardVisual.UiIcon("Battle"),
                new Vector2(12f, 8f),
                new Vector2(28f, 28f),
                NexusTheme.Gold);
            var bannerText = NexusUiFactory.CreateText(
                banner.transform,
                "Text",
                UiText.BridgeBanner,
                new Vector2(48f, 12f),
                new Vector2(1600f, 22f),
                13f,
                NexusTheme.Gold);
            bannerText.textWrappingMode = TextWrappingModes.NoWrap;

            int power = DataUtil.Instance?.currentPlayer?.combatPower ?? 0;
            int credits = DataUtil.Instance?.currentPlayer?.creditPoints ?? 0;
            int progress = DataUtil.Instance?.currentPlayer?.explorationProgress ?? 0;
            int cards = all.Count;
            var combatMembers = DeckService.GetActiveCombatMembers(all);
            int inLine = combatMembers.Count;
            if (power <= 0 && inLine > 0)
            {
                foreach (var e in combatMembers)
                    if (e != null) power += Mathf.RoundToInt(e.power);
            }

            int busy = DeckService.CountBusyDecks();
            int maxParallel = DeckService.MaxParallelActions;

            AddStat(root, new Vector2(28f, 128f), UiText.StatCombatPower, power.ToString("N0"), NexusTheme.Gold, new Vector2(248f, 100f));
            AddStat(root, new Vector2(292f, 128f), UiText.StatExploration, $"{progress}%", NexusTheme.Cyan, new Vector2(248f, 100f));
            AddStat(root, new Vector2(556f, 128f), UiText.StatCredits, credits.ToString("N0") + "₵", NexusTheme.Purple, new Vector2(248f, 100f));
            AddStat(root, new Vector2(820f, 128f), UiText.StatRoster, UiText.ParallelOps(busy, maxParallel), NexusTheme.Green, new Vector2(248f, 100f));

            BuildActiveFleet(combatMembers);
            BuildRunningOps();
            BuildPendingLoot();
            BuildSectors();
            BuildLog(inLine, cards, progress, credits, power);
            BuildCtas();

            string opsHint = WorldService.IsSectorComplete()
                ? UiText.SectorComplete
                : UiText.BridgeOpsHint;
            NexusUiFactory.CreateText(
                root,
                "OpsHint",
                opsHint,
                new Vector2(28f, 868f),
                new Vector2(1740f, 28f),
                11f,
                NexusTheme.DimText);
        }

        private void BuildCtas()
        {
            void Cta(string name, string label, float x, UnityEngine.Events.UnityAction action, Color fill, Color tint)
            {
                NexusUiFactory.CreateButton(
                    root, name, label,
                    new Vector2(x, 548f),
                    new Vector2(126f, 40f),
                    action, fill, tint, 12f);
            }

            Cta("CTA Recruit", UiText.BridgeOpenRecruit, 1090f,
                () => navigate?.Invoke(AppScreen.Recruit),
                NexusTheme.WithAlpha(NexusTheme.Purple, 0.16f), NexusTheme.Purple);
            Cta("CTA Formation", UiText.BridgeOpenFormation, 1226f,
                () => openFormation?.Invoke(),
                NexusTheme.SurfaceRaised, NexusTheme.Text);
            Cta("CTA Explore", UiText.BridgeStartAutoBattle, 1362f,
                () => openExplore?.Invoke(),
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f), NexusTheme.Gold);
            Cta("CTA Ship", UiText.OpenShipBay, 1498f,
                () => openShip?.Invoke(),
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.16f), NexusTheme.Cyan);
            Cta("CTA Missions", UiText.TodaysMissions, 1634f,
                () => openMissions?.Invoke(),
                NexusTheme.SurfaceRaised, NexusTheme.Cyan);
        }

        private void BuildActiveFleet(List<CardEntity> lineup)
        {
            GameObject fleet = NexusUiFactory.CreateBox(
                root,
                "Fleet",
                new Vector2(28f, 244f),
                new Vector2(1040f, 200f),
                NexusTheme.Surface,
                NexusTheme.BorderSoft);
            var combat = DeckService.GetActiveCombatDeck();
            string heading = combat != null
                ? $"{UiText.ActiveFleet}: {combat.displayName}"
                : UiText.ActiveFleet;
            NexusUiFactory.CreateText(
                fleet.transform,
                "Heading",
                heading,
                new Vector2(20f, 12f),
                new Vector2(700f, 28f),
                16f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            float x = 20f;
            if (lineup != null && lineup.Count > 0)
            {
                foreach (CardEntity entity in lineup)
                {
                    if (entity == null) continue;
                    string footer = $"Lv.{entity.Level} · {UiText.SlotLabel((int)entity.GetLineupPosition())}";
                    NexusCardVisual.CreatePortraitCard(
                        fleet.transform,
                        $"Unit {entity.id}",
                        entity,
                        new Vector2(x, 36f),
                        new Vector2(150f, 148f),
                        footer);
                    x += 170f;
                }
            }
            else
            {
                NexusUiFactory.CreateText(
                    fleet.transform,
                    "Empty",
                    UiText.EmptyFleet,
                    new Vector2(24f, 100f),
                    new Vector2(700f, 40f),
                    14f,
                    NexusTheme.MutedText);
            }
        }

        private void BuildRunningOps()
        {
            GameObject ops = NexusUiFactory.CreateBox(
                root,
                "RunningOps",
                new Vector2(28f, 456f),
                new Vector2(1040f, 84f),
                NexusTheme.Surface,
                NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(
                ops.transform,
                "Heading",
                $"{UiText.RunningOps} · {UiText.ParallelOps(DeckService.CountBusyDecks(), DeckService.MaxParallelActions)}",
                new Vector2(20f, 6f),
                new Vector2(700f, 20f),
                13f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            var busyDecks = DeckService.GetBusyDecks();
            if (busyDecks.Count == 0)
            {
                NexusUiFactory.CreateText(
                    ops.transform,
                    "Empty",
                    UiText.NoRunningOps,
                    new Vector2(20f, 32f),
                    new Vector2(900f, 40f),
                    12f,
                    NexusTheme.MutedText);
                return;
            }

            float x = 20f;
            foreach (var deck in busyDecks)
            {
                string label =
                    $"{deck.displayName}: {UiText.DeckActionLabel(deck.action.status.ToString(), deck.action.actionType.ToString())}";
                GameObject row = NexusUiFactory.CreateBox(
                    ops.transform,
                    $"Op {deck.deckId}",
                    new Vector2(x, 32f),
                    new Vector2(320f, 44f),
                    NexusTheme.SurfaceRaised,
                    NexusTheme.BorderSoft);
                NexusUiFactory.CreateText(
                    row.transform,
                    "Label",
                    Truncate(label, 34),
                    new Vector2(8f, 6f),
                    new Vector2(220f, 36f),
                    11f,
                    NexusTheme.Cyan,
                    TextAlignmentOptions.Left,
                    FontStyles.Bold);

                string capturedId = deck.deckId;
                NexusUiFactory.CreateButton(
                    row.transform,
                    "Stop",
                    UiText.StopAction,
                    new Vector2(230f, 8f),
                    new Vector2(80f, 32f),
                    () => TryStopDeck(capturedId),
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f),
                    NexusTheme.Gold,
                    11f);
                x += 340f;
            }
        }

        private void TryStopDeck(string deckId)
        {
            if (pendingStopDeckId != deckId)
            {
                pendingStopDeckId = deckId;
                Debug.Log("[DECK] " + UiText.StopActionConfirm);
                Rebuild();
                return;
            }

            var result = DeckService.TryStop(deckId);
            pendingStopDeckId = "";
            if (!result.Success)
                Debug.LogWarning("[DECK] Stop failed: " + result.Message);
            Rebuild();
        }

        private void BuildPendingLoot()
        {
            var count = IdleSettlementService.PendingCount;
            GameObject box = NexusUiFactory.CreateBox(
                root,
                "PendingLoot",
                new Vector2(1090f, 128f),
                new Vector2(678f, 100f),
                NexusTheme.Surface,
                NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(
                box.transform,
                "Heading",
                UiText.PendingLootTitle(count),
                new Vector2(16f, 8f),
                new Vector2(420f, 24f),
                14f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            var lootBody = NexusUiFactory.CreateText(
                box.transform,
                "Body",
                count > 0 ? UiText.PendingLootHint : UiText.PendingLootEmpty,
                new Vector2(16f, 36f),
                new Vector2(400f, 52f),
                12f,
                NexusTheme.MutedText);
            lootBody.textWrappingMode = TextWrappingModes.Normal;
            if (count > 0)
            {
                NexusUiFactory.CreateButton(
                    box.transform,
                    "Claim",
                    UiText.ClaimPendingLoot,
                    new Vector2(470f, 28f),
                    new Vector2(186f, 44f),
                    () =>
                    {
                        var r = IdleSettlementService.ClaimAllPending();
                        Debug.Log("[IDLE] claim: " + (r.Success ? "ok" : r.Message));
                        Rebuild();
                    },
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f),
                    NexusTheme.Gold,
                    13f);
            }
        }

        private void BuildSectors()
        {
            GameObject sectors = NexusUiFactory.CreateBox(
                root,
                "Sectors",
                new Vector2(28f, 608f),
                new Vector2(1740f, 248f),
                NexusTheme.Surface,
                NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(
                sectors.transform,
                "Heading",
                UiText.BridgeSectors,
                new Vector2(20f, 10f),
                new Vector2(640f, 24f),
                15f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            var viewport = new GameObject("SectorViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(sectors.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.offsetMin = new Vector2(12f, 12f);
            viewportRect.offsetMax = new Vector2(-12f, -40f);
            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            viewportImage.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0.5f);
            contentRect.anchorMax = new Vector2(0f, 0.5f);
            contentRect.pivot = new Vector2(0f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;

            var regions = RegionCatalog.All;
            int unique = regions != null && regions.Count > 0 ? regions.Count : 5;
            const float itemW = 188f;
            const float itemH = 168f;
            const float gap = 16f;
            float stride = itemW + gap;

            var carousel = content.AddComponent<BridgeSectorCarousel>();
            carousel.content = contentRect;
            carousel.stride = stride;
            carousel.uniqueCount = unique;
            carousel.pixelsPerSecond = 32f;
            carousel.hoverScale = 1.18f;

            int copies = unique * 2;
            for (int i = 0; i < copies; i++)
            {
                int index = i % unique;
                var region = regions != null && index < regions.Count ? regions[index] : null;
                string name = region != null
                    ? UiText.T(region.displayNameEn, region.displayNameZh)
                    : UiText.SectorName(index);
                string faction = region != null && !string.IsNullOrEmpty(region.factionTag)
                    ? UiText.T(FactionTags.ShortEn(region.factionTag), FactionTags.ShortZh(region.factionTag))
                    : "";

                GameObject cell = NexusUiFactory.CreateBox(
                    content.transform,
                    $"Sector {i}",
                    Vector2.zero,
                    new Vector2(itemW, itemH),
                    NexusTheme.SurfaceRaised,
                    NexusTheme.BorderSoft);
                var cellRect = cell.GetComponent<RectTransform>();
                cellRect.anchorMin = new Vector2(0f, 0.5f);
                cellRect.anchorMax = new Vector2(0f, 0.5f);
                cellRect.pivot = new Vector2(0.5f, 0.5f);
                cellRect.anchoredPosition = new Vector2(itemW * 0.5f + i * stride, 0f);
                cellRect.sizeDelta = new Vector2(itemW, itemH);
                var cellImage = cell.GetComponent<Image>();
                cellImage.raycastTarget = true;

                NexusUiFactory.CreateIcon(
                    cell.transform,
                    "Planet",
                    NexusCardVisual.PlanetSprite(index * 3),
                    new Vector2(44f, 10f),
                    new Vector2(100f, 86f),
                    Color.white);
                NexusUiFactory.CreateText(
                    cell.transform,
                    "Name",
                    name,
                    new Vector2(8f, 102f),
                    new Vector2(itemW - 16f, 28f),
                    13f,
                    NexusTheme.Text,
                    TextAlignmentOptions.Center,
                    FontStyles.Bold);
                if (!string.IsNullOrEmpty(faction))
                {
                    NexusUiFactory.CreateText(
                        cell.transform,
                        "Faction",
                        faction,
                        new Vector2(8f, 132f),
                        new Vector2(itemW - 16f, 22f),
                        11f,
                        NexusTheme.Cyan,
                        TextAlignmentOptions.Center);
                }

                var item = cell.AddComponent<BridgeSectorCarouselItem>();
                item.owner = carousel;
                item.onClick = () => openExplore?.Invoke();
            }

            contentRect.sizeDelta = new Vector2(copies * stride + 16f, itemH);
        }

        private void BuildLog(int inLine, int cards, int progress, int credits, int power)
        {
            GameObject log = NexusUiFactory.CreateBox(
                root,
                "Log",
                new Vector2(1090f, 244f),
                new Vector2(678f, 292f),
                NexusTheme.Surface,
                NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(
                log.transform,
                "Heading",
                UiText.EventLog,
                new Vector2(20f, 12f),
                new Vector2(400f, 28f),
                16f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            string[] logLines =
            {
                UiText.BridgeLogLine(0, inLine, cards),
                UiText.BridgeLogLine(1, progress, credits),
                UiText.BridgeLogLine(2, power, 0),
                UiText.BridgeLogLine(3, 0, 0),
                UiText.ParallelOps(DeckService.CountBusyDecks(), DeckService.MaxParallelActions)
            };
            for (int i = 0; i < logLines.Length; i++)
            {
                float ly = 48f + i * 48f;
                GameObject row = NexusUiFactory.CreateBox(
                    log.transform,
                    $"LogRow {i}",
                    new Vector2(16f, ly),
                    new Vector2(646f, 44f),
                    NexusTheme.SurfaceRaised,
                    NexusTheme.BorderSoft);
                NexusUiFactory.CreateIcon(
                    row.transform,
                    "Icon",
                    NexusCardVisual.EventSprite(i),
                    new Vector2(10f, 8f),
                    new Vector2(28f, 28f),
                    Color.white);
                var line = NexusUiFactory.CreateText(
                    row.transform,
                    "Text",
                    logLines[i],
                    new Vector2(48f, 6f),
                    new Vector2(580f, 32f),
                    12f,
                    NexusTheme.Text);
                line.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        private void BuildOnboardingCta()
        {
            if (OnboardingService.IsChainComplete)
                return;

            var step = OnboardingService.ActiveStep;
            if (step == null)
                return;

            var title = UiText.T(step.titleEn, step.titleZh);
            NexusUiFactory.CreateText(
                root,
                "OnboardNext",
                UiText.MissionsNextStep(title),
                new Vector2(780f, 20f),
                new Vector2(360f, 24f),
                12f,
                NexusTheme.Gold,
                TextAlignmentOptions.Right,
                FontStyles.Bold);

            NexusUiFactory.CreateButton(
                root,
                "OnboardMissions",
                UiText.MissionsOpenMissions,
                new Vector2(1160f, 14f),
                new Vector2(140f, 32f),
                () => openMissions?.Invoke(),
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f),
                NexusTheme.Gold,
                11f);

            NexusUiFactory.CreateButton(
                root,
                "OnboardGo",
                UiText.MissionsGo,
                new Vector2(1312f, 14f),
                new Vector2(88f, 32f),
                () =>
                {
                    var target = MapOnboardingTarget(step.targetScreen);
                    if (navigate != null)
                        navigate(target);
                    else if (target == AppScreen.Formation)
                        openFormation?.Invoke();
                    else if (target == AppScreen.Battle)
                        openExplore?.Invoke();
                    else
                        openMissions?.Invoke();
                },
                NexusTheme.SurfaceRaised,
                NexusTheme.Text,
                11f);
        }

        private static AppScreen MapOnboardingTarget(string targetScreen) =>
            targetScreen switch
            {
                "Formation" => AppScreen.Formation,
                "Battle" => AppScreen.Battle,
                "Crafting" => AppScreen.Crafting,
                "Market" => AppScreen.Market,
                "Ship" => AppScreen.Ship,
                _ => AppScreen.Missions
            };

        private static void AddStat(Transform parent, Vector2 position, string label, string value, Color accent, Vector2? size = null)
        {
            Vector2 cardSize = size ?? new Vector2(320f, 118f);
            GameObject card = NexusUiFactory.CreateBox(parent, $"Stat {label}", position, cardSize, NexusTheme.SurfaceRaised, NexusTheme.BorderSoft);
            NexusUiFactory.CreatePanel(card.transform, "Accent", accent, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(3f, 0f));
            NexusUiFactory.CreateText(card.transform, "Value", value, new Vector2(16f, 16f), new Vector2(cardSize.x - 32f, 36f), 22f, NexusTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(card.transform, "Label", label, new Vector2(16f, 58f), new Vector2(cardSize.x - 32f, 22f), 12f, NexusTheme.MutedText);
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
                return value ?? "";
            return value.Substring(0, max - 1) + "…";
        }

        private static void ClearDynamicChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
}
