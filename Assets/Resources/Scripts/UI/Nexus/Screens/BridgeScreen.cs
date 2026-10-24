using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Market;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Quest;
using Assets.Resources.Scripts.Quest.Domain;
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
        private readonly System.Action openFormation;
        private readonly System.Action openExplore;
        private readonly System.Action openShip;
        private readonly System.Action<AppScreen> navigate;
        private BridgeLogCategory? logFilter;
        private QuestType? boardType;
        private CommanderGoalService.QuestBoardStatusFilter boardStatus = CommanderGoalService.QuestBoardStatusFilter.All;
        private string selectedQuestId = "";

        private BridgeScreen(
            Transform root,
            System.Action openFormation,
            System.Action openExplore,
            System.Action openShip,
            System.Action<AppScreen> navigate)
        {
            this.root = root;
            this.openFormation = openFormation;
            this.openExplore = openExplore;
            this.openShip = openShip;
            this.navigate = navigate;
        }

        public GameObject Root => root.gameObject;

        public static BridgeScreen Build(
            Transform parent,
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
                root.transform, openFormation, openExplore, openShip, navigate);
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

            int power = NexusProgressUi.ActiveCombatPower();
            int progress = WorldService.State?.explorationProgress
                ?? DataUtil.Instance?.currentPlayer?.explorationProgress ?? 0;
            int cards = all.Count;
            var combatMembers = DeckService.GetActiveCombatMembers(all);
            int inLine = combatMembers.Count;

            BuildActiveFleet(all);
            BuildFleetCarousel();
            BuildCommanderGoal();
            BuildPendingLoot();
            BuildSectors();
            BuildLog(inLine, cards, progress, power);

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
            var headline = CommanderGoalService.CurrentStoryHeadline();
            var visible = CommanderGoalService.ListBoard(boardType, boardStatus);
            EnsureBoardSelection(visible, headline.QuestId);
            QuestView selected = FindBoardQuest(visible, selectedQuestId);
            int claimable = CountClaimable(visible);

            GameObject panel = NexusUiFactory.CreateBox(
                root, "Commander Goal",
                new Vector2(1090f, 128f), new Vector2(678f, 300f),
                NexusTheme.Surface, NexusTheme.BorderSoft);

            // The bridge content sits beside the left nav, so the card's right edge
            // can fall outside a 1920-wide game view. Keep text and buttons inside
            // the visible span; the card chrome stays aligned with the loot panel.
            const float contentW = 580f;
            NexusUiFactory.CreateText(
                panel.transform, "GoalHead", UiText.BridgeCommanderGoal,
                new Vector2(12f, 6f), new Vector2(contentW, 16f), 11f, NexusTheme.MutedText);
            NexusUiFactory.CreateText(
                panel.transform, "GoalTitle", headline.Title,
                new Vector2(12f, 22f), new Vector2(contentW, 20f), 15f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);

            DrawBoardChips(panel.transform, 12f, 46f, TypeChips());
            DrawBoardChips(panel.transform, 12f, 72f, StatusChips());

            var content = CreateBoardScroll(panel.transform, new Vector2(12f, 98f), new Vector2(contentW, 158f));
            if (visible.Count == 0)
            {
                string empty = boardStatus == CommanderGoalService.QuestBoardStatusFilter.InProgress
                    ? UiText.QuestTrackerEmpty
                    : UiText.BridgeQuestFilterEmpty;
                NexusUiFactory.CreateText(
                    panel.transform, "BoardEmpty", empty,
                    new Vector2(20f, 112f), new Vector2(560f, 36f), 12f, NexusTheme.MutedText);
            }
            else
            {
                const float rowH = 34f;
                for (int i = 0; i < visible.Count; i++)
                    DrawBoardRow(content, visible[i], i * rowH, contentW - 8f, visible[i].Def.questId == selectedQuestId);
                var contentRect = content.GetComponent<RectTransform>();
                contentRect.sizeDelta = new Vector2(0f, visible.Count * rowH);
            }

            bool canGo = selected != null && selected.Status != QuestStatus.Claimed
                && (selected.Def.questId == QuestIds.ChPrologue || !string.IsNullOrEmpty(QuestService.TargetScreen(selected.Def)));
            var go = NexusUiFactory.CreateRoleButton(
                panel.transform, "BoardGo", UiText.QuestGo,
                new Vector2(12f, 262f), new Vector2(120f, 30f),
                () => GoToQuest(selected),
                canGo ? NexusButtonRole.Primary : NexusButtonRole.Disabled, 12f);
            go.interactable = canGo;

            bool canClaim = selected != null && selected.CanClaim;
            var claim = NexusUiFactory.CreateRoleButton(
                panel.transform, "BoardClaim", UiText.QuestClaim,
                new Vector2(140f, 262f), new Vector2(100f, 30f),
                () => ClaimQuest(selected),
                canClaim ? NexusButtonRole.Confirm : NexusButtonRole.Disabled, 12f);
            claim.interactable = canClaim;

            var claimAll = NexusUiFactory.CreateRoleButton(
                panel.transform, "BoardClaimAll", UiText.MissionsClaimTab,
                new Vector2(372f, 262f), new Vector2(196f, 30f),
                () => ClaimVisible(visible),
                claimable > 0 ? NexusButtonRole.Confirm : NexusButtonRole.Disabled, 12f);
            claimAll.interactable = claimable > 0;
        }

        private struct BoardChip
        {
            public string Name;
            public string Label;
            public bool On;
            public System.Action Click;
        }

        private BoardChip[] TypeChips()
        {
            return new[]
            {
                Chip("TypeAll", UiText.BridgeQuestFilterAll, boardType == null, () => boardType = null),
                Chip("TypeStory", UiText.BridgeQuestFilterStory, boardType == QuestType.Story, () => boardType = QuestType.Story),
                Chip("TypeTeach", UiText.QuestOnboardingTitle, boardType == QuestType.Onboarding, () => boardType = QuestType.Onboarding),
                Chip("TypeExpedition", UiText.QuestExpeditionTitle, boardType == QuestType.Expedition, () => boardType = QuestType.Expedition),
                Chip("TypeShip", UiText.QuestShipTitle, boardType == QuestType.Ship, () => boardType = QuestType.Ship),
                Chip("TypeLoop", UiText.QuestLoopTitle, boardType == QuestType.Loop, () => boardType = QuestType.Loop)
            };
        }

        private BoardChip[] StatusChips()
        {
            return new[]
            {
                Chip("StatusAll", UiText.BridgeQuestFilterAll, boardStatus == CommanderGoalService.QuestBoardStatusFilter.All, () => boardStatus = CommanderGoalService.QuestBoardStatusFilter.All),
                Chip("StatusActive", UiText.MissionsStatusActive, boardStatus == CommanderGoalService.QuestBoardStatusFilter.InProgress, () => boardStatus = CommanderGoalService.QuestBoardStatusFilter.InProgress),
                Chip("StatusReady", UiText.BridgeQuestStatusReady, boardStatus == CommanderGoalService.QuestBoardStatusFilter.Ready, () => boardStatus = CommanderGoalService.QuestBoardStatusFilter.Ready),
                Chip("StatusDone", UiText.MissionsStatusDone, boardStatus == CommanderGoalService.QuestBoardStatusFilter.Claimed, () => boardStatus = CommanderGoalService.QuestBoardStatusFilter.Claimed)
            };
        }

        private static BoardChip Chip(string name, string label, bool on, System.Action click) =>
            new BoardChip { Name = name, Label = label, On = on, Click = click };

        private void DrawBoardChips(Transform parent, float x, float y, BoardChip[] chips)
        {
            float cursor = x;
            for (int i = 0; i < chips.Length; i++)
            {
                var chip = chips[i];
                float width = ChipWidth(chip.Label);
                var captured = chip.Click;
                var button = NexusUiFactory.CreateButton(
                    parent, chip.Name, chip.Label,
                    new Vector2(cursor, y), new Vector2(width, 22f),
                    () =>
                    {
                        captured?.Invoke();
                        Rebuild();
                    },
                    chip.On ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.28f) : NexusTheme.SurfaceRaised,
                    chip.On ? NexusTheme.Gold : NexusTheme.MutedText,
                    11f);
                if (chip.On)
                    button.GetComponent<Image>().color = NexusTheme.WithAlpha(NexusTheme.Gold, 0.28f);
                cursor += width + 4f;
            }
        }

        private static float ChipWidth(string label)
        {
            int chars = string.IsNullOrEmpty(label) ? 2 : label.Length;
            return Mathf.Clamp(chars * 13f + 16f, 52f, 132f);
        }

        private static Transform CreateBoardScroll(Transform parent, Vector2 position, Vector2 size)
        {
            var viewport = new GameObject("QuestScroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(parent, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 1f);
            viewportRect.anchorMax = new Vector2(0f, 1f);
            viewportRect.pivot = new Vector2(0f, 1f);
            viewportRect.anchoredPosition = new Vector2(position.x, -position.y);
            viewportRect.sizeDelta = size;
            var image = viewport.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.01f);
            image.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, size.y);

            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            return content.transform;
        }

        private void DrawBoardRow(Transform parent, QuestView view, float y, float width, bool selected)
        {
            if (view?.Def == null) return;
            var def = view.Def;
            string title = UiText.T(def.titleEn, def.titleZh);
            string status = BoardStatusLabel(view);
            var bg = selected
                ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f)
                : view.Status == QuestStatus.Ready
                    ? NexusTheme.WithAlpha(NexusTheme.Green, 0.14f)
                    : NexusTheme.SurfaceRaised;
            string questId = def.questId;
            var row = NexusUiFactory.CreateBox(
                parent, "Quest " + questId,
                new Vector2(0f, y), new Vector2(width, 32f),
                bg, selected ? NexusTheme.Gold : NexusTheme.BorderSoft);
            var rowImage = row.GetComponent<Image>();
            rowImage.raycastTarget = true;
            var button = row.AddComponent<Button>();
            button.targetGraphic = rowImage;
            button.onClick.AddListener(() =>
            {
                selectedQuestId = questId;
                Rebuild();
            });

            NexusUiFactory.CreateText(
                row.transform, "Title",
                def.displayId + "  " + title,
                new Vector2(8f, 6f), new Vector2(width - 150f, 20f), 12f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(
                row.transform, "Status", status,
                new Vector2(width - 136f, 6f), new Vector2(124f, 20f), 11f,
                view.Status == QuestStatus.Ready ? NexusTheme.Green : NexusTheme.MutedText,
                TextAlignmentOptions.Right);
        }

        private static string BoardStatusLabel(QuestView view)
        {
            if (view.Status == QuestStatus.Claimed) return UiText.MissionsStatusDone;
            if (view.Status == QuestStatus.Ready) return UiText.QuestStepReady;
            if (view.Def.type == QuestType.Loop && view.LoopTarget > 0)
                return UiText.QuestLoopProgress(view.LoopCount, view.LoopTarget);
            return UiText.MissionsStatusActive;
        }

        private void EnsureBoardSelection(List<QuestView> visible, string storyId)
        {
            if (visible == null || visible.Count == 0)
            {
                selectedQuestId = "";
                return;
            }

            if (FindBoardQuest(visible, selectedQuestId) != null)
                return;
            if (FindBoardQuest(visible, storyId) != null)
            {
                selectedQuestId = storyId;
                return;
            }

            selectedQuestId = visible[0].Def.questId;
        }

        private static QuestView FindBoardQuest(List<QuestView> visible, string questId)
        {
            if (visible == null || string.IsNullOrEmpty(questId)) return null;
            foreach (var view in visible)
            {
                if (view?.Def != null && view.Def.questId == questId)
                    return view;
            }

            return null;
        }

        private static int CountClaimable(List<QuestView> visible)
        {
            int count = 0;
            if (visible == null) return 0;
            foreach (var view in visible)
            {
                if (view != null && view.CanClaim)
                    count++;
            }

            return count;
        }

        private void GoToQuest(QuestView view)
        {
            if (view?.Def == null || view.Status == QuestStatus.Claimed) return;
            if (view.Def.questId == QuestIds.ChPrologue && view.Status == QuestStatus.Active)
            {
                ChapterQuestService.StartPrologueBattle();
                return;
            }

            var target = MapQuestTarget(QuestService.TargetScreen(view.Def));
            if (navigate != null)
                navigate(target);
            else if (target == AppScreen.Formation)
                openFormation?.Invoke();
            else if (target == AppScreen.Battle)
                openExplore?.Invoke();
            else if (target == AppScreen.Ship)
                openShip?.Invoke();
        }

        private void ClaimQuest(QuestView view)
        {
            if (view?.Def == null || !view.CanClaim) return;
            var result = QuestService.TryClaim(view.Def.questId);
            if (!result.Success && !string.IsNullOrEmpty(result.Message))
                NexusSnackbar.Show(result.Message);
            Rebuild();
        }

        private void ClaimVisible(List<QuestView> visible)
        {
            var ids = new List<string>();
            foreach (var view in visible)
            {
                if (view?.Def != null && view.CanClaim)
                    ids.Add(view.Def.questId);
            }

            if (ids.Count == 0)
            {
                NexusSnackbar.Show(UiText.MissionsClaimTabEmpty);
                return;
            }

            int before = CurrencyService.Credits;
            int claimed = 0;
            foreach (var id in ids)
            {
                if (QuestService.TryClaim(id).Success)
                    claimed++;
            }

            int delta = CurrencyService.Credits - before;
            NexusSnackbar.Show(claimed > 0
                ? UiText.MissionsClaimTabResult(claimed, delta)
                : UiText.MissionsClaimTabEmpty);
            Rebuild();
        }

        private static AppScreen MapQuestTarget(string targetScreen) =>
            targetScreen switch
            {
                "Formation" => AppScreen.Formation,
                "Battle" => AppScreen.Battle,
                "Recruit" => AppScreen.Recruit,
                "Crafting" => AppScreen.Crafting,
                "Market" => AppScreen.Market,
                "Ship" => AppScreen.Ship,
                "Inventory" => AppScreen.Inventory,
                _ => AppScreen.Bridge
            };

        private void BuildCtas()
        {
            // P6: peer CTAs removed; use commander goal + shell navigation.
        }

        private void BuildActiveFleet(List<CardEntity> all)
        {
            GameObject fleet = NexusUiFactory.CreateBox(
                root,
                "Fleet",
                new Vector2(28f, 128f),
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

            const float slotW = 150f;
            const float slotH = 148f;
            const float gap = 20f;
            float x = 20f;
            for (int i = 0; i < DeckConstants.SlotsPerDeck; i++)
            {
                CardEntity entity = combat != null
                    ? DeckService.FindMemberInSlot(combat.deckId, i, all)
                    : null;
                if (entity != null)
                {
                    string footer = $"Lv.{entity.Level} · {UiText.SlotLabel(i + 1)}";
                    NexusCardVisual.CreatePortraitCard(
                        fleet.transform,
                        $"Unit {i}",
                        entity,
                        new Vector2(x, 36f),
                        new Vector2(slotW, slotH),
                        footer);
                }
                else
                {
                    GameObject empty = NexusUiFactory.CreateBox(
                        fleet.transform,
                        $"Empty {i}",
                        new Vector2(x, 36f),
                        new Vector2(slotW, slotH),
                        NexusTheme.WithAlpha(NexusTheme.SurfaceRaised, 0.55f),
                        NexusTheme.BorderSoft);
                    var emptyImg = empty.GetComponent<Image>();
                    emptyImg.raycastTarget = true;
                    var emptyBtn = empty.AddComponent<Button>();
                    emptyBtn.onClick.AddListener(() => openFormation?.Invoke());
                    NexusUiFactory.CreateText(
                        empty.transform,
                        "Slot",
                        UiText.SlotLabel(i + 1),
                        new Vector2(8f, 12f),
                        new Vector2(slotW - 16f, 20f),
                        11f,
                        NexusTheme.DimText,
                        TextAlignmentOptions.Center);
                    NexusUiFactory.CreateText(
                        empty.transform,
                        "Plus",
                        UiText.BridgeEmptySlot,
                        new Vector2(8f, slotH * 0.5f - 12f),
                        new Vector2(slotW - 16f, 28f),
                        13f,
                        NexusTheme.Cyan,
                        TextAlignmentOptions.Center,
                        FontStyles.Bold);
                }

                x += slotW + gap;
            }
        }

        private void BuildFleetCarousel()
        {
            GameObject ops = NexusUiFactory.CreateBox(
                root,
                "FleetCarousel",
                new Vector2(28f, 360f),
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
                new Vector2(1090f, 440f),
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
                        var lines = RewardPopup.FromPending(claimed);
                        lines.AddRange(RewardPopup.FromProgressNotes(IdleSettlementService.State?.lastProgressNotes));
                        IdleSettlementService.State.lastProgressNotes = new List<Assets.Resources.Scripts.Economy.Domain.OfflineProgressNote>();
                        IdleSettlementService.Save();
                        RewardPopup.Show(UiText.RewardTitle, lines);
                        Rebuild();
                    },
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f),
                    NexusTheme.Gold,
                    13f);
            }
        }

        private static readonly Color LockedTint = new Color(0.36f, 0.40f, 0.48f, 0.55f);

        private void BuildSectors()
        {
            GameObject sectors = NexusUiFactory.CreateBox(
                root,
                "Sectors",
                new Vector2(28f, 512f),
                new Vector2(1040f, 340f),
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
            const float itemH = 260f;
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
                    new Vector2(44f, 36f),
                    new Vector2(100f, 100f),
                    locked ? LockedTint : Color.white);
                NexusUiFactory.CreateText(
                    cell.transform,
                    "Name",
                    name,
                    new Vector2(8f, 150f),
                    new Vector2(itemW - 16f, 36f),
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
                        new Vector2(8f, 190f),
                        new Vector2(itemW - 16f, 24f),
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
                            new Vector2(itemW * 0.5f - 16f, 56f),
                            new Vector2(32f, 32f),
                            NexusTheme.Text);
                    }

                    NexusUiFactory.CreateText(
                        cell.transform,
                        "LockLabel",
                        UiText.RegionLocked,
                        new Vector2(8f, 110f),
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

        private void BuildLog(int inLine, int cards, int progress, int power)
        {
            BridgeEventLog.Ensure();
            if (inLine < DeckConstants.SlotsPerDeck)
            {
                BridgeEventLog.UpsertLive("live_fleet", BridgeLogCategory.Combat,
                    "Combat deck incomplete — open Formation to fill slots",
                    "战斗编队未满 — 打开编队补齐席位");
            }
            else
            {
                BridgeEventLog.UpsertLive("live_fleet", BridgeLogCategory.Combat,
                    $"Fleet ready: {inLine}/{DeckConstants.SlotsPerDeck} · roster {cards}",
                    $"编队就绪：{inLine}/{DeckConstants.SlotsPerDeck} · 卡池 {cards}");
            }

            BridgeEventLog.UpsertLive("live_explore", BridgeLogCategory.Explore,
                $"Grid restore {progress}% — open Explore to chart neighbors",
                $"航网恢复 {progress}% — 打开探索标绘邻接节点");
            BridgeEventLog.UpsertLive("live_power", BridgeLogCategory.Combat,
                $"Combat power {power:N0} — compare vs node recommended power",
                $"战力 {power:N0} — 对照节点推荐战力");
            BridgeEventLog.UpsertLive("live_ops", BridgeLogCategory.Production,
                $"Parallel ops {DeckService.CountBusyDecks()}/{DeckService.MaxParallelActions}",
                $"并行行动 {DeckService.CountBusyDecks()}/{DeckService.MaxParallelActions}");

            GameObject log = NexusUiFactory.CreateBox(
                root,
                "Log",
                new Vector2(1090f, 552f),
                new Vector2(678f, 300f),
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

        private static void ClearDynamicChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
}
