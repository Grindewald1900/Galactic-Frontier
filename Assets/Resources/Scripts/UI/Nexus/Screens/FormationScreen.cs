using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Characters;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.UI.Nexus.Tutorial;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Multi-deck formation editor: switch unlocked decks, edit 5 slots, set active combat deck.
    /// Idle decks are backup presets (P1.5); running decks require stop before edit.
    /// </summary>
    internal sealed class FormationScreen
    {
        private readonly Transform root;
        private readonly Transform deckTabsRoot;
        private readonly Transform rosterRoot;
        private readonly Transform slotsRoot;
        private readonly Transform detailRoot;
        private readonly TextMeshProUGUI statsText;
        private readonly TextMeshProUGUI statusText;
        private Button setCombatButton;
        private Button strategyButton;
        private Button stopButton;
        private int selectedSlot = -1;
        private CardEntity selectedCard;
        private string pendingStopDeckId = "";
        private int archetypeFilter;
        private int factionFilter;
        private int rarityFilter;
        private int sortMode;
        private string openFilterMenu = "";

        private const float DeckBarH = 92f;
        private const float SlotBarH = 188f;
        private const float RosterW = 300f;
        private const float StatsW = 268f;

        private static readonly Archetype[] ArchetypeCycle =
        {
            Archetype.Assassin, Archetype.Magician, Archetype.Mechanician,
            Archetype.Monster, Archetype.Potioneer, Archetype.Warrior
        };

        private static readonly EquipSlot[] GearSlots =
        {
            EquipSlot.Weapon, EquipSlot.Armor, EquipSlot.Accessory, EquipSlot.Tool
        };

        private static readonly CharacterTier[] RarityMins =
        {
            CharacterTier.None,
            CharacterTier.TierB,
            CharacterTier.TierA,
            CharacterTier.TierS,
            CharacterTier.TierSS
        };

        private FormationScreen(
            Transform root,
            Transform deckTabsRoot,
            Transform rosterRoot,
            Transform slotsRoot,
            Transform detailRoot,
            TextMeshProUGUI statsText,
            TextMeshProUGUI statusText)
        {
            this.root = root;
            this.deckTabsRoot = deckTabsRoot;
            this.rosterRoot = rosterRoot;
            this.slotsRoot = slotsRoot;
            this.detailRoot = detailRoot;
            this.statsText = statsText;
            this.statusText = statusText;
        }

        public static FormationScreen Build(Transform parent)
        {
            GameObject root = NexusUiFactory.CreatePanel(
                parent,
                "Formation Screen",
                NexusTheme.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            GameObject deckBar = NexusUiFactory.CreatePanel(
                root.transform,
                "DeckTabs",
                NexusTheme.Surface,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -DeckBarH),
                new Vector2(0f, 0f),
                true);
            NexusUiFactory.CreateText(
                deckBar.transform,
                "Header",
                UiText.DeckListHeader,
                new Vector2(16f, 8f),
                new Vector2(240f, 22f),
                12f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            GameObject roster = NexusUiFactory.CreatePanel(
                root.transform,
                "Roster",
                NexusTheme.Surface,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0f),
                new Vector2(RosterW, -DeckBarH),
                true);
            roster.AddComponent<RectMask2D>();
            NexusUiFactory.CreateText(
                roster.transform,
                "Header",
                UiText.AvailableCharacters,
                new Vector2(12f, 8f),
                new Vector2(256f, 24f),
                13f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            GameObject center = NexusUiFactory.CreatePanel(
                root.transform,
                "Detail",
                NexusTheme.Background,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(RosterW, SlotBarH),
                new Vector2(-StatsW, -DeckBarH));
            center.AddComponent<RectMask2D>();
            NexusUiFactory.CreateText(
                center.transform,
                "Header",
                UiText.FormationTitle,
                new Vector2(24f, 12f),
                new Vector2(520f, 28f),
                20f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            GameObject slotBar = NexusUiFactory.CreatePanel(
                root.transform,
                "SlotBar",
                NexusTheme.Surface,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(RosterW, 0f),
                new Vector2(0f, SlotBarH),
                true);
            slotBar.AddComponent<RectMask2D>();
            NexusUiFactory.CreateText(
                slotBar.transform,
                "Header",
                UiText.DeckLineup,
                new Vector2(16f, 8f),
                new Vector2(420f, 20f),
                12f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            GameObject slotsHost = new GameObject("Slots", typeof(RectTransform));
            slotsHost.transform.SetParent(slotBar.transform, false);
            var slotsRect = slotsHost.GetComponent<RectTransform>();
            slotsRect.anchorMin = new Vector2(0f, 1f);
            slotsRect.anchorMax = new Vector2(0f, 1f);
            slotsRect.pivot = new Vector2(0f, 1f);
            slotsRect.anchoredPosition = new Vector2(16f, -28f);
            slotsRect.sizeDelta = new Vector2(800f, 152f);

            GameObject stats = NexusUiFactory.CreatePanel(
                root.transform,
                "Stats",
                NexusTheme.Surface,
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(-StatsW, SlotBarH),
                new Vector2(0f, -DeckBarH),
                true);
            stats.AddComponent<RectMask2D>();
            NexusUiFactory.CreateText(
                stats.transform,
                "Header",
                UiText.FormationStats,
                new Vector2(16f, 14f),
                new Vector2(240f, 28f),
                14f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            var statsText = NexusUiFactory.CreateText(
                stats.transform,
                "Values",
                string.Empty,
                new Vector2(16f, 36f),
                new Vector2(StatsW - 32f, 180f),
                12f,
                NexusTheme.Text);
            statsText.textWrappingMode = TextWrappingModes.Normal;
            statsText.overflowMode = TextOverflowModes.Truncate;
            var valuesRect = statsText.rectTransform;
            valuesRect.anchorMin = new Vector2(0f, 0f);
            valuesRect.anchorMax = new Vector2(1f, 1f);
            valuesRect.offsetMin = new Vector2(16f, 228f);
            valuesRect.offsetMax = new Vector2(-16f, -36f);
            var statusText = NexusUiFactory.CreateText(
                stats.transform,
                "Status",
                string.Empty,
                new Vector2(16f, 0f),
                new Vector2(StatsW - 32f, 36f),
                11f,
                NexusTheme.Gold);
            statusText.textWrappingMode = TextWrappingModes.Normal;
            statusText.overflowMode = TextOverflowModes.Ellipsis;
            PinBottomLeft(statusText, 16f, 188f, new Vector2(StatsW - 32f, 36f));

            var screen = new FormationScreen(
                root.transform,
                deckBar.transform,
                roster.transform,
                slotsHost.transform,
                center.transform,
                statsText,
                statusText);
            screen.Rebuild();

            var setCombat = NexusUiFactory.CreateButton(
                stats.transform,
                "SetCombat",
                UiText.SetCombatDeck,
                new Vector2(16f, 340f),
                new Vector2(StatsW - 32f, 40f),
                () => screen.SetAsCombatDeck(),
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.16f),
                NexusTheme.Cyan,
                13f);
            var strategyBtn = NexusUiFactory.CreateButton(
                stats.transform,
                "Strategy",
                UiText.CycleStrategy,
                new Vector2(16f, 388f),
                new Vector2(StatsW - 32f, 40f),
                () => screen.CycleStrategy(),
                NexusTheme.WithAlpha(NexusTheme.Purple, 0.16f),
                NexusTheme.Purple,
                13f);
            var stopBtn = NexusUiFactory.CreateButton(
                stats.transform,
                "Stop",
                UiText.StopAction,
                new Vector2(16f, 436f),
                new Vector2(StatsW - 32f, 40f),
                () => screen.TryStopEditingDeck(),
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.12f),
                NexusTheme.Gold,
                13f);

            screen.setCombatButton = setCombat;
            screen.strategyButton = strategyBtn;
            screen.stopButton = stopBtn;
            screen.LayoutDeckButtons();

            return screen;
        }

        /// <summary>
        /// Stacks the stats-panel buttons from the bottom up. Combat controls only exist for the
        /// active combat deck; every other deck just offers promotion into the combat slot.
        /// </summary>
        private void LayoutDeckButtons()
        {
            if (setCombatButton == null) return;

            var editing = DeckService.GetEditingDeck();
            bool isCombatDeck = editing != null && editing.deckId == DeckService.GetActiveCombatDeck()?.deckId;
            var size = new Vector2(StatsW - 32f, 40f);
            float bottom = 12f;

            void Stack(Component button, bool visible)
            {
                if (button == null) return;
                button.gameObject.SetActive(visible);
                if (!visible) return;
                PinBottomLeft(button, 16f, bottom, size);
                bottom += 44f;
            }

            Stack(stopButton, isCombatDeck);
            Stack(strategyButton, isCombatDeck);
            Stack(setCombatButton, !isCombatDeck);
        }

        public GameObject Root => root.gameObject;

        public void Rebuild()
        {
            TutorialGuideService.UnregisterAnchor("formation_empty_slot");
            TutorialGuideService.UnregisterAnchor("formation_join");
            CloseFilterMenu();
            CloseGearPicker();
            ClearChildren(deckTabsRoot, keepHeader: true);
            ClearChildren(rosterRoot, keepHeader: true);
            ClearChildren(detailRoot, keepHeader: true);
            ClearChildren(slotsRoot, keepHeader: false);

            List<CardEntity> all = CardListManager.Instance?.GetCardEntities() ?? new List<CardEntity>();
            if (DataUtil.Instance != null)
                DeckService.EnsureLoaded(DataUtil.Instance, all);

            BuildDeckTabs();
            BuildRoster(all);
            EnsureSelectedCard(all);
            BuildDetail(all);
            BuildSlots(all);
            RefreshStats(all);
            LayoutDeckButtons();
        }

        private void BuildDeckTabs()
        {
            var decks = DeckService.GetDecks();
            float x = 16f;
            for (var i = 0; i < decks.Count; i++)
            {
                var deck = decks[i];
                if (deck == null) continue;
                var deckId = deck.deckId;
                bool selected = deckId == DeckService.EditingDeckId;
                bool combat = deckId == DeckService.GetActiveCombatDeck()?.deckId;
                Color fill = !deck.unlocked
                    ? NexusTheme.WithAlpha(NexusTheme.DimText, 0.2f)
                    : selected
                        ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f)
                        : NexusTheme.SurfaceRaised;
                Color border = selected ? NexusTheme.Gold : NexusTheme.BorderSoft;

                string label = deck.unlocked
                    ? Truncate(deck.displayName, 14)
                    : $"{UiText.DeckLocked} {i + 1}";
                if (combat && deck.unlocked)
                    label = "★ " + label;

                GameObject tab = NexusUiFactory.CreateBox(
                    deckTabsRoot,
                    $"DeckTab {i}",
                    new Vector2(x, 36f),
                    new Vector2(170f, 48f),
                    fill,
                    border);
                NexusUiFactory.CreateText(
                    tab.transform,
                    "Name",
                    label,
                    new Vector2(8f, 6f),
                    new Vector2(154f, 20f),
                    12f,
                    deck.unlocked ? NexusTheme.Text : NexusTheme.DimText,
                    TextAlignmentOptions.Left,
                    FontStyles.Bold);
                string sub = deck.unlocked
                    ? $"{UiText.DeckPurposeLabel(deck.purpose.ToString())} · {MemberCountLabel(deck)}"
                    : UiText.DeckUnlockHint(
                        DeckUnlockTable.GetSlotUnlockRequirementEn(i),
                        DeckUnlockTable.GetSlotUnlockRequirementZh(i));
                NexusUiFactory.CreateText(
                    tab.transform,
                    "Sub",
                    Truncate(sub, 22),
                    new Vector2(8f, 26f),
                    new Vector2(154f, 18f),
                    10f,
                    NexusTheme.MutedText);

                var image = tab.GetComponent<Image>();
                image.raycastTarget = true;
                var button = tab.AddComponent<Button>();
                button.targetGraphic = image;
                int slotIndex = i;
                button.onClick.AddListener(() => OnDeckTabClicked(deckId, slotIndex, deck.unlocked));
                if (!deck.unlocked)
                {
                    var locked = tab.AddComponent<CanvasGroup>();
                    locked.alpha = 0.5f;
                    locked.interactable = true;
                    locked.blocksRaycasts = true;
                }
                x += 178f;
            }
        }

        private void OnDeckTabClicked(string deckId, int slotIndex, bool unlocked)
        {
            if (!unlocked)
            {
                statusText.text = UiText.DeckUnlockHint(
                    DeckUnlockTable.GetSlotUnlockRequirementEn(slotIndex),
                    DeckUnlockTable.GetSlotUnlockRequirementZh(slotIndex));
                return;
            }

            DeckService.TrySelectDeck(deckId);
            pendingStopDeckId = "";
            selectedCard = null;
            selectedSlot = -1;
            Rebuild();
        }

        private void EnsureSelectedCard(List<CardEntity> all)
        {
            var editing = DeckService.GetEditingDeck();

            if (selectedCard != null && all != null)
            {
                bool found = false;
                foreach (var entity in all)
                {
                    if (entity != null && entity.id == selectedCard.id)
                    {
                        selectedCard = entity;
                        found = true;
                        break;
                    }
                }

                if (found && editing != null && IndexInEditingDeck(editing, selectedCard.id) >= 0)
                    return;
            }

            if (selectedSlot >= 0 && selectedCard == null)
                return;

            if (selectedSlot >= 0 && selectedCard != null)
                return;

            selectedCard = null;
            selectedSlot = -1;
            if (editing == null)
                return;

            for (int i = 0; i < DeckConstants.SlotsPerDeck; i++)
            {
                var member = DeckService.FindMemberInSlot(editing.deckId, i, all);
                if (member == null)
                    continue;
                selectedCard = member;
                selectedSlot = i;
                return;
            }
        }

        private void BuildRoster(List<CardEntity> all)
        {
            var editing = DeckService.GetEditingDeck();
            var memberIds = new HashSet<string>();
            if (editing?.slotCardIds != null)
            {
                foreach (var id in editing.slotCardIds)
                {
                    if (!string.IsNullOrEmpty(id))
                        memberIds.Add(id);
                }
            }

            var membership = BuildDeckMembership();
            var visible = FilterAndSort(all, memberIds);
            var header = rosterRoot.Find("Header")?.GetComponent<TextMeshProUGUI>();
            if (header != null)
            {
                int availableTotal = CountAvailableForDeck(all, memberIds);
                header.text = $"{UiText.AvailableCharacters}  {visible.Count}/{availableTotal}";
            }

            DrawRosterFilters();
            var content = CreateRosterScroll(rosterRoot);
            if (visible.Count == 0)
            {
                var empty = NexusUiFactory.CreateText(
                    content,
                    "Empty",
                    UiText.RosterEmpty,
                    new Vector2(8f, 8f),
                    new Vector2(240f, 56f),
                    12f,
                    NexusTheme.MutedText);
                empty.textWrappingMode = TextWrappingModes.Normal;
                return;
            }

            const float rowH = 64f;
            float y = 4f;
            foreach (CardEntity entity in visible)
            {
                DrawRosterRow(content, entity, membership, memberIds, y);
                y += rowH;
            }

            var contentRect = content.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(0f, Mathf.Max(80f, 8f + visible.Count * rowH));
        }

        private void DrawRosterFilters()
        {
            DrawRosterFiltersOn(rosterRoot, Vector2.zero);
        }

        private void DrawRosterFiltersOn(Transform parent, Vector2 offset)
        {
            string typeValue = archetypeFilter == 0
                ? UiText.RosterFilterAll
                : UiText.ArchetypeLabel(ArchetypeCycle[archetypeFilter - 1].ToString());
            string factionValue = UiText.RosterFactionLabel(factionFilter);
            string rarityValue = UiText.RosterRarityLabel(rarityFilter);
            string sortValue = UiText.RosterSortLabel(sortMode);

            DrawFilterChip(
                parent,
                "FilterType",
                new Vector2(12f, 34f) + offset,
                $"{UiText.RosterTypeChip(typeValue)}  ▾",
                archetypeFilter != 0 || openFilterMenu == "type",
                () => ToggleFilterMenu("type"));
            DrawFilterChip(
                parent,
                "FilterFaction",
                new Vector2(150f, 34f) + offset,
                $"{UiText.RosterFactionChip(factionValue)}  ▾",
                factionFilter != 0 || openFilterMenu == "faction",
                () => ToggleFilterMenu("faction"));
            DrawFilterChip(
                parent,
                "FilterRarity",
                new Vector2(12f, 72f) + offset,
                $"{UiText.RosterRarityChip(rarityValue)}  ▾",
                rarityFilter != 0 || openFilterMenu == "rarity",
                () => ToggleFilterMenu("rarity"));
            DrawFilterChip(
                parent,
                "FilterSort",
                new Vector2(150f, 72f) + offset,
                $"{UiText.RosterSortChip(sortValue)}  ▾",
                sortMode != 0 || openFilterMenu == "sort",
                () => ToggleFilterMenu("sort"));
        }

        private void DrawFilterChip(
            Transform parent, string name, Vector2 position, string label, bool active, UnityAction onClick)
        {
            NexusUiFactory.CreateButton(
                parent,
                name,
                label,
                position,
                new Vector2(138f, 32f),
                onClick,
                active ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f) : NexusTheme.SurfaceRaised,
                active ? NexusTheme.Gold : NexusTheme.Text,
                10f);
        }

        private void ToggleFilterMenu(string id)
        {
            if (openFilterMenu == id)
            {
                CloseFilterMenu();
                return;
            }

            CloseFilterMenu();
            openFilterMenu = id;
            DrawFilterDropdown();
        }

        private void CloseFilterMenu()
        {
            openFilterMenu = "";
            Transform overlay = root.Find("FilterOverlay");
            if (overlay != null)
                UnityEngine.Object.DestroyImmediate(overlay.gameObject);
        }

        private void DrawFilterDropdown()
        {
            GameObject overlay = NexusUiFactory.CreatePanel(
                root,
                "FilterOverlay",
                NexusTheme.WithAlpha(Color.black, 0.28f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                true);
            overlay.transform.SetAsLastSibling();
            var dismiss = overlay.AddComponent<Button>();
            dismiss.transition = Selectable.Transition.None;
            dismiss.onClick.AddListener(CloseFilterMenu);

            DrawRosterFiltersOn(overlay.transform, new Vector2(0f, DeckBarH));

            GetOpenMenuSpec(out Vector2 rosterChipPos, out string[] options, out int selected);
            const float itemH = 28f;
            float width = 168f;
            float height = 8f + options.Length * itemH;
            float menuX = rosterChipPos.x;
            if (menuX + width > RosterW - 8f)
                menuX = Mathf.Max(8f, RosterW - 8f - width);
            Vector2 menuPos = new Vector2(menuX, DeckBarH + rosterChipPos.y + 34f);

            GameObject menu = NexusUiFactory.CreateBox(
                overlay.transform,
                "FilterMenu",
                menuPos,
                new Vector2(width, height),
                NexusTheme.SurfaceRaised,
                NexusTheme.Gold);
            menu.GetComponent<Image>().raycastTarget = true;

            for (int i = 0; i < options.Length; i++)
            {
                int captured = i;
                bool isSelected = captured == selected;
                NexusUiFactory.CreateButton(
                    menu.transform,
                    "Option " + captured,
                    options[captured],
                    new Vector2(4f, 4f + captured * itemH),
                    new Vector2(width - 8f, itemH - 2f),
                    () => ApplyFilterChoice(captured),
                    isSelected ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f) : NexusTheme.Surface,
                    isSelected ? NexusTheme.Gold : NexusTheme.Text,
                    11f);
            }
        }

        private void GetOpenMenuSpec(out Vector2 rosterChipPos, out string[] options, out int selected)
        {
            switch (openFilterMenu)
            {
                case "faction":
                    rosterChipPos = new Vector2(150f, 34f);
                    options = new[]
                    {
                        UiText.RosterFilterAll,
                        UiText.RosterFactionLabel(1),
                        UiText.RosterFactionLabel(2)
                    };
                    selected = factionFilter;
                    return;
                case "rarity":
                    rosterChipPos = new Vector2(12f, 72f);
                    options = new[]
                    {
                        UiText.RosterFilterAll,
                        UiText.RosterRarityLabel(1),
                        UiText.RosterRarityLabel(2),
                        UiText.RosterRarityLabel(3),
                        UiText.RosterRarityLabel(4)
                    };
                    selected = rarityFilter;
                    return;
                case "sort":
                    rosterChipPos = new Vector2(150f, 72f);
                    options = new[]
                    {
                        UiText.RosterSortLabel(0),
                        UiText.RosterSortLabel(1),
                        UiText.RosterSortLabel(2),
                        UiText.RosterSortLabel(3),
                        UiText.RosterSortLabel(4)
                    };
                    selected = sortMode;
                    return;
                default:
                    rosterChipPos = new Vector2(12f, 34f);
                    options = TypeMenuOptions();
                    selected = archetypeFilter;
                    return;
            }
        }

        private static string[] TypeMenuOptions()
        {
            var options = new string[ArchetypeCycle.Length + 1];
            options[0] = UiText.RosterFilterAll;
            for (int i = 0; i < ArchetypeCycle.Length; i++)
                options[i + 1] = UiText.ArchetypeLabel(ArchetypeCycle[i].ToString());
            return options;
        }

        private void ApplyFilterChoice(int index)
        {
            switch (openFilterMenu)
            {
                case "faction":
                    factionFilter = index;
                    break;
                case "rarity":
                    rarityFilter = index;
                    break;
                case "sort":
                    sortMode = index;
                    break;
                default:
                    archetypeFilter = index;
                    break;
            }

            CloseFilterMenu();
            Rebuild();
        }

        private void DrawRosterRow(
            Transform parent,
            CardEntity entity,
            Dictionary<string, List<string>> membership,
            HashSet<string> currentMemberIds,
            float y)
        {
            bool assigned = membership.TryGetValue(entity.id, out var deckNames) && deckNames.Count > 0;
            bool inCurrent = currentMemberIds != null && currentMemberIds.Contains(entity.id);
            bool inOther = assigned && !inCurrent;
            bool selected = selectedCard != null && selectedCard.id == entity.id;
            var occ = DeckService.GetOccupation(entity.id);
            string occLabel = UiText.OccupationBadge(occ.ToString());
            string baseName = DisplayName(entity);

            string faction = CharacterFactionCatalog.LabelEn(entity.characterName);
            string factionZh = CharacterFactionCatalog.LabelZh(entity.characterName);
            string factionLabel = string.IsNullOrEmpty(faction) ? "" : UiText.T(faction, factionZh);
            string stats = $"{UiText.LevelAbbrev}{entity.Level}  {UiText.TierShort(entity.CharacterTier.ToString())}  {entity.power:N0}  {UiText.ArchetypeLabel(entity.archetype.ToString())}";
            if (!string.IsNullOrEmpty(factionLabel))
                stats += $"  {factionLabel}";

            string meta;
            if (assigned)
            {
                meta = UiText.RosterAssignedTo(JoinDeckNames(deckNames));
                if (!string.IsNullOrEmpty(occLabel))
                    meta += $" · {occLabel}";
            }
            else
            {
                meta = stats;
                if (!string.IsNullOrEmpty(occLabel))
                    meta += $" · {occLabel}";
            }

            Color fill = inOther
                ? NexusTheme.WithAlpha(NexusTheme.Surface, 0.55f)
                : inCurrent
                    ? NexusTheme.WithAlpha(NexusTheme.Cyan, 0.12f)
                    : NexusTheme.SurfaceRaised;
            Color border = selected ? NexusTheme.Gold : NexusTheme.BorderSoft;
            Color nameColor = inOther ? NexusTheme.DimText : inCurrent ? NexusTheme.Cyan : NexusTheme.Text;
            Color metaColor = inOther ? NexusTheme.DimText : NexusTheme.MutedText;

            CardEntity captured = entity;
            GameObject row = NexusUiFactory.CreateBox(
                parent,
                $"Roster {entity.id}",
                new Vector2(4f, y),
                new Vector2(RosterW - 28f, 56f),
                fill,
                border);
            var rowImage = row.GetComponent<Image>();
            rowImage.raycastTarget = true;
            NexusUiFactory.CreateIcon(
                row.transform,
                "Portrait",
                NexusCardVisual.CharacterSprite(entity),
                new Vector2(6f, 6f),
                new Vector2(44f, 44f),
                inOther ? new Color(1f, 1f, 1f, 0.35f) : Color.white);
            NexusUiFactory.CreateText(
                row.transform,
                "Label",
                baseName,
                new Vector2(56f, 6f),
                new Vector2(RosterW - 86f, 22f),
                11f,
                nameColor,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            NexusUiFactory.CreateText(
                row.transform,
                "Meta",
                meta,
                new Vector2(56f, 28f),
                new Vector2(RosterW - 86f, 22f),
                10f,
                metaColor);
            var button = row.AddComponent<Button>();
            button.targetGraphic = rowImage;
            if (inOther)
            {
                button.interactable = false;
                var colors = button.colors;
                colors.disabledColor = NexusTheme.WithAlpha(NexusTheme.Surface, 0.7f);
                button.colors = colors;
            }
            else
            {
                button.onClick.AddListener(() =>
                {
                    selectedCard = captured;
                    var deck = DeckService.GetEditingDeck();
                    if (selectedSlot >= 0 && deck != null && !IsSlotEmpty(deck, selectedSlot))
                        selectedSlot = -1;
                    Rebuild();
                });
            }
        }

        private static Dictionary<string, List<string>> BuildDeckMembership()
        {
            var map = new Dictionary<string, List<string>>();
            var decks = DeckService.GetDecks();
            if (decks == null)
                return map;

            string editingId = DeckService.EditingDeckId;
            foreach (var deck in decks)
            {
                if (deck == null || !deck.unlocked || deck.slotCardIds == null)
                    continue;
                string name = string.IsNullOrEmpty(deck.displayName) ? deck.deckId : deck.displayName;
                foreach (var id in deck.slotCardIds)
                {
                    if (string.IsNullOrEmpty(id))
                        continue;
                    if (!map.TryGetValue(id, out var names))
                    {
                        names = new List<string>();
                        map[id] = names;
                    }

                    if (names.Contains(name))
                        continue;
                    if (deck.deckId == editingId)
                        names.Insert(0, name);
                    else
                        names.Add(name);
                }
            }

            return map;
        }

        private static string JoinDeckNames(List<string> names)
        {
            if (names == null || names.Count == 0)
                return "";
            return string.Join(UiText.ListSeparator, names);
        }

        private List<CardEntity> FilterAndSort(List<CardEntity> all, HashSet<string> memberIds)
        {
            var filtered = new List<CardEntity>();
            foreach (CardEntity entity in all)
            {
                if (entity == null) continue;
                if (memberIds != null && memberIds.Contains(entity.id))
                    continue;
                if (archetypeFilter > 0 && entity.archetype != ArchetypeCycle[archetypeFilter - 1])
                    continue;
                if (rarityFilter > 0 && entity.CharacterTier < RarityMins[rarityFilter])
                    continue;
                if (factionFilter > 0)
                {
                    string tag = CharacterFactionCatalog.GetTag(entity.characterName);
                    if (factionFilter == 1 && tag != FactionTags.FrontierGuard)
                        continue;
                    if (factionFilter == 2 && tag != FactionTags.RiftSyndicate)
                        continue;
                }

                filtered.Add(entity);
            }

            filtered.Sort((a, b) =>
            {
                int cmp = sortMode switch
                {
                    1 => b.CharacterTier.CompareTo(a.CharacterTier),
                    2 => b.Level.CompareTo(a.Level),
                    3 => string.Compare(DisplayName(a), DisplayName(b), StringComparison.OrdinalIgnoreCase),
                    4 => string.Compare(a.archetype.ToString(), b.archetype.ToString(), StringComparison.Ordinal),
                    _ => b.power.CompareTo(a.power)
                };
                if (cmp != 0)
                    return cmp;
                int deckCmp = (memberIds.Contains(b.id) ? 1 : 0) - (memberIds.Contains(a.id) ? 1 : 0);
                if (deckCmp != 0)
                    return deckCmp;
                return string.Compare(DisplayName(a), DisplayName(b), StringComparison.OrdinalIgnoreCase);
            });
            return filtered;
        }

        private static int CountAvailableForDeck(List<CardEntity> all, HashSet<string> memberIds)
        {
            int count = 0;
            foreach (var entity in all)
            {
                if (entity != null && (memberIds == null || !memberIds.Contains(entity.id)))
                    count++;
            }

            return count;
        }

        private static int CountValid(List<CardEntity> all)
        {
            int count = 0;
            foreach (var entity in all)
            {
                if (entity != null)
                    count++;
            }

            return count;
        }

        private static string DisplayName(CardEntity entity)
        {
            return string.IsNullOrEmpty(entity.cardName)
                ? entity.characterName.ToString()
                : entity.cardName;
        }

        private static Transform CreateRosterScroll(Transform parent)
        {
            var viewport = new GameObject("RosterScroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(parent, false);

            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.pivot = new Vector2(0.5f, 1f);
            viewportRect.offsetMin = new Vector2(8f, 8f);
            viewportRect.offsetMax = new Vector2(-8f, -112f);

            var image = viewport.GetComponent<Image>();
            image.color = NexusTheme.WithAlpha(NexusTheme.Surface, 0.35f);
            image.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 80f);

            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            var track = new GameObject("RosterScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            track.transform.SetParent(parent, false);
            var trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(1f, 0f);
            trackRect.anchorMax = new Vector2(1f, 1f);
            trackRect.pivot = new Vector2(1f, 1f);
            trackRect.offsetMin = new Vector2(-16f, 8f);
            trackRect.offsetMax = new Vector2(-8f, -112f);
            var trackImage = track.GetComponent<Image>();
            trackImage.color = NexusTheme.WithAlpha(NexusTheme.BorderSoft, 0.45f);
            trackImage.raycastTarget = true;

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(track.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            handle.GetComponent<Image>().color = NexusTheme.WithAlpha(NexusTheme.Gold, 0.55f);

            var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(track.transform, false);
            var slidingRect = slidingArea.GetComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.offsetMin = new Vector2(1f, 4f);
            slidingRect.offsetMax = new Vector2(-1f, -4f);
            handle.transform.SetParent(slidingArea.transform, false);

            var scrollbar = track.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return content.transform;
        }

        private void BuildDetail(List<CardEntity> all)
        {
            if (selectedCard != null && all != null)
            {
                foreach (var entity in all)
                {
                    if (entity != null && entity.id == selectedCard.id)
                    {
                        selectedCard = entity;
                        break;
                    }
                }
            }

            if (selectedCard == null)
            {
                DrawOnboardingBanner();
                var hint = NexusUiFactory.CreateText(
                    detailRoot,
                    "Hint",
                    UiText.InspectHint,
                    new Vector2(20f, 64f),
                    new Vector2(Mathf.Max(400f, DetailSize().x - 40f), 48f),
                    14f,
                    NexusTheme.MutedText);
                hint.textWrappingMode = TextWrappingModes.Normal;
                DrawDeckActions(canJoin: false, canLeave: false);
                return;
            }

            DrawOnboardingBanner();
            CardEntity card = selectedCard;
            var editing = DeckService.GetEditingDeck();
            bool inCurrent = editing != null && IndexInEditingDeck(editing, card.id) >= 0;
            var membership = BuildDeckMembership();
            bool inOther = membership.TryGetValue(card.id, out var names) && names.Count > 0 && !inCurrent;
            bool canJoin = CanJoinSelected(editing, card, inCurrent, inOther, out _);
            bool canLeave = CanLeaveSelected(editing, inCurrent);

            string faction = CharacterFactionCatalog.LabelEn(card.characterName);
            string factionZh = CharacterFactionCatalog.LabelZh(card.characterName);
            string factionLabel = string.IsNullOrEmpty(faction) ? "" : UiText.T(faction, factionZh);
            string gold = ColorUtility.ToHtmlStringRGB(NexusTheme.Gold);
            string cyan = ColorUtility.ToHtmlStringRGB(NexusTheme.Cyan);
            string muted = ColorUtility.ToHtmlStringRGB(NexusTheme.MutedText);

            Vector2 panel = DetailSize();
            const float pad = 20f;
            const float gap = 12f;
            const float actionBand = 56f;
            float colTop = 168f;
            float colW = (panel.x - pad * 2f - gap * 2f) / 3f;
            float colH = Mathf.Max(140f, panel.y - colTop - actionBand - 8f);
            float x0 = pad;
            float x1 = pad + colW + gap;
            float x2 = pad + (colW + gap) * 2f;

            NexusUiFactory.CreateIcon(
                detailRoot,
                "Portrait",
                NexusCardVisual.CharacterSprite(card),
                new Vector2(pad, 64f),
                new Vector2(96f, 96f),
                Color.white);

            string identity =
                $"<size=20><color=#{gold}>{DisplayName(card)}</color></size>\n" +
                $"<color=#{cyan}>{card.characterName} · {UiText.ArchetypeLabel(card.archetype.ToString())}" +
                (string.IsNullOrEmpty(factionLabel) ? "" : $" · {factionLabel}") + "</color>\n" +
                $"{UiText.LevelAbbrev}{card.Level}  {UiText.TierShort(card.CharacterTier.ToString())}  " +
                $"<color=#{gold}>{card.power:N0}</color>";
            if (inCurrent)
                identity += $"\n<color=#{cyan}>{UiText.RosterAssignedTo(editing.displayName)}</color>";
            else if (inOther)
                identity += $"\n<color=#{muted}>{UiText.RosterAssignedTo(JoinDeckNames(names))}</color>";

            var identityText = NexusUiFactory.CreateText(
                detailRoot,
                "Identity",
                identity,
                new Vector2(pad + 108f, 64f),
                new Vector2(Mathf.Max(220f, panel.x - pad - 128f), 96f),
                13f,
                NexusTheme.Text);
            identityText.textWrappingMode = TextWrappingModes.Normal;
            identityText.overflowMode = TextOverflowModes.Truncate;
            identityText.richText = true;

            string statsBlock =
                $"<color=#{muted}>{UiText.DetailStats}</color>\n" +
                $"{UiText.StatHp}  {card.GetPanelHealth():N0}\n" +
                $"{UiText.StatAtk}  {card.GetPanelAttack():N0}\n" +
                $"{UiText.StatDef}  {card.GetPanelDefense():N0}\n" +
                $"{UiText.StatAcc}  {card.GetPanelAccuracy():N0}\n" +
                $"{UiText.StatDodge}  {card.GetPanelDodge():N0}\n" +
                $"{UiText.StatCrit}  {card.GetPanelCritical():N0}\n" +
                $"{UiText.StatCritDmg}  {card.GetPanelCritialDamage():N0}\n" +
                $"{UiText.StatDr}  {card.GetPanelDMGReduction():N0}\n" +
                $"{UiText.StatEnergy}  {card.GetPanelEnergyRate():N0}\n" +
                $"{UiText.StatSpeed}  {card.GetPanelSpeed():N0}";
            var statsBlockText = NexusUiFactory.CreateText(
                detailRoot,
                "CombatStats",
                statsBlock,
                new Vector2(x0, colTop),
                new Vector2(colW, colH),
                12f,
                NexusTheme.Text);
            statsBlockText.textWrappingMode = TextWrappingModes.Normal;
            statsBlockText.overflowMode = TextOverflowModes.Truncate;
            statsBlockText.richText = true;

            string extras = $"<color=#{muted}>{UiText.DetailSkills}</color>\n";
            if (card.skills == null || card.skills.Count == 0)
                extras += $"{UiText.None}\n";
            else
            {
                int shown = Mathf.Min(card.skills.Count, 4);
                for (int i = 0; i < shown; i++)
                {
                    var skill = card.skills[i];
                    if (skill == null) continue;
                    extras += $"{skill.GetSkillName()}  {UiText.TierShort(skill.skillTier.ToString())}\n";
                }
            }

            extras += $"\n<color=#{muted}>{UiText.DetailExpertise}</color>\n";
            if (card.expertises == null || card.expertises.Count == 0)
                extras += UiText.None;
            else
            {
                int shown = Mathf.Min(card.expertises.Count, 4);
                for (int i = 0; i < shown; i++)
                {
                    var exp = card.expertises[i];
                    if (exp == null) continue;
                    extras += $"{exp.attributeType}  {UiText.TierShort(exp.expertiseTier.ToString())}  +{exp.value:0.##}\n";
                }
            }

            var extrasText = NexusUiFactory.CreateText(
                detailRoot,
                "Skills",
                extras,
                new Vector2(x1, colTop),
                new Vector2(colW, colH),
                12f,
                NexusTheme.Text);
            extrasText.textWrappingMode = TextWrappingModes.Normal;
            extrasText.overflowMode = TextOverflowModes.Truncate;
            extrasText.richText = true;

            DrawEquippedGear(card, x2, colTop, colW);
            DrawDeckActions(canJoin, canLeave);
        }

        private void DrawEquippedGear(CardEntity card, float x, float y, float width)
        {
            NexusUiFactory.CreateText(
                detailRoot,
                "GearHeader",
                UiText.EquippedGear,
                new Vector2(x, y),
                new Vector2(width, 22f),
                13f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            var equipped = new Dictionary<EquipSlot, ItemEntity>();
            var items = ProductionService.GetLocalItems();
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item == null || item.equippedToCardId != card.id)
                        continue;
                    var def = ItemCatalog.Get(ItemFactory.ResolveDefId(item));
                    if (def == null || def.equipSlot == EquipSlot.None)
                        continue;
                    equipped[def.equipSlot] = item;
                }
            }

            float rowY = y + 26f;
            float rowH = 34f;
            foreach (var slot in GearSlots)
            {
                equipped.TryGetValue(slot, out var item);
                string line = item == null
                    ? $"{UiText.EquipSlotLabel(slot.ToString())}  ·  {UiText.NoGearInSlot}"
                    : $"{UiText.EquipSlotLabel(slot.ToString())}  ·  {GearDisplayName(item)}  {item.durability}/{item.maxDurability}";

                EquipSlot captured = slot;
                var row = NexusUiFactory.CreateButton(
                    detailRoot,
                    "Gear " + slot,
                    line,
                    new Vector2(x, rowY),
                    new Vector2(width, rowH),
                    () => OpenGearPicker(captured),
                    NexusTheme.SurfaceRaised,
                    item == null ? NexusTheme.DimText : NexusTheme.Text,
                    11f);
                var rowLabel = row.GetComponentInChildren<TextMeshProUGUI>();
                if (rowLabel != null)
                {
                    rowLabel.alignment = TextAlignmentOptions.Left;
                    rowLabel.fontStyle = FontStyles.Normal;
                    rowLabel.margin = new Vector4(8f, 0f, 8f, 0f);
                    rowLabel.overflowMode = TextOverflowModes.Ellipsis;
                }

                rowY += rowH + 6f;
            }
        }

        private static string GearDisplayName(ItemEntity item)
        {
            if (item == null) return "";
            var def = ItemCatalog.Get(ItemFactory.ResolveDefId(item));
            return def != null ? UiText.T(def.displayNameEn, def.displayNameZh) : item.itemName;
        }

        private void CloseGearPicker()
        {
            Transform overlay = root.Find("GearOverlay");
            if (overlay != null)
                UnityEngine.Object.DestroyImmediate(overlay.gameObject);
        }

        /// <summary>
        /// Modal for one equip slot: what the card wears now, plus every warehouse alternative laid
        /// out in a scrollable grid. Picking a tile swaps the gear and reopens on the same slot.
        /// </summary>
        private void OpenGearPicker(EquipSlot slot)
        {
            if (selectedCard == null)
            {
                statusText.text = UiText.SelectRosterFirst;
                return;
            }

            CloseGearPicker();
            CardEntity card = selectedCard;

            GameObject overlay = NexusUiFactory.CreatePanel(
                root,
                "GearOverlay",
                NexusTheme.WithAlpha(Color.black, 0.55f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                true);
            overlay.transform.SetAsLastSibling();
            var dismiss = overlay.AddComponent<Button>();
            dismiss.transition = Selectable.Transition.None;
            dismiss.onClick.AddListener(CloseGearPicker);

            const float dialogW = 760f;
            const float dialogH = 540f;
            GameObject dialog = NexusUiFactory.CreateBox(
                overlay.transform,
                "GearDialog",
                Vector2.zero,
                new Vector2(dialogW, dialogH),
                NexusTheme.Surface,
                NexusTheme.Gold);
            var dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = new Vector2(dialogW, dialogH);
            dialog.GetComponent<Image>().raycastTarget = true;

            NexusUiFactory.CreateText(
                dialog.transform,
                "Title",
                $"{UiText.GearDetail}  ·  {UiText.EquipSlotLabel(slot.ToString())}  ·  {DisplayName(card)}",
                new Vector2(20f, 14f),
                new Vector2(dialogW - 140f, 26f),
                16f,
                NexusTheme.Gold,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            NexusUiFactory.CreateButton(
                dialog.transform,
                "Close",
                UiText.Close,
                new Vector2(dialogW - 100f, 12f),
                new Vector2(80f, 28f),
                CloseGearPicker,
                NexusTheme.SurfaceRaised,
                NexusTheme.Text,
                12f);

            DrawGearPickerCurrent(dialog.transform, card, slot, dialogW);
            DrawGearPickerGrid(dialog.transform, card, slot, dialogW, dialogH);
        }

        private void DrawGearPickerCurrent(Transform dialog, CardEntity card, EquipSlot slot, float dialogW)
        {
            ItemEntity current = FindEquippedInSlot(card.id, slot);
            NexusUiFactory.CreateBox(
                dialog,
                "Current",
                new Vector2(20f, 50f),
                new Vector2(dialogW - 40f, 104f),
                NexusTheme.SurfaceRaised,
                NexusTheme.BorderSoft);

            if (current == null)
            {
                NexusUiFactory.CreateText(
                    dialog,
                    "CurrentEmpty",
                    UiText.NoGearInSlot,
                    new Vector2(36f, 88f),
                    new Vector2(dialogW - 72f, 24f),
                    13f,
                    NexusTheme.DimText);
                return;
            }

            NexusUiFactory.CreateText(
                dialog,
                "CurrentName",
                GearDisplayName(current),
                new Vector2(36f, 64f),
                new Vector2(dialogW - 220f, 26f),
                15f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            NexusUiFactory.CreateText(
                dialog,
                "CurrentStats",
                $"{UiText.GearQuality(current.quality)}   ·   " +
                $"{UiText.GearDurability(current.durability, current.maxDurability)}   ·   " +
                UiText.GearScore(GearScore(current)),
                new Vector2(36f, 94f),
                new Vector2(dialogW - 220f, 24f),
                12f,
                NexusTheme.MutedText);

            var gearDef = ItemCatalog.Get(ItemFactory.ResolveDefId(current));
            var gearDesc = gearDef != null ? UiText.ItemDescription(gearDef) : "";
            if (!string.IsNullOrEmpty(gearDesc))
            {
                NexusUiFactory.CreateText(
                    dialog,
                    "CurrentDesc",
                    gearDesc,
                    new Vector2(36f, 118f),
                    new Vector2(dialogW - 72f, 36f),
                    11f,
                    NexusTheme.DimText);
            }

            string instanceId = current.itemInstanceId;
            string itemName = GearDisplayName(current);
            NexusUiFactory.CreateButton(
                dialog,
                "Unequip",
                UiText.UnequipGear,
                new Vector2(dialogW - 160f, 70f),
                new Vector2(124f, 34f),
                () =>
                {
                    statusText.text = DurabilityService.TryUnequip(instanceId)
                        ? UiText.UnequippedItem(itemName)
                        : UiText.EquipFailed;
                    Rebuild();
                    OpenGearPicker(slot);
                },
                NexusTheme.WithAlpha(NexusTheme.Red, 0.16f),
                NexusTheme.Red,
                12f);
        }

        private void DrawGearPickerGrid(
            Transform dialog, CardEntity card, EquipSlot slot, float dialogW, float dialogH)
        {
            NexusUiFactory.CreateText(
                dialog,
                "GridHeader",
                UiText.GearReplacements,
                new Vector2(20f, 166f),
                new Vector2(dialogW - 40f, 22f),
                13f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            var candidates = CollectSlotCandidates(slot);
            candidates.RemoveAll(item => item.equippedToCardId == card.id);
            if (candidates.Count == 0)
            {
                NexusUiFactory.CreateText(
                    dialog,
                    "GridEmpty",
                    UiText.GearNoneAvailable,
                    new Vector2(24f, 200f),
                    new Vector2(dialogW - 48f, 24f),
                    12f,
                    NexusTheme.DimText);
                return;
            }

            float gridW = dialogW - 40f;
            float gridH = dialogH - 194f - 20f;
            GameObject scrollHost = NexusUiFactory.CreateBox(
                dialog,
                "GearGrid",
                new Vector2(20f, 194f),
                new Vector2(gridW, gridH),
                NexusTheme.WithAlpha(NexusTheme.Background, 0.55f),
                NexusTheme.BorderSoft);
            scrollHost.AddComponent<RectMask2D>();
            var scroll = scrollHost.AddComponent<ScrollRect>();

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(scrollHost.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            const int columns = 4;
            var grid = content.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.spacing = new Vector2(10f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2((gridW - 20f - (columns - 1) * 10f) / columns, 92f);

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
            scroll.viewport = scrollHost.GetComponent<RectTransform>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            string muted = ColorUtility.ToHtmlStringRGB(NexusTheme.MutedText);
            for (int i = 0; i < candidates.Count; i++)
            {
                ItemEntity item = candidates[i];
                string holder = DescribeGearHolder(item);
                string label =
                    $"<b>{Truncate(GearDisplayName(item), 16)}</b>\n" +
                    $"<color=#{muted}>{UiText.GearQuality(item.quality)} · " +
                    $"{UiText.GearDurability(item.durability, item.maxDurability)}</color>\n" +
                    $"{UiText.GearScore(GearScore(item))}" +
                    (string.IsNullOrEmpty(holder) ? "" : $"\n<color=#{muted}>{holder}</color>");

                string instanceId = item.itemInstanceId;
                string itemName = GearDisplayName(item);
                var cell = NexusUiFactory.CreateButton(
                    content.transform,
                    "Gear " + i,
                    label,
                    Vector2.zero,
                    grid.cellSize,
                    () =>
                    {
                        statusText.text = DurabilityService.TryEquip(instanceId, card.id)
                            ? UiText.EquippedItem(itemName)
                            : UiText.EquipFailed;
                        Rebuild();
                        OpenGearPicker(slot);
                    },
                    NexusTheme.SurfaceRaised,
                    NexusTheme.Text,
                    10f);
                var cellLabel = cell.GetComponentInChildren<TextMeshProUGUI>();
                if (cellLabel != null)
                {
                    cellLabel.alignment = TextAlignmentOptions.TopLeft;
                    cellLabel.fontStyle = FontStyles.Normal;
                    cellLabel.textWrappingMode = TextWrappingModes.NoWrap;
                    cellLabel.margin = new Vector4(8f, 6f, 6f, 4f);
                }
            }
        }

        /// <summary>Name of the character already wearing an item, or empty when it sits in the bag.</summary>
        private static string DescribeGearHolder(ItemEntity item)
        {
            if (item == null || string.IsNullOrEmpty(item.equippedToCardId))
                return "";
            var all = CardListManager.Instance?.GetCardEntities();
            if (all != null)
            {
                foreach (var entity in all)
                {
                    if (entity != null && entity.id == item.equippedToCardId)
                        return $"{UiText.GearEquippedBadge}: {Truncate(DisplayName(entity), 12)}";
                }
            }

            return UiText.GearEquippedBadge;
        }

        private void DrawDeckActions(bool canJoin, bool canLeave)
        {
            Vector2 panel = DetailSize();
            float pad = 20f;
            float gap = 10f;
            float btnW = Mathf.Max(140f, (panel.x - pad * 2f - gap * 2f) / 3f);
            float btnH = 40f;
            float bottom = 10f;

            var join = NexusUiFactory.CreateButton(
                detailRoot,
                "JoinDeck",
                UiText.JoinDeck,
                new Vector2(pad, 500f),
                new Vector2(btnW, btnH),
                () => TryJoinSelected(),
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.16f),
                NexusTheme.Cyan,
                14f);
            join.interactable = canJoin;
            PinBottomLeft(join, pad, bottom, new Vector2(btnW, btnH));
            TutorialGuideService.RegisterAnchor(
                "formation_join",
                join.GetComponent<RectTransform>(),
                join);

            var leave = NexusUiFactory.CreateButton(
                detailRoot,
                "LeaveDeck",
                UiText.LeaveDeck,
                new Vector2(pad, 500f),
                new Vector2(btnW, btnH),
                () => TryLeaveSelected(),
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f),
                NexusTheme.Gold,
                14f);
            leave.interactable = canLeave;
            PinBottomLeft(leave, pad + btnW + gap, bottom, new Vector2(btnW, btnH));

            var equip = NexusUiFactory.CreateButton(
                detailRoot,
                "Equip",
                UiText.AutoEquip,
                new Vector2(pad, 500f),
                new Vector2(btnW, btnH),
                () => AutoEquipSelected(),
                NexusTheme.WithAlpha(NexusTheme.Green, 0.16f),
                NexusTheme.Green,
                13f);
            equip.interactable = selectedCard != null;
            PinBottomLeft(equip, pad + (btnW + gap) * 2f, bottom, new Vector2(btnW, btnH));
        }

        private bool CanJoinSelected(
            DeckEntity editing, CardEntity card, bool inCurrent, bool inOther, out string reason)
        {
            reason = "";
            if (card == null)
            {
                reason = UiText.SelectRosterFirst;
                return false;
            }

            if (editing == null || !editing.unlocked)
            {
                reason = UiText.DeckLocked;
                return false;
            }

            if (editing.IsActionBusy)
            {
                reason = UiText.DeckBusyHint;
                return false;
            }

            if (inCurrent || inOther)
            {
                reason = UiText.RosterAlreadyAssignedHint(editing.displayName);
                return false;
            }

            if (!IsSelectedSlotEmpty(editing))
            {
                reason = UiText.JoinNeedsEmptySlot;
                return false;
            }

            return true;
        }

        private bool IsSelectedSlotEmpty(DeckEntity editing) => IsSlotEmpty(editing, selectedSlot);

        private static bool IsSlotEmpty(DeckEntity editing, int slot)
        {
            if (editing?.slotCardIds == null)
                return false;
            if (slot < 0 || slot >= editing.slotCardIds.Length)
                return false;
            return string.IsNullOrEmpty(editing.slotCardIds[slot]);
        }

        private static bool CanLeaveSelected(DeckEntity editing, bool inCurrent)
        {
            return editing != null && editing.unlocked && !editing.IsActionBusy && inCurrent;
        }

        private static int FirstEmptySlot(DeckEntity deck)
        {
            if (deck?.slotCardIds == null)
                return -1;
            for (int i = 0; i < deck.slotCardIds.Length; i++)
            {
                if (string.IsNullOrEmpty(deck.slotCardIds[i]))
                    return i;
            }

            return -1;
        }

        private static int IndexInEditingDeck(DeckEntity deck, string cardId)
        {
            if (deck?.slotCardIds == null || string.IsNullOrEmpty(cardId))
                return -1;
            for (int i = 0; i < deck.slotCardIds.Length; i++)
            {
                if (deck.slotCardIds[i] == cardId)
                    return i;
            }

            return -1;
        }

        private void BuildSlots(List<CardEntity> all)
        {
            var editing = DeckService.GetEditingDeck();
            var barHeader = slotsRoot.parent != null
                ? slotsRoot.parent.Find("Header")?.GetComponent<TextMeshProUGUI>()
                : null;
            if (barHeader != null)
            {
                barHeader.text = editing == null
                    ? UiText.DeckLineup
                    : $"{editing.displayName}  ·  {MemberCountLabel(editing)}";
            }

            string[] labels = UiText.FormationSlotLabels;
            const float slotW = 148f;
            const float gap = 8f;
            bool registeredEmptySlot = false;
            for (int i = 0; i < DeckConstants.SlotsPerDeck; i++)
            {
                float sx = i * (slotW + gap);
                CardEntity occupant = editing != null
                    ? DeckService.FindMemberInSlot(editing.deckId, i, all)
                    : null;
                int slotIndex = i;
                bool selected = selectedSlot == i
                    || (occupant != null && selectedCard != null && selectedCard.id == occupant.id);
                string title = occupant != null
                    ? DisplayName(occupant)
                    : UiText.AddUnit;

                GameObject slot = NexusUiFactory.CreateBox(
                    slotsRoot,
                    $"Slot {i}",
                    new Vector2(sx, 0f),
                    new Vector2(slotW, 140f),
                    occupant != null
                        ? NexusTheme.WithAlpha(NexusTheme.Cyan, 0.10f)
                        : NexusTheme.SurfaceRaised,
                    selected ? NexusTheme.Gold : NexusTheme.BorderSoft);

                if (occupant != null)
                {
                    NexusUiFactory.CreateIcon(
                        slot.transform,
                        "Portrait",
                        NexusCardVisual.CharacterSprite(occupant),
                        new Vector2(24f, 22f),
                        new Vector2(100f, 78f),
                        Color.white);
                }

                NexusUiFactory.CreateText(
                    slot.transform, "Pos", labels[Mathf.Min(i, labels.Length - 1)],
                    new Vector2(8f, 4f), new Vector2(slotW - 16f, 16f), 10f, NexusTheme.MutedText);
                NexusUiFactory.CreateText(
                    slot.transform, "Name", Truncate(title, 12),
                    new Vector2(8f, 104f), new Vector2(slotW - 16f, 18f), 12f,
                    occupant != null ? NexusTheme.Text : NexusTheme.DimText,
                    TextAlignmentOptions.Left, FontStyles.Bold);
                if (occupant != null)
                {
                    NexusUiFactory.CreateText(
                        slot.transform, "Meta",
                        $"Lv.{occupant.Level}  {UiText.TierShort(occupant.CharacterTier.ToString())}",
                        new Vector2(8f, 120f), new Vector2(slotW - 16f, 16f), 10f, NexusTheme.Cyan);
                }

                var image = slot.GetComponent<Image>();
                image.raycastTarget = true;
                var slotButton = slot.AddComponent<Button>();
                slotButton.targetGraphic = image;
                slotButton.onClick.AddListener(() =>
                {
                    selectedSlot = slotIndex;
                    if (occupant != null)
                        selectedCard = occupant;
                    Rebuild();
                });

                if (!registeredEmptySlot && occupant == null)
                {
                    TutorialGuideService.RegisterAnchor(
                        "formation_empty_slot",
                        slot.GetComponent<RectTransform>(),
                        slotButton);
                    registeredEmptySlot = true;
                }
            }
        }

        private void TryJoinSelected()
        {
            var editing = DeckService.GetEditingDeck();
            var membership = BuildDeckMembership();
            bool inCurrent = editing != null && IndexInEditingDeck(editing, selectedCard?.id) >= 0;
            bool inOther = selectedCard != null
                && membership.TryGetValue(selectedCard.id, out var names)
                && names.Count > 0
                && !inCurrent;
            if (!CanJoinSelected(editing, selectedCard, inCurrent, inOther, out string reason))
            {
                statusText.text = reason;
                NexusSnackbar.Show(reason);
                return;
            }

            int target = selectedSlot;
            List<CardEntity> all = CardListManager.Instance?.GetCardEntities();
            if (all == null) return;
            var assign = DeckService.TryAssignToEditing(target, selectedCard.id, all);
            if (!assign.Success)
            {
                statusText.text = UiText.DeckCommandMessage(assign);
                NexusSnackbar.Show(assign);
            }
            else
            {
                statusText.text = string.Empty;
            }

            if (assign.Success)
            {
                selectedCard = null;
                var refreshed = DeckService.GetEditingDeck();
                selectedSlot = FirstEmptySlot(refreshed);
                Persist();
            }
            Rebuild();
        }

        private void TryLeaveSelected()
        {
            var editing = DeckService.GetEditingDeck();
            bool inCurrent = editing != null && IndexInEditingDeck(editing, selectedCard?.id) >= 0;
            if (!CanLeaveSelected(editing, inCurrent))
            {
                statusText.text = editing != null && editing.IsActionBusy
                    ? UiText.DeckBusyHint
                    : UiText.InspectHint;
                return;
            }

            int index = IndexInEditingDeck(editing, selectedCard.id);
            ClearSlot(index);
        }

        private void ClearSlot(int index)
        {
            List<CardEntity> all = CardListManager.Instance?.GetCardEntities();
            var editing = DeckService.GetEditingDeck();
            if (editing == null || all == null) return;
            if (editing.IsActionBusy)
            {
                statusText.text = UiText.DeckBusyHint;
                return;
            }

            var result = DeckService.TryAssignToEditing(index, "", all);
            if (!result.Success)
                statusText.text = result.Message;
            else
            {
                selectedCard = null;
                selectedSlot = index;
            }
            Persist();
            Rebuild();
        }

        private void SetAsCombatDeck()
        {
            var editing = DeckService.GetEditingDeck();
            if (editing == null) return;
            List<CardEntity> all = CardListManager.Instance?.GetCardEntities();
            bool swapped = editing.deckId != FirstUnlockedDeckId();
            var result = DeckService.TryPromoteToCombatSlot(editing.deckId, all);
            if (result.Success)
                statusText.text = swapped ? UiText.DeckPromotedToCombat : UiText.ActiveCombatBadge;
            else
                statusText.text = result.Error == DeckCommandError.DeckBusy ? UiText.DeckSwapBusy : result.Message;
            Persist();
            Rebuild();
        }

        private static string FirstUnlockedDeckId()
        {
            foreach (var deck in DeckService.GetDecks())
            {
                if (deck != null && deck.unlocked)
                    return deck.deckId;
            }

            return "";
        }

        private void CycleStrategy()
        {
            var editing = DeckService.GetEditingDeck();
            if (editing == null) return;
            var current = CombatStrategyRules.Parse(editing.combatStrategyId);
            var next = (CombatStrategyId)(((int)current + 1) % 4);
            DeckService.TrySetCombatStrategy(editing.deckId, next.ToString());
            statusText.text = $"{UiText.CombatStrategyLabel}: {next}";
            Rebuild();
        }

        /// <summary>
        /// Fills every equip slot on the selected card with the highest-scoring free item in the
        /// warehouse, leaving a slot untouched when nothing beats what is already worn.
        /// </summary>
        private void AutoEquipSelected()
        {
            if (selectedCard == null)
            {
                statusText.text = UiText.SelectRosterFirst;
                return;
            }

            int equipped = 0;
            foreach (var slot in GearSlots)
            {
                ItemEntity current = FindEquippedInSlot(selectedCard.id, slot);
                int bestScore = current != null ? GearScore(current) : int.MinValue;
                ItemEntity best = null;

                foreach (var candidate in CollectSlotCandidates(slot))
                {
                    if (!string.IsNullOrEmpty(candidate.equippedToCardId)) continue;
                    int score = GearScore(candidate);
                    if (score <= bestScore) continue;
                    bestScore = score;
                    best = candidate;
                }

                if (best != null && DurabilityService.TryEquip(best.itemInstanceId, selectedCard.id))
                    equipped++;
            }

            statusText.text = equipped > 0 ? UiText.AutoEquipDone(equipped) : UiText.AutoEquipNone;
            Rebuild();
        }

        /// <summary>Ranks gear by quality first, then base value, then remaining durability.</summary>
        private static int GearScore(ItemEntity item)
        {
            if (item == null) return 0;
            var def = ItemCatalog.Get(ItemFactory.ResolveDefId(item));
            int baseCost = def?.baseCost ?? item.itemCost;
            return Mathf.Max(1, item.quality) * 1000 + baseCost * 2 + item.durability;
        }

        private static ItemEntity FindEquippedInSlot(string cardId, EquipSlot slot)
        {
            var items = ProductionService.GetLocalItems();
            if (items == null || string.IsNullOrEmpty(cardId)) return null;
            foreach (var item in items)
            {
                if (item == null || item.equippedToCardId != cardId) continue;
                var def = ItemCatalog.Get(ItemFactory.ResolveDefId(item));
                if (def != null && def.equipSlot == slot)
                    return item;
            }

            return null;
        }

        /// <summary>Warehouse equipment for a slot, excluding broken pieces, best score first.</summary>
        private static List<ItemEntity> CollectSlotCandidates(EquipSlot slot)
        {
            var result = new List<ItemEntity>();
            var items = ProductionService.GetLocalItems();
            if (items == null) return result;

            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.itemInstanceId)) continue;
                var def = ItemCatalog.Get(ItemFactory.ResolveDefId(item));
                if (def == null || def.category != ItemCategory.Equipment || def.equipSlot != slot) continue;
                if (DurabilityRules.IsBroken(item.durability)) continue;
                result.Add(item);
            }

            result.Sort((a, b) => GearScore(b).CompareTo(GearScore(a)));
            return result;
        }

        private void TryStopEditingDeck()
        {
            var editing = DeckService.GetEditingDeck();
            if (editing == null || !editing.IsActionBusy)
            {
                statusText.text = string.Empty;
                pendingStopDeckId = "";
                return;
            }

            if (pendingStopDeckId != editing.deckId)
            {
                pendingStopDeckId = editing.deckId;
                statusText.text = UiText.StopActionConfirm;
                return;
            }

            var stop = DeckService.TryStop(editing.deckId);
            pendingStopDeckId = "";
            statusText.text = stop.Success
                ? (stop.Settlement != null && stop.Settlement.DiscardedUnsettledProgress
                    ? UiText.StopActionConfirm
                    : UiText.ActionStopped)
                : stop.Message;
            Rebuild();
        }

        private void Persist()
        {
            if (CardListManager.Instance == null) return;
            if (DeckService.IsLoaded)
                DeckService.Save();
            DataUtil.Instance?.SaveCardData(CardListManager.Instance.GetCardEntities());
            CardListManager.Instance.UpdateCardList(CardListManager.Instance.GetCardEntities());
        }

        private void RefreshStats(List<CardEntity> all)
        {
            var editing = DeckService.GetEditingDeck();
            int count = editing?.MemberCount ?? 0;
            int power = 0;
            if (editing != null)
            {
                foreach (var member in DeckService.GetOrderedMembers(editing.deckId, all))
                {
                    if (member != null)
                        power += Mathf.RoundToInt(member.power);
                }
            }

            string gold = ColorUtility.ToHtmlStringRGB(NexusTheme.Gold);
            string cyan = ColorUtility.ToHtmlStringRGB(NexusTheme.Cyan);
            string muted = ColorUtility.ToHtmlStringRGB(NexusTheme.MutedText);
            string actionLabel = editing == null
                ? ""
                : UiText.DeckActionLabel(
                    editing.action?.status.ToString() ?? "Idle",
                    editing.action?.actionType.ToString() ?? "None");
            bool isCombat = editing != null && editing.deckId == DeckService.GetActiveCombatDeck()?.deckId;
            string strategy = editing == null
                ? ""
                : CombatStrategyRules.Parse(editing.combatStrategyId).ToString();

            statsText.text =
                UiText.FormationTeamSummary + "\n\n" +
                $"<color=#{muted}>{UiText.SelectedDeckLabel}</color>\n" +
                $"<size=16><color=#{gold}>{Truncate(editing?.displayName ?? "-", 20)}</color></size>\n" +
                $"<color=#{cyan}>{UiText.DeckPurposeLabel(editing?.purpose.ToString() ?? "Flexible")}" +
                (isCombat ? $" · {UiText.ActiveCombatBadge}" : "") + "</color>\n" +
                $"{actionLabel}\n" +
                $"{UiText.CombatStrategyLabel}: {strategy}\n\n" +
                $"<color=#{muted}>{UiText.InFormationLabel}</color>\n" +
                $"<size=18><color=#{gold}>{count} / {DeckConstants.SlotsPerDeck}</color></size>\n\n" +
                $"<color=#{muted}>{UiText.TotalPowerLabel}</color>\n" +
                $"<size=18><color=#{cyan}>{power:N0}</color></size>\n\n" +
                $"<color=#{muted}>{UiText.ParallelOps(DeckService.CountBusyDecks(), DeckService.MaxParallelActions)}</color>";
            statsText.overflowMode = TextOverflowModes.Truncate;
        }

        private static string MemberCountLabel(DeckEntity deck) =>
            $"{deck.MemberCount}/{DeckConstants.SlotsPerDeck}";

        private void DrawOnboardingBanner()
        {
            Vector2 panel = DetailSize();
            OnboardingBanner.TryDraw(
                detailRoot,
                AppScreen.Formation,
                new Vector2(20f, 34f),
                new Vector2(Mathf.Max(240f, panel.x - 40f), 22f));
        }

        private Vector2 DetailSize()
        {
            Canvas.ForceUpdateCanvases();
            var rect = detailRoot as RectTransform;
            if (rect == null)
                return new Vector2(900f, 560f);
            var size = rect.rect.size;
            return new Vector2(Mathf.Max(size.x, 640f), Mathf.Max(size.y, 360f));
        }

        private static void PinBottomLeft(Component target, float x, float bottom, Vector2 size)
        {
            if (target == null)
                return;
            var rect = target.transform as RectTransform;
            if (rect == null)
                return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(x, bottom);
            rect.sizeDelta = size;
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
                return value ?? "";
            return value.Substring(0, max - 1) + "…";
        }

        private static void ClearChildren(Transform parent, bool keepHeader)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (keepHeader && (child.name == "Header" || child.name.StartsWith("Header")))
                    continue;
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }
    }
}
