using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Inventory;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.DebugTools;
using Assets.Resources.Scripts.Utils.Save;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Debug Mode screen: edit local inventory quantities and add/edit player cards.
    /// Only reachable when <see cref="DebugModeController.IsEnabled"/>.
    /// </summary>
    internal static class DebugScreen
    {
        private static readonly string[] QuickItems =
            { "Copper", "Steel", "GoldBar", "SteelBar", "Water", "Wood" };

        public static GameObject Build(Transform parent)
        {
            GameObject root = NexusUiFactory.CreatePanel(
                parent,
                "Debug Screen",
                NexusTheme.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            NexusUiFactory.CreateText(
                root.transform,
                "Title",
                UiText.DebugTitle,
                new Vector2(28f, 24f),
                new Vector2(640f, 40f),
                22f,
                NexusTheme.Gold,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            NexusUiFactory.CreateText(
                root.transform,
                "Hint",
                UiText.DebugHint,
                new Vector2(28f, 68f),
                new Vector2(1100f, 36f),
                13f,
                NexusTheme.MutedText);

            BuildItemsSection(root.transform);
            BuildCardsSection(root.transform);

            NexusUiFactory.CreateButton(
                root.transform,
                "Disable Debug",
                UiText.DebugDisable,
                new Vector2(28f, 620f),
                new Vector2(280f, 44f),
                () => DebugModeController.Instance?.SetEnabled(false),
                NexusTheme.SurfaceRaised,
                NexusTheme.Red,
                14f);

            return root;
        }

        private static void BuildItemsSection(Transform parent)
        {
            NexusUiFactory.CreateText(
                parent,
                "Items Header",
                UiText.DebugItemsHeader,
                new Vector2(28f, 120f),
                new Vector2(400f, 28f),
                16f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            var status = NexusUiFactory.CreateText(
                parent,
                "Items Status",
                string.Empty,
                new Vector2(28f, 150f),
                new Vector2(900f, 24f),
                12f,
                NexusTheme.Cyan);

            float y = 190f;
            foreach (string itemName in QuickItems)
            {
                string captured = itemName;
                NexusUiFactory.CreateText(
                    parent,
                    $"Label {captured}",
                    captured,
                    new Vector2(28f, y),
                    new Vector2(140f, 36f),
                    14f,
                    NexusTheme.Text);

                var qtyField = NexusUiFactory.CreateInputField(
                    parent,
                    $"Qty {captured}",
                    "qty",
                    new Vector2(180f, y),
                    new Vector2(100f, 36f));

                int current = GetLocalQuantity(captured);
                qtyField.text = current.ToString();

                NexusUiFactory.CreateButton(
                    parent,
                    $"Set {captured}",
                    UiText.DebugApplyQty,
                    new Vector2(300f, y),
                    new Vector2(120f, 36f),
                    () =>
                    {
                        if (!int.TryParse(qtyField.text, out int qty))
                        {
                            status.text = UiText.DebugInvalidNumber;
                            return;
                        }

                        if (SetLocalQuantity(captured, qty))
                        {
                            status.text = UiText.DebugItemUpdated(captured, qty);
                            qtyField.text = Mathf.Max(0, qty).ToString();
                        }
                        else
                            status.text = UiText.DebugItemFailed;
                    },
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f),
                    NexusTheme.Gold,
                    12f);

                NexusUiFactory.CreateButton(
                    parent,
                    $"+10 {captured}",
                    "+10",
                    new Vector2(440f, y),
                    new Vector2(72f, 36f),
                    () =>
                    {
                        int next = GetLocalQuantity(captured) + 10;
                        SetLocalQuantity(captured, next);
                        qtyField.text = next.ToString();
                        status.text = UiText.DebugItemUpdated(captured, next);
                    },
                    NexusTheme.SurfaceRaised,
                    NexusTheme.Text,
                    12f);

                y += 48f;
            }
        }

        private static void BuildCardsSection(Transform parent)
        {
            NexusUiFactory.CreateText(
                parent,
                "Cards Header",
                UiText.DebugCardsHeader,
                new Vector2(700f, 120f),
                new Vector2(400f, 28f),
                16f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            var listLabel = NexusUiFactory.CreateText(
                parent,
                "Card List",
                BuildCardSummary(),
                new Vector2(700f, 156f),
                new Vector2(520f, 280f),
                12f,
                NexusTheme.MutedText,
                TextAlignmentOptions.TopLeft);
            listLabel.textWrappingMode = TextWrappingModes.Normal;
            listLabel.overflowMode = TextOverflowModes.Truncate;

            var status = NexusUiFactory.CreateText(
                parent,
                "Cards Status",
                string.Empty,
                new Vector2(700f, 450f),
                new Vector2(520f, 28f),
                12f,
                NexusTheme.Cyan);

            NexusUiFactory.CreateButton(
                parent,
                "Add Random Card",
                UiText.DebugAddRandomCard,
                new Vector2(700f, 490f),
                new Vector2(240f, 44f),
                () =>
                {
                    if (TryAddRandomCard(out string name))
                    {
                        status.text = UiText.DebugCardAdded(name);
                        listLabel.text = BuildCardSummary();
                    }
                    else
                        status.text = UiText.DebugCardFailed;
                },
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f),
                NexusTheme.Gold,
                13f);

            NexusUiFactory.CreateButton(
                parent,
                "Upgrade First",
                UiText.DebugUpgradeFirstCard,
                new Vector2(960f, 490f),
                new Vector2(240f, 44f),
                () =>
                {
                    var cards = CardListManager.Instance?.GetCardEntities();
                    if (cards == null || cards.Count == 0)
                    {
                        status.text = UiText.DebugNoCards;
                        return;
                    }

                    cards[0].UpgradeCard();
                    DataUtil.Instance?.SaveCardData(cards);
                    status.text = UiText.DebugCardUpgraded(cards[0].cardName);
                    listLabel.text = BuildCardSummary();
                },
                NexusTheme.SurfaceRaised,
                NexusTheme.Text,
                13f);

            NexusUiFactory.CreateButton(
                parent,
                "Add Exp First",
                UiText.DebugAddExpFirstCard,
                new Vector2(700f, 550f),
                new Vector2(240f, 44f),
                () =>
                {
                    var cards = CardListManager.Instance?.GetCardEntities();
                    if (cards == null || cards.Count == 0)
                    {
                        status.text = UiText.DebugNoCards;
                        return;
                    }

                    cards[0].AddExperience(cards[0].ExpToNextLevel);
                    DataUtil.Instance?.SaveCardData(cards);
                    status.text = UiText.DebugCardLeveled(cards[0].cardName, cards[0].Level);
                    listLabel.text = BuildCardSummary();
                },
                NexusTheme.SurfaceRaised,
                NexusTheme.Text,
                13f);

            NexusUiFactory.CreateButton(
                parent,
                "Refresh List",
                UiText.DebugRefresh,
                new Vector2(960f, 550f),
                new Vector2(240f, 44f),
                () => { listLabel.text = BuildCardSummary(); },
                NexusTheme.SurfaceRaised,
                NexusTheme.MutedText,
                13f);
        }

        private static string BuildCardSummary()
        {
            var cards = CardListManager.Instance?.GetCardEntities();
            if (cards == null || cards.Count == 0)
                return UiText.DebugNoCards;

            var lines = new List<string>();
            int limit = Mathf.Min(cards.Count, 12);
            for (int i = 0; i < limit; i++)
            {
                CardEntity c = cards[i];
                lines.Add($"{i + 1}. {c.cardName}  Lv.{c.Level}  {c.CharacterTier}");
            }

            if (cards.Count > limit)
                lines.Add($"… +{cards.Count - limit}");
            return string.Join("\n", lines);
        }

        private static int GetLocalQuantity(string itemName)
        {
            if (ItemManager.Instance != null)
            {
                var items = ItemManager.Instance.GetItems();
                var hit = items?.Find(i =>
                    i != null && string.Equals(i.itemName, itemName, System.StringComparison.Ordinal));
                return hit?.quantity ?? 0;
            }

            if (DataUtil.Instance == null)
                return 0;

            var list = DataUtil.Instance.LoadInventory(InventoryStore.Local);
            var found = list?.Find(i =>
                i != null && string.Equals(i.itemName, itemName, System.StringComparison.Ordinal));
            return found?.quantity ?? 0;
        }

        private static bool SetLocalQuantity(string itemName, int quantity)
        {
            quantity = Mathf.Max(0, quantity);
            if (ItemManager.Instance != null)
                return ItemManager.Instance.SetItemQuantity(itemName, quantity);

            if (DataUtil.Instance == null)
                return false;

            var writeList = DataUtil.Instance.LoadInventory(InventoryStore.Local)
                            ?? new List<ItemEntity>();
            int idx = writeList.FindIndex(i =>
                i != null && string.Equals(i.itemName, itemName, System.StringComparison.Ordinal));
            if (quantity <= 0)
            {
                if (idx >= 0)
                    writeList.RemoveAt(idx);
            }
            else if (idx >= 0)
                writeList[idx].quantity = quantity;
            else
            {
                writeList.Add(new ItemEntity(itemName, itemName, itemName, 0, ItemType.Material)
                    .SetQuantity(quantity));
            }

            DataUtil.Instance.SaveInventory(InventoryStore.Local, writeList);
            return true;
        }

        private static bool TryAddRandomCard(out string cardName)
        {
            cardName = null;
            if (CardDataManager.Instance == null || CardListManager.Instance == null)
                return false;

            var character = CardDataManager.Instance.GetCharacter();
            if (character == null)
                return false;

            var card = CardDataManager.Instance.GetCardEntity(character);
            if (card == null)
                return false;

            CardListManager.Instance.AddCardEntity(card);
            cardName = card.cardName;
            return true;
        }
    }
}
