using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils;
using TMPro;
using UnityEngine;
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
        private readonly TextMeshProUGUI statsText;
        private readonly TextMeshProUGUI statusText;
        private int selectedSlot = -1;
        private CardEntity selectedCard;
        private string pendingStopDeckId = "";

        private FormationScreen(
            Transform root,
            Transform deckTabsRoot,
            Transform rosterRoot,
            Transform slotsRoot,
            TextMeshProUGUI statsText,
            TextMeshProUGUI statusText)
        {
            this.root = root;
            this.deckTabsRoot = deckTabsRoot;
            this.rosterRoot = rosterRoot;
            this.slotsRoot = slotsRoot;
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
                new Vector2(0f, -96f),
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
                new Vector2(280f, -96f),
                true);
            NexusUiFactory.CreateText(
                roster.transform,
                "Header",
                UiText.AvailableCharacters,
                new Vector2(16f, 14f),
                new Vector2(240f, 28f),
                14f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            GameObject center = NexusUiFactory.CreatePanel(
                root.transform,
                "Grid",
                NexusTheme.Background,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(280f, 0f),
                new Vector2(-280f, -96f));
            NexusUiFactory.CreateText(
                center.transform,
                "Title",
                UiText.FormationTitle,
                new Vector2(24f, 18f),
                new Vector2(420f, 32f),
                20f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            GameObject slotsHost = new GameObject("Slots", typeof(RectTransform));
            slotsHost.transform.SetParent(center.transform, false);
            var slotsRect = slotsHost.GetComponent<RectTransform>();
            slotsRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotsRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotsRect.pivot = new Vector2(0.5f, 0.5f);
            slotsRect.sizeDelta = new Vector2(520f, 380f);
            slotsRect.anchoredPosition = new Vector2(0f, -20f);

            GameObject stats = NexusUiFactory.CreatePanel(
                root.transform,
                "Stats",
                NexusTheme.Surface,
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(-280f, 0f),
                new Vector2(0f, -96f),
                true);
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
                new Vector2(16f, 56f),
                new Vector2(248f, 220f),
                13f,
                NexusTheme.Text);
            var statusText = NexusUiFactory.CreateText(
                stats.transform,
                "Status",
                string.Empty,
                new Vector2(16f, 300f),
                new Vector2(248f, 80f),
                12f,
                NexusTheme.Gold);
            statusText.textWrappingMode = TextWrappingModes.Normal;

            var screen = new FormationScreen(
                root.transform,
                deckBar.transform,
                roster.transform,
                slotsHost.transform,
                statsText,
                statusText);
            screen.Rebuild();

            NexusUiFactory.CreateButton(
                stats.transform,
                "SetCombat",
                UiText.SetCombatDeck,
                new Vector2(16f, 760f),
                new Vector2(248f, 40f),
                () => screen.SetAsCombatDeck(),
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.16f),
                NexusTheme.Cyan,
                13f);
            NexusUiFactory.CreateButton(
                stats.transform,
                "Stop",
                UiText.StopAction,
                new Vector2(16f, 810f),
                new Vector2(248f, 40f),
                () => screen.TryStopEditingDeck(),
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.12f),
                NexusTheme.Gold,
                13f);
            NexusUiFactory.CreateButton(
                stats.transform,
                "Save",
                UiText.SaveFormation,
                new Vector2(16f, 860f),
                new Vector2(248f, 44f),
                () => screen.Persist(),
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f),
                NexusTheme.Gold,
                14f);

            return screen;
        }

        public GameObject Root => root.gameObject;

        public void Rebuild()
        {
            ClearChildren(deckTabsRoot, keepHeader: true);
            ClearChildren(rosterRoot, keepHeader: true);
            ClearChildren(slotsRoot, keepHeader: false);

            List<CardEntity> all = CardListManager.Instance?.GetCardEntities() ?? new List<CardEntity>();
            if (DataUtil.Instance != null)
                DeckService.EnsureLoaded(DataUtil.Instance, all);

            BuildDeckTabs();
            BuildRoster(all);
            BuildSlots(all);
            RefreshStats(all);
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

            float y = 56f;
            foreach (CardEntity entity in all)
            {
                if (entity == null) continue;
                bool inDeck = memberIds.Contains(entity.id);
                var occ = DeckService.GetOccupation(entity.id);
                string occLabel = UiText.OccupationBadge(occ.ToString());
                string baseName = string.IsNullOrEmpty(entity.cardName)
                    ? entity.characterName.ToString()
                    : entity.cardName;
                string label = inDeck ? $"{baseName}  [in]" : baseName;
                if (!string.IsNullOrEmpty(occLabel))
                    label += $" · {occLabel}";

                CardEntity captured = entity;
                GameObject row = NexusUiFactory.CreateBox(
                    rosterRoot,
                    $"Roster {entity.id}",
                    new Vector2(12f, y),
                    new Vector2(256f, 56f),
                    inDeck ? NexusTheme.WithAlpha(NexusTheme.Cyan, 0.12f) : NexusTheme.SurfaceRaised,
                    NexusTheme.BorderSoft);
                var rowImage = row.GetComponent<Image>();
                rowImage.raycastTarget = true;
                NexusUiFactory.CreateIcon(
                    row.transform,
                    "Portrait",
                    NexusCardVisual.CharacterSprite(entity),
                    new Vector2(6f, 6f),
                    new Vector2(44f, 44f),
                    Color.white);
                NexusUiFactory.CreateText(
                    row.transform,
                    "Label",
                    label,
                    new Vector2(58f, 8f),
                    new Vector2(186f, 40f),
                    11f,
                    inDeck ? NexusTheme.Cyan : NexusTheme.Text,
                    TextAlignmentOptions.Left,
                    FontStyles.Bold);
                var button = row.AddComponent<Button>();
                button.targetGraphic = rowImage;
                button.onClick.AddListener(() =>
                {
                    selectedCard = captured;
                    TryAssignSelected();
                });
                y += 64f;
            }
        }

        private void BuildSlots(List<CardEntity> all)
        {
            var editing = DeckService.GetEditingDeck();
            string[] labels = UiText.FormationSlotLabels;
            const int columns = 3;
            for (int i = 0; i < DeckConstants.SlotsPerDeck; i++)
            {
                int col = i % columns;
                int row = i / columns;
                float sx = col * 170f;
                float sy = row * 180f;
                CardEntity occupant = editing != null
                    ? DeckService.FindMemberInSlot(editing.deckId, i, all)
                    : null;
                int slotIndex = i;
                string title = occupant != null
                    ? (string.IsNullOrEmpty(occupant.cardName) ? occupant.characterName.ToString() : occupant.cardName)
                    : UiText.AddUnit;
                string meta = occupant != null
                    ? $"{occupant.characterName} · Lv.{occupant.Level}"
                    : labels[Mathf.Min(i, labels.Length - 1)];

                GameObject slot = NexusUiFactory.CreateBox(
                    slotsRoot,
                    $"Slot {i}",
                    new Vector2(sx, sy),
                    new Vector2(150f, 160f),
                    NexusTheme.SurfaceRaised,
                    selectedSlot == i ? NexusTheme.Gold : NexusTheme.BorderSoft);

                if (occupant != null)
                {
                    NexusUiFactory.CreateIcon(
                        slot.transform,
                        "Portrait",
                        NexusCardVisual.CharacterSprite(occupant),
                        new Vector2(25f, 28f),
                        new Vector2(100f, 90f),
                        Color.white);
                }

                NexusUiFactory.CreateText(
                    slot.transform, "Pos", labels[Mathf.Min(i, labels.Length - 1)],
                    new Vector2(10f, 8f), new Vector2(130f, 18f), 11f, NexusTheme.MutedText);
                NexusUiFactory.CreateText(
                    slot.transform, "Name", title,
                    new Vector2(10f, 118f), new Vector2(130f, 22f), 13f, NexusTheme.Text,
                    TextAlignmentOptions.Left, FontStyles.Bold);
                NexusUiFactory.CreateText(
                    slot.transform, "Meta", meta,
                    new Vector2(10f, 138f), new Vector2(130f, 18f), 11f, NexusTheme.Cyan);

                var image = slot.GetComponent<Image>();
                image.raycastTarget = true;
                var slotButton = slot.AddComponent<Button>();
                slotButton.targetGraphic = image;
                slotButton.onClick.AddListener(() =>
                {
                    selectedSlot = slotIndex;
                    if (selectedCard != null)
                        TryAssignSelected();
                    else if (occupant != null)
                        ClearSlot(slotIndex);
                    else
                        Rebuild();
                });
            }
        }

        private void TryAssignSelected()
        {
            if (selectedCard == null || selectedSlot < 0)
                return;

            List<CardEntity> all = CardListManager.Instance?.GetCardEntities();
            if (all == null) return;

            var editing = DeckService.GetEditingDeck();
            if (editing == null)
                return;
            if (!editing.unlocked)
            {
                statusText.text = UiText.DeckLocked;
                return;
            }

            if (editing.IsActionBusy)
            {
                statusText.text = UiText.DeckBusyHint;
                selectedCard = null;
                Rebuild();
                return;
            }

            var assign = DeckService.TryAssignToEditing(selectedSlot, selectedCard.id, all);
            if (!assign.Success)
                statusText.text = assign.Message;
            else
                statusText.text = string.Empty;

            Persist();
            selectedCard = null;
            Rebuild();
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
            Persist();
            Rebuild();
        }

        private void SetAsCombatDeck()
        {
            var editing = DeckService.GetEditingDeck();
            if (editing == null) return;
            List<CardEntity> all = CardListManager.Instance?.GetCardEntities();
            var result = DeckService.TrySetActiveCombatDeck(editing.deckId, all);
            statusText.text = result.Success
                ? UiText.ActiveCombatBadge
                : result.Message;
            Persist();
            Rebuild();
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
                    : UiText.T("Action stopped.", "行动已停止。"))
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

            statsText.text =
                $"<color=#{muted}>{UiText.T("Selected deck", "当前卡组")}</color>\n" +
                $"<size=16><color=#{gold}>{Truncate(editing?.displayName ?? "-", 20)}</color></size>\n" +
                $"<color=#{cyan}>{UiText.DeckPurposeLabel(editing?.purpose.ToString() ?? "Flexible")}" +
                (isCombat ? $" · {UiText.ActiveCombatBadge}" : "") + "</color>\n" +
                $"{actionLabel}\n\n" +
                $"<color=#{muted}>{UiText.T("In formation", "上阵人数")}</color>\n" +
                $"<size=18><color=#{gold}>{count} / {DeckConstants.SlotsPerDeck}</color></size>\n\n" +
                $"<color=#{muted}>{UiText.T("Total power", "综合战力")}</color>\n" +
                $"<size=18><color=#{cyan}>{power:N0}</color></size>\n\n" +
                $"<color=#{muted}>{UiText.ParallelOps(DeckService.CountBusyDecks(), DeckService.MaxParallelActions)}</color>\n\n" +
                UiText.FormationHint;
            statsText.textWrappingMode = TextWrappingModes.Normal;
        }

        private static string MemberCountLabel(DeckEntity deck) =>
            $"{deck.MemberCount}/{DeckConstants.SlotsPerDeck}";

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
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }
}
