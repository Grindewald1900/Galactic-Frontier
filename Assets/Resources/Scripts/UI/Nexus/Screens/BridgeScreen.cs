using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Onboarding.Domain;
using Assets.Resources.Scripts.Unlock;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Bridge command dashboard: combat deck, fleet carousel, sector strip.
    /// </summary>
    internal sealed class BridgeScreen
    {
        private readonly Transform root;
        private readonly System.Action openMissions;
        private readonly System.Action openFormation;
        private readonly System.Action openExplore;
        private readonly System.Action openShip;
        private readonly System.Action<AppScreen> navigate;
        private BridgeLogCategory? logFilter;

        /// <summary>Desaturated wash applied to locked sector art.</summary>
        private static readonly Color LockedTint = new Color(0.36f, 0.40f, 0.48f, 0.55f);

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
                FeatureUnlockService.EnsureLoaded(DataUtil.Instance);
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

            AddStat(root, new Vector2(28f, 128f), UiText.StatCombatPower, power.ToString("N0"), NexusTheme.Gold, new Vector2(320f, 100f));
            AddStat(root, new Vector2(364f, 128f), UiText.StatExploration, $"{progress}%", NexusTheme.Cyan, new Vector2(320f, 100f));
            AddStat(root, new Vector2(700f, 128f), UiText.StatCredits, credits.ToString("N0") + "₵", NexusTheme.Purple, new Vector2(320f, 100f));

            BuildActiveFleet(combatMembers);
            BuildFleetCarousel();
            BuildCommanderGoal();
            BuildPendingLoot();
            BuildSectors();
            BuildLog(inLine, cards, progress, credits, power);

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

        private void BuildCommanderGoal()
        {
            var goal = CommanderGoalService.GetCurrent();
            GameObject panel = NexusUiFactory.CreateBox(
                root, "Commander Goal",
                new Vector2(1090f, 128f), new Vector2(678f, 280f),
                NexusTheme.Surface, NexusTheme.BorderSoft);

            NexusUiFactory.CreateText(
                panel.transform, "GoalHead", UiText.BridgeCommanderGoal,
                new Vector2(16f, 12f), new Vector2(640f, 22f), 12f, NexusTheme.MutedText);
            NexusUiFactory.CreateText(
                panel.transform, "GoalTitle", goal.Title,
                new Vector2(16f, 34f), new Vector2(640f, 28f), 16f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(
                panel.transform, "Progress", goal.Progress,
                new Vector2(16f, 68f), new Vector2(640f, 22f), 12f, NexusTheme.Cyan);
            NexusUiFactory.CreateText(
                panel.transform, "BottleneckHead", UiText.BridgeBottleneck,
                new Vector2(16f, 98f), new Vector2(640f, 20f), 11f, NexusTheme.MutedText);
            NexusUiFactory.CreateText(
                panel.transform, "Bottleneck", goal.Bottleneck,
                new Vector2(16f, 118f), new Vector2(640f, 48f), 12f, NexusTheme.Text);

            NexusUiFactory.CreateText(
                panel.transform, "RecHead", UiText.BridgeRecommended,
                new Vector2(16f, 168f), new Vector2(200f, 20f), 11f, NexusTheme.MutedText);

            NexusUiFactory.CreateRoleButton(
                panel.transform, "Recommended", goal.RecommendedAction,
                new Vector2(16f, 192f), new Vector2(320f, 44f),
                () =>
                {
                    if (goal.OnRecommended != null)
                        goal.OnRecommended();
                    else if (navigate != null)
                        navigate(goal.RecommendedScreen);
                    else
                        openExplore?.Invoke();
                },
                NexusButtonRole.Primary, 14f);
        }

        private void BuildCtas()
        {
            // P6: peer CTAs removed; use commander goal + shell navigation.
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
            NexusUiFactory.CreateText(
                fleet.transform,
                "EditHint",
                UiText.EditFormationHint,
                new Vector2(760f, 14f),
                new Vector2(260f, 24f),
                12f,
                NexusTheme.Cyan,
                TextAlignmentOptions.Right);

            var fleetImage = fleet.GetComponent<Image>();
            fleetImage.raycastTarget = true;
            var fleetButton = fleet.AddComponent<Button>();
            var fleetColors = fleetButton.colors;
            fleetColors.normalColor = NexusTheme.Surface;
            fleetColors.highlightedColor = NexusTheme.SurfaceHover;
            fleetColors.pressedColor = NexusTheme.SurfaceRaised;
            fleetColors.selectedColor = NexusTheme.SurfaceHover;
            fleetColors.colorMultiplier = 1f;
            fleetButton.colors = fleetColors;
            fleetButton.onClick.AddListener(() => openFormation?.Invoke());

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

        private void BuildFleetCarousel()
        {
            GameObject ops = NexusUiFactory.CreateBox(
                root,
                "FleetCarousel",
                new Vector2(28f, 456f),
                new Vector2(1040f, 140f),
                NexusTheme.Surface,
                NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(
                ops.transform,
                "Heading",
                $"{UiText.FleetHeading} · {UiText.ParallelOps(DeckService.CountBusyDecks(), DeckService.MaxParallelActions)}",
                new Vector2(20f, 6f),
                new Vector2(900f, 22f),
                13f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            var viewport = new GameObject("FleetViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(ops.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.offsetMin = new Vector2(12f, 8f);
            viewportRect.offsetMax = new Vector2(-12f, -32f);
            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            viewportImage.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;

            const float cardW = 280f;
            const float cardH = 92f;
            const float gap = 12f;
            DrawFlagshipCard(content.transform, 0f, cardW, cardH);
            DrawBerthCard(content.transform, cardW + gap, cardW, cardH);

            contentRect.sizeDelta = new Vector2(cardW * 2f + gap + 8f, 0f);

            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;
        }

        private void DrawFlagshipCard(Transform parent, float x, float width, float height)
        {
            ShipService.EnsureReady();
            var ship = ShipService.State;
            var combat = DeckService.GetActiveCombatDeck();
            string shipName = ship != null ? $"{ship.displayName}  Lv.{ship.level}" : UiText.FleetHeading;
            string deckName = combat != null ? combat.displayName : UiText.EmptyFleet;
            string status = combat != null && combat.IsActionBusy
                ? UiText.DeckActionLabel(combat.action.status.ToString(), combat.action.actionType.ToString())
                : UiText.FleetStatusIdle;

            GameObject card = NexusUiFactory.CreateBox(
                parent,
                "Flagship",
                Vector2.zero,
                new Vector2(width, height),
                NexusTheme.SurfaceRaised,
                NexusTheme.Gold);
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            var image = card.GetComponent<Image>();
            image.raycastTarget = true;
            var button = card.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() =>
            {
                if (FeatureUnlockUi.CanOpen(AppScreen.Ship))
                    openShip?.Invoke();
                else
                    FeatureUnlockUi.ShowLocked(AppScreen.Ship);
            });

            NexusUiFactory.CreateIcon(
                card.transform,
                "Icon",
                NexusCardVisual.UiIcon("World"),
                new Vector2(10f, 18f),
                new Vector2(36f, 36f),
                NexusTheme.Cyan);
            NexusUiFactory.CreateText(
                card.transform,
                "Name",
                shipName,
                new Vector2(54f, 8f),
                new Vector2(width - 64f, 22f),
                13f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            NexusUiFactory.CreateText(
                card.transform,
                "Deck",
                UiText.FleetDeckLabel(deckName),
                new Vector2(54f, 32f),
                new Vector2(width - 64f, 18f),
                11f,
                NexusTheme.MutedText);
            NexusUiFactory.CreateText(
                card.transform,
                "Status",
                status,
                new Vector2(54f, 52f),
                new Vector2(width - 64f, 18f),
                11f,
                combat != null && combat.IsActionBusy ? NexusTheme.Cyan : NexusTheme.Green);

            float fill = combat != null && combat.IsActionBusy ? 0.65f : 0f;
            GameObject barBg = NexusUiFactory.CreateBox(
                card.transform,
                "BarBg",
                new Vector2(12f, height - 14f),
                new Vector2(width - 24f, 6f),
                NexusTheme.WithAlpha(NexusTheme.Border, 0.5f));
            barBg.GetComponent<Image>().raycastTarget = false;
            if (fill > 0f)
            {
                GameObject bar = NexusUiFactory.CreateBox(
                    card.transform,
                    "Bar",
                    new Vector2(12f, height - 14f),
                    new Vector2((width - 24f) * fill, 6f),
                    NexusTheme.Cyan);
                bar.GetComponent<Image>().raycastTarget = false;
            }
        }

        private void DrawBerthCard(Transform parent, float x, float width, float height)
        {
            GameObject card = NexusUiFactory.CreateBox(
                parent,
                "Berth",
                Vector2.zero,
                new Vector2(width, height),
                NexusTheme.WithAlpha(NexusTheme.SurfaceRaised, 0.45f),
                NexusTheme.BorderSoft);
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            var image = card.GetComponent<Image>();
            image.raycastTarget = true;
            var button = card.AddComponent<Button>();
            button.onClick.AddListener(() => NexusSnackbar.Show(UiText.FleetBerthHint));

            NexusUiFactory.CreateText(
                card.transform,
                "Title",
                UiText.FleetBerthLocked,
                new Vector2(16f, 20f),
                new Vector2(width - 32f, 24f),
                14f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            var hint = NexusUiFactory.CreateText(
                card.transform,
                "Hint",
                UiText.FleetBerthHint,
                new Vector2(16f, 48f),
                new Vector2(width - 32f, 36f),
                11f,
                NexusTheme.DimText);
            hint.textWrappingMode = TextWrappingModes.Normal;
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
                        var r = IdleSettlementService.ClaimAllPending(out var claimed);
                        if (!r.Success)
                            NexusSnackbar.Show(r.Message);
                        RewardPopup.Show(UiText.RewardTitle, RewardPopup.FromPending(claimed));
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
            var unlocked = CollectUnlockedRegionIds();
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
                bool locked = region != null && !unlocked.Contains(region.regionId);

                GameObject cell = NexusUiFactory.CreateBox(
                    content.transform,
                    $"Sector {i}",
                    Vector2.zero,
                    new Vector2(itemW, itemH),
                    locked ? NexusTheme.WithAlpha(NexusTheme.SurfaceRaised, 0.45f) : NexusTheme.SurfaceRaised,
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
                    locked ? LockedTint : Color.white);
                NexusUiFactory.CreateText(
                    cell.transform,
                    "Name",
                    name,
                    new Vector2(8f, 102f),
                    new Vector2(itemW - 16f, 28f),
                    13f,
                    locked ? NexusTheme.DimText : NexusTheme.Text,
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
                        locked ? NexusTheme.DimText : NexusTheme.Cyan,
                        TextAlignmentOptions.Center);
                }

                if (locked)
                {
                    Sprite lockSprite = NexusCardVisual.UiIcon("Lock");
                    if (lockSprite != null)
                    {
                        NexusUiFactory.CreateIcon(
                            cell.transform,
                            "LockIcon",
                            lockSprite,
                            new Vector2(itemW * 0.5f - 16f, 30f),
                            new Vector2(32f, 32f),
                            NexusTheme.Text);
                    }

                    NexusUiFactory.CreateText(
                        cell.transform,
                        "LockLabel",
                        UiText.RegionLocked,
                        new Vector2(8f, 74f),
                        new Vector2(itemW - 16f, 20f),
                        11f,
                        NexusTheme.MutedText,
                        TextAlignmentOptions.Center,
                        FontStyles.Bold);
                }

                var item = cell.AddComponent<BridgeSectorCarouselItem>();
                item.owner = carousel;
                item.onClick = () => openExplore?.Invoke();
            }

            contentRect.sizeDelta = new Vector2(copies * stride + 16f, itemH);
        }

        /// <summary>
        /// Region ids the player may currently enter. On any world-state failure every region is
        /// treated as unlocked so the strip degrades to its previous look instead of showing all locks.
        /// </summary>
        private static HashSet<string> CollectUnlockedRegionIds()
        {
            var ids = new HashSet<string>();
            try
            {
                foreach (RegionView view in WorldService.GetAllRegionViews())
                {
                    if (view?.Config == null || !view.CanEnter) continue;
                    ids.Add(view.Config.regionId);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BRIDGE] Sector lock state unavailable: {ex.Message}");
                foreach (var config in RegionCatalog.All)
                {
                    if (config != null)
                        ids.Add(config.regionId);
                }
            }

            return ids;
        }

        private void BuildLog(int inLine, int cards, int progress, int credits, int power)
        {
            BridgeEventLog.Ensure();
            BridgeEventLog.UpsertLive("live_fleet", BridgeLogCategory.Combat,
                $"Fleet status: {inLine} units deployed · roster {cards}",
                $"舰队状态：{inLine} 人上阵 · 卡池 {cards}");
            BridgeEventLog.UpsertLive("live_explore", BridgeLogCategory.Explore,
                $"Grid restore {progress}% · credits {credits:N0}₵",
                $"航网恢复 {progress}% · 信用点 {credits:N0}₵");
            BridgeEventLog.UpsertLive("live_power", BridgeLogCategory.Combat,
                $"Combat power estimate {power:N0}",
                $"预估战力 {power:N0}");
            BridgeEventLog.UpsertLive("live_ops", BridgeLogCategory.Production,
                $"Parallel ops {DeckService.CountBusyDecks()}/{DeckService.MaxParallelActions}",
                $"并行行动 {DeckService.CountBusyDecks()}/{DeckService.MaxParallelActions}");

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
                new Vector2(20f, 10f),
                new Vector2(400f, 24f),
                16f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            DrawLogFilters(log.transform);

            var rows = BridgeEventLog.Query(logFilter);
            if (rows.Count == 0)
            {
                NexusUiFactory.CreateText(
                    log.transform,
                    "Empty",
                    UiText.EventLogEmpty,
                    new Vector2(20f, 86f),
                    new Vector2(638f, 40f),
                    13f,
                    NexusTheme.DimText);
                return;
            }

            int shown = Mathf.Min(rows.Count, 4);
            for (int i = 0; i < shown; i++)
            {
                var entry = rows[i];
                float ly = 82f + i * 50f;
                GameObject row = NexusUiFactory.CreateBox(
                    log.transform,
                    $"LogRow {i}",
                    new Vector2(16f, ly),
                    new Vector2(646f, 46f),
                    NexusTheme.SurfaceRaised,
                    NexusTheme.BorderSoft);
                NexusUiFactory.CreateIcon(
                    row.transform,
                    "Icon",
                    NexusCardVisual.EventSprite((int)entry.Category % 5),
                    new Vector2(10f, 8f),
                    new Vector2(28f, 28f),
                    Color.white);
                var tag = NexusUiFactory.CreateText(
                    row.transform,
                    "Tag",
                    UiText.EventLogFilter(entry.Category),
                    new Vector2(48f, 4f),
                    new Vector2(160f, 16f),
                    10f,
                    CategoryTint(entry.Category),
                    TextAlignmentOptions.Left,
                    FontStyles.Bold);
                tag.textWrappingMode = TextWrappingModes.NoWrap;
                var line = NexusUiFactory.CreateText(
                    row.transform,
                    "Text",
                    UiText.T(entry.En, entry.Zh),
                    new Vector2(48f, 20f),
                    new Vector2(580f, 22f),
                    12f,
                    NexusTheme.Text);
                line.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        private void DrawLogFilters(Transform log)
        {
            BridgeLogCategory?[] filters =
            {
                null,
                BridgeLogCategory.Explore,
                BridgeLogCategory.Combat,
                BridgeLogCategory.Production,
                BridgeLogCategory.Trade
            };
            float x = 16f;
            for (int i = 0; i < filters.Length; i++)
            {
                var cat = filters[i];
                bool on = cat == logFilter || (!cat.HasValue && !logFilter.HasValue);
                string label = cat.HasValue ? UiText.EventLogFilter(cat.Value) : UiText.EventLogFilterAll;
                var captured = cat;
                NexusUiFactory.CreateButton(
                    log,
                    "LogFilter " + (cat?.ToString() ?? "All"),
                    label,
                    new Vector2(x, 38f),
                    new Vector2(86f, 36f),
                    () =>
                    {
                        logFilter = captured;
                        Rebuild();
                    },
                    on ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f) : NexusTheme.SurfaceRaised,
                    on ? NexusTheme.Gold : NexusTheme.MutedText,
                    11f);
                x += 92f;
            }
        }

        private static Color CategoryTint(BridgeLogCategory category) => category switch
        {
            BridgeLogCategory.Explore => NexusTheme.Cyan,
            BridgeLogCategory.Combat => NexusTheme.Gold,
            BridgeLogCategory.Production => NexusTheme.Green,
            BridgeLogCategory.Trade => NexusTheme.Purple,
            _ => NexusTheme.MutedText
        };

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
