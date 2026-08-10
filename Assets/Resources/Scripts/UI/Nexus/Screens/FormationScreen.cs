using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Formation screen rebuilt from Figma Make Formation.tsx.
    /// Writes lineup positions on CardEntity and persists through CardListManager.
    /// Does not depend on the legacy BattlePanel PortraitSlot hierarchy.
    /// </summary>
    internal sealed class FormationScreen
    {
        private readonly Transform root;
        private readonly Transform rosterRoot;
        private readonly Transform slotsRoot;
        private readonly TextMeshProUGUI statsText;
        private int selectedSlot = -1;
        private CardEntity selectedCard;

        private FormationScreen(Transform root, Transform rosterRoot, Transform slotsRoot, TextMeshProUGUI statsText)
        {
            this.root = root;
            this.rosterRoot = rosterRoot;
            this.slotsRoot = slotsRoot;
            this.statsText = statsText;
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

            GameObject roster = NexusUiFactory.CreatePanel(
                root.transform,
                "Roster",
                NexusTheme.Surface,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                Vector2.zero,
                new Vector2(280f, 0f),
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
                new Vector2(-280f, 0f));
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
            slotsRect.anchoredPosition = Vector2.zero;

            GameObject stats = NexusUiFactory.CreatePanel(
                root.transform,
                "Stats",
                NexusTheme.Surface,
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(-280f, 0f),
                Vector2.zero,
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
                new Vector2(248f, 280f),
                13f,
                NexusTheme.Text);

            var screen = new FormationScreen(root.transform, roster.transform, slotsHost.transform, statsText);
            screen.Rebuild();

            NexusUiFactory.CreateButton(
                stats.transform,
                "Save",
                UiText.SaveFormation,
                new Vector2(16f, 920f),
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
            ClearChildren(rosterRoot, keepHeader: true);
            ClearChildren(slotsRoot, keepHeader: false);

            List<CardEntity> all = CardListManager.Instance?.GetCardEntities() ?? new List<CardEntity>();
            if (DataUtil.Instance != null)
                DeckService.EnsureLoaded(DataUtil.Instance, all);
            float y = 56f;
            foreach (CardEntity entity in all)
            {
                if (entity == null) continue;
                bool inLine = entity.GetLineupPosition() != LineupPosition.None;
                string label = inLine
                    ? $"{entity.cardName}  [S{(int)entity.GetLineupPosition()}]"
                    : (string.IsNullOrEmpty(entity.cardName) ? entity.characterName.ToString() : entity.cardName);
                CardEntity captured = entity;

                GameObject row = NexusUiFactory.CreateBox(
                    rosterRoot,
                    $"Roster {entity.id}",
                    new Vector2(12f, y),
                    new Vector2(256f, 56f),
                    inLine ? NexusTheme.WithAlpha(NexusTheme.Cyan, 0.12f) : NexusTheme.SurfaceRaised,
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
                Sprite badge = NexusCardVisual.TierBadgeSprite(entity);
                if (badge != null)
                {
                    NexusUiFactory.CreateIcon(
                        row.transform,
                        "Badge",
                        badge,
                        new Vector2(220f, 16f),
                        new Vector2(24f, 24f),
                        Color.white);
                }
                NexusUiFactory.CreateText(
                    row.transform,
                    "Label",
                    label,
                    new Vector2(58f, 10f),
                    new Vector2(160f, 36f),
                    12f,
                    inLine ? NexusTheme.Cyan : NexusTheme.Text,
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

            string[] labels = UiText.FormationSlotLabels;
            int columns = 3;
            for (int i = 0; i < DefaultProperty.defaultLineupSize; i++)
            {
                int col = i % columns;
                int row = i / columns;
                float sx = col * 170f;
                float sy = row * 180f;
                CardEntity occupant = FindInSlot(all, i);
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
                    Sprite badge = NexusCardVisual.TierBadgeSprite(occupant);
                    if (badge != null)
                    {
                        NexusUiFactory.CreateIcon(
                            slot.transform,
                            "Badge",
                            badge,
                            new Vector2(110f, 12f),
                            new Vector2(28f, 28f),
                            Color.white);
                    }
                }

                NexusUiFactory.CreateText(slot.transform, "Pos", labels[Mathf.Min(i, labels.Length - 1)], new Vector2(10f, 8f), new Vector2(130f, 18f), 11f, NexusTheme.MutedText);
                NexusUiFactory.CreateText(slot.transform, "Name", title, new Vector2(10f, 118f), new Vector2(130f, 22f), 13f, NexusTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
                NexusUiFactory.CreateText(slot.transform, "Meta", meta, new Vector2(10f, 138f), new Vector2(130f, 18f), 11f, NexusTheme.Cyan);

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

            RefreshStats(all);
        }

        private void TryAssignSelected()
        {
            if (selectedCard == null || selectedSlot < 0)
                return;

            List<CardEntity> all = CardListManager.Instance?.GetCardEntities();
            if (all == null) return;

            CardEntity previous = FindInSlot(all, selectedSlot);
            if (previous != null && previous.id != selectedCard.id)
                previous.SetLineupPosition(LineupPosition.None);

            if (selectedCard.GetLineupPosition() != LineupPosition.None &&
                (int)selectedCard.GetLineupPosition() != selectedSlot)
            {
                selectedCard.SetLineupPosition(LineupPosition.None);
            }

            selectedCard.SetLineupPosition((LineupPosition)selectedSlot);
            if (DeckService.IsLoaded)
            {
                var assign = DeckService.TryAssignToActiveCombat(selectedSlot, selectedCard.id, all);
                if (!assign.Success)
                    Debug.LogWarning("[DECK] Formation assign failed: " + assign.Message);
            }

            Persist();
            selectedCard = null;
            Rebuild();
        }

        private void ClearSlot(int index)
        {
            List<CardEntity> all = CardListManager.Instance?.GetCardEntities();
            CardEntity occupant = FindInSlot(all, index);
            if (occupant == null) return;
            occupant.SetLineupPosition(LineupPosition.None);
            if (DeckService.IsLoaded)
                DeckService.TryAssignToActiveCombat(index, "", all);
            Persist();
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
            int count = 0;
            int power = 0;
            foreach (CardEntity entity in all)
            {
                if (entity == null || entity.GetLineupPosition() == LineupPosition.None) continue;
                count++;
                power += Mathf.RoundToInt(entity.power);
            }

            string gold = ColorUtility.ToHtmlStringRGB(NexusTheme.Gold);
            string cyan = ColorUtility.ToHtmlStringRGB(NexusTheme.Cyan);
            statsText.text =
                $"<color=#{ColorUtility.ToHtmlStringRGB(NexusTheme.MutedText)}>{UiText.T("In formation", "上阵人数")}</color>\n" +
                $"<size=18><color=#{gold}>{count} / {DefaultProperty.defaultLineupSize}</color></size>\n\n" +
                $"<color=#{ColorUtility.ToHtmlStringRGB(NexusTheme.MutedText)}>{UiText.T("Total power", "综合战力")}</color>\n" +
                $"<size=18><color=#{cyan}>{power:N0}</color></size>\n\n" +
                UiText.FormationHint;
            statsText.textWrappingMode = TextWrappingModes.Normal;
        }

        private static CardEntity FindInSlot(List<CardEntity> all, int index)
        {
            if (all == null) return null;
            foreach (CardEntity entity in all)
            {
                if (entity != null && (int)entity.GetLineupPosition() == index)
                    return entity;
            }

            return null;
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
