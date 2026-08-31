using System.Collections.Generic;
using System.Linq;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Inventory;
using Assets.Resources.Scripts.Market;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.DebugTools;
using Assets.Resources.Scripts.Utils.Save;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Debug Mode: edit credits and every catalog item quantity; add/edit player cards.
    /// </summary>
    internal sealed class DebugScreen
    {
        private readonly Transform root;
        private readonly System.Action onUniverseRegenerated;

        private DebugScreen(Transform root, System.Action onUniverseRegenerated)
        {
            this.root = root;
            this.onUniverseRegenerated = onUniverseRegenerated;
        }

        public GameObject Root => root.gameObject;

        public static DebugScreen Build(Transform parent, System.Action onUniverseRegenerated = null)
        {
            var panel = NexusUiFactory.CreatePanel(
                parent, "Debug Screen", NexusTheme.Background,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var screen = new DebugScreen(panel.transform, onUniverseRegenerated);
            screen.Rebuild();
            return screen;
        }

        public void Rebuild()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            NexusUiFactory.CreateText(
                root, "Title", UiText.DebugTitle,
                new Vector2(28f, 16f), new Vector2(640f, 32f), 22f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);

            NexusUiFactory.CreateText(
                root, "Hint", UiText.DebugHintAllItems,
                new Vector2(28f, 48f), new Vector2(1100f, 28f), 12f, NexusTheme.MutedText);

            BuildCreditsRow();
            BuildItemsSection();
            BuildCardsSection();
            BuildMapGenSection();

            NexusUiFactory.CreateButton(
                root, "Disable Debug", UiText.DebugDisable,
                new Vector2(28f, 830f), new Vector2(280f, 40f),
                () => DebugModeController.Instance?.SetEnabled(false),
                NexusTheme.SurfaceRaised, NexusTheme.Red, 14f);
        }

        private void BuildCreditsRow()
        {
            NexusUiFactory.CreateText(
                root, "Credits Header", UiText.DebugCreditsHeader,
                new Vector2(28f, 80f), new Vector2(200f, 24f), 14f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            var creditsField = NexusUiFactory.CreateInputField(
                root, "Credits Qty", CurrencyService.Credits.ToString(),
                new Vector2(220f, 78f), new Vector2(120f, 32f));

            var status = NexusUiFactory.CreateText(
                root, "Credits Status", "",
                new Vector2(620f, 80f), new Vector2(400f, 24f), 12f, NexusTheme.Cyan);

            NexusUiFactory.CreateButton(
                root, "Set Credits", UiText.DebugApplyQty,
                new Vector2(360f, 78f), new Vector2(100f, 32f),
                () =>
                {
                    if (!int.TryParse(creditsField.text, out int qty))
                    {
                        status.text = UiText.DebugInvalidNumber;
                        return;
                    }

                    CurrencyService.SetCredits(qty);
                    creditsField.text = CurrencyService.Credits.ToString();
                    status.text = UiText.DebugCreditsUpdated(CurrencyService.Credits);
                },
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f), NexusTheme.Gold, 12f);

            NexusUiFactory.CreateButton(
                root, "+1000 Credits", "+1000",
                new Vector2(480f, 78f), new Vector2(88f, 32f),
                () =>
                {
                    CurrencyService.AddCredits(1000);
                    creditsField.text = CurrencyService.Credits.ToString();
                    status.text = UiText.DebugCreditsUpdated(CurrencyService.Credits);
                },
                NexusTheme.SurfaceRaised, NexusTheme.Text, 12f);
        }

        private void BuildItemsSection()
        {
            NexusUiFactory.CreateText(
                root, "Items Header", UiText.DebugItemsHeader,
                new Vector2(28f, 118f), new Vector2(500f, 22f), 14f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            var status = NexusUiFactory.CreateText(
                root, "Items Status", "",
                new Vector2(28f, 140f), new Vector2(900f, 18f), 11f, NexusTheme.Cyan);

            var defs = ItemCatalog.All.Values
                .Where(d => d != null)
                .OrderBy(d => d.category)
                .ThenBy(d => d.displayNameEn)
                .ToList();

            const int cols = 2;
            const float rowH = 34f;
            for (var i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                int col = i % cols;
                int row = i / cols;
                float x = 28f + col * 520f;
                float y = 164f + row * rowH;
                BuildItemRow(def, x, y, status);
            }
        }

        private static void BuildItemRow(ItemDef def, float x, float y, TextMeshProUGUI status)
        {
            var label = $"{def.displayNameEn} [{def.category}]";
            NexusUiFactory.CreateText(
                status.transform.parent, "Lbl " + def.itemDefId, label,
                new Vector2(x, y), new Vector2(230f, 30f), 11f, NexusTheme.Text);

            int current = GetLocalQuantity(def.itemDefId);
            var qtyField = NexusUiFactory.CreateInputField(
                status.transform.parent, "Qty " + def.itemDefId, current.ToString(),
                new Vector2(x + 234f, y), new Vector2(70f, 30f));

            NexusUiFactory.CreateButton(
                status.transform.parent, "Set " + def.itemDefId, UiText.DebugApplyQty,
                new Vector2(x + 310f, y), new Vector2(72f, 30f),
                () =>
                {
                    if (!int.TryParse(qtyField.text, out int qty))
                    {
                        status.text = UiText.DebugInvalidNumber;
                        return;
                    }

                    if (SetLocalQuantity(def.itemDefId, qty))
                    {
                        status.text = UiText.DebugItemUpdated(def.displayNameEn, qty);
                        qtyField.text = Mathf.Max(0, qty).ToString();
                    }
                    else
                        status.text = UiText.DebugItemFailed;
                },
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f), NexusTheme.Gold, 11f);

            NexusUiFactory.CreateButton(
                status.transform.parent, "+10 " + def.itemDefId, "+10",
                new Vector2(x + 388f, y), new Vector2(52f, 30f),
                () =>
                {
                    int next = GetLocalQuantity(def.itemDefId) + 10;
                    SetLocalQuantity(def.itemDefId, next);
                    qtyField.text = next.ToString();
                    status.text = UiText.DebugItemUpdated(def.displayNameEn, next);
                },
                NexusTheme.SurfaceRaised, NexusTheme.Text, 11f);
        }

        private void BuildCardsSection()
        {
            NexusUiFactory.CreateText(
                root, "Cards Header", UiText.DebugCardsHeader,
                new Vector2(1100f, 118f), new Vector2(280f, 22f), 14f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            var listLabel = NexusUiFactory.CreateText(
                root, "Card List", BuildCardSummary(),
                new Vector2(1100f, 146f), new Vector2(560f, 220f), 12f, NexusTheme.MutedText,
                TextAlignmentOptions.TopLeft);
            listLabel.textWrappingMode = TextWrappingModes.Normal;
            listLabel.overflowMode = TextOverflowModes.Truncate;

            var status = NexusUiFactory.CreateText(
                root, "Cards Status", "",
                new Vector2(1100f, 370f), new Vector2(560f, 22f), 12f, NexusTheme.Cyan);

            NexusUiFactory.CreateButton(
                root, "Add Random Card", UiText.DebugAddRandomCard,
                new Vector2(1100f, 400f), new Vector2(260f, 40f),
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
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f), NexusTheme.Gold, 13f);

            NexusUiFactory.CreateButton(
                root, "Upgrade First", UiText.DebugUpgradeFirstCard,
                new Vector2(1380f, 400f), new Vector2(260f, 40f),
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
                NexusTheme.SurfaceRaised, NexusTheme.Text, 13f);

            NexusUiFactory.CreateButton(
                root, "Add Exp First", UiText.DebugAddExpFirstCard,
                new Vector2(1100f, 450f), new Vector2(260f, 40f),
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
                NexusTheme.SurfaceRaised, NexusTheme.Text, 13f);

            NexusUiFactory.CreateButton(
                root, "Refresh List", UiText.DebugRefresh,
                new Vector2(1380f, 450f), new Vector2(260f, 40f),
                () => { listLabel.text = BuildCardSummary(); },
                NexusTheme.SurfaceRaised, NexusTheme.MutedText, 13f);
        }

        private void BuildMapGenSection()
        {
            NexusUiFactory.CreateText(
                root, "Map Gen Header", UiText.DebugMapGenHeader,
                new Vector2(1100f, 500f), new Vector2(400f, 22f), 14f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            int currentSeed = WorldService.IsLoaded ? WorldService.State.universeSeed : UniverseMapCatalog.ActiveSeed;
            var seedField = NexusUiFactory.CreateInputField(
                root, "Universe Seed", currentSeed.ToString(),
                new Vector2(1100f, 528f), new Vector2(160f, 32f));

            var report = NexusUiFactory.CreateText(
                root, "Map Gen Report", DescribeUniverse(),
                new Vector2(1100f, 568f), new Vector2(560f, 200f), 11f, NexusTheme.MutedText,
                TextAlignmentOptions.TopLeft);
            report.textWrappingMode = TextWrappingModes.Normal;
            report.overflowMode = TextOverflowModes.Truncate;

            NexusUiFactory.CreateButton(
                root, "Regenerate Universe", UiText.DebugMapGenRegenerate,
                new Vector2(1280f, 528f), new Vector2(180f, 32f),
                () =>
                {
                    if (!int.TryParse(seedField.text, out int seed))
                    {
                        report.text = UiText.DebugInvalidNumber;
                        return;
                    }

                    WorldService.RegenerateUniverse(seed, DataUtil.Instance);
                    seedField.text = WorldService.State.universeSeed.ToString();
                    report.text = DescribeUniverse();
                    onUniverseRegenerated?.Invoke();
                },
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f), NexusTheme.Gold, 12f);

            NexusUiFactory.CreateButton(
                root, "Validate Universe", UiText.DebugMapGenValidate,
                new Vector2(1470f, 528f), new Vector2(180f, 32f),
                () => { report.text = DescribeUniverse(validateOnly: true); },
                NexusTheme.SurfaceRaised, NexusTheme.Text, 12f);
        }

        private static string DescribeUniverse(bool validateOnly = false)
        {
            var universe = UniverseMapCatalog.ActiveUniverse;
            if (universe == null)
                return UiText.DebugMapGenNoData;

            var validation = validateOnly
                ? UniverseValidator.Validate(universe)
                : universe.validation ?? UniverseValidator.Validate(universe);

            var lines = new List<string>
            {
                UiText.DebugMapGenSummary(
                    universe.sectors?.Count ?? 0,
                    universe.routes?.Count ?? 0,
                    WorldService.IsLoaded ? WorldService.State.universeSeed : universe.seed)
            };

            if (universe.planeModifiers != null)
            {
                lines.Add(UiText.DebugMapGenPlane(
                    universe.planeModifiers.primary,
                    universe.planeModifiers.secondary,
                    universe.planeModifiers.gap));
            }

            lines.Add(validation.isValid ? UiText.DebugMapGenValid : UiText.DebugMapGenInvalid);
            if (validation.issues != null)
            {
                foreach (var issue in validation.issues)
                {
                    if (issue == null) continue;
                    lines.Add($"- {issue.code}: {issue.message}");
                }
            }

            return string.Join("\n", lines);
        }

        private static string BuildCardSummary()
        {
            var cards = CardListManager.Instance?.GetCardEntities();
            if (cards == null || cards.Count == 0)
                return UiText.DebugNoCards;

            var lines = new List<string>();
            int limit = Mathf.Min(cards.Count, 10);
            for (int i = 0; i < limit; i++)
            {
                CardEntity c = cards[i];
                lines.Add($"{i + 1}. {c.cardName}  Lv.{c.Level}  {c.CharacterTier}");
            }

            if (cards.Count > limit)
                lines.Add($"… +{cards.Count - limit}");
            return string.Join("\n", lines);
        }

        private static int GetLocalQuantity(string itemDefId)
        {
            if (ItemManager.Instance != null && ItemManager.Instance.HasLoaded)
                return ItemManager.Instance.CountDef(itemDefId, 1);

            if (DataUtil.Instance == null)
                return 0;

            var list = DataUtil.Instance.LoadInventory(InventoryStore.Local);
            return InventoryRules.CountOf(ItemFactory.ToStacks(list), itemDefId, 1);
        }

        private static bool SetLocalQuantity(string itemDefId, int quantity)
        {
            quantity = Mathf.Max(0, quantity);
            if (ItemManager.Instance != null && ItemManager.Instance.HasLoaded)
                return ItemManager.Instance.SetItemQuantityByDef(itemDefId, quantity);

            if (DataUtil.Instance == null)
                return false;

            var writeList = DataUtil.Instance.LoadInventory(InventoryStore.Local)
                            ?? new List<ItemEntity>();
            var stacks = ItemFactory.ToStacks(writeList);
            var have = InventoryRules.CountOf(stacks, itemDefId, 1);
            if (have > 0)
                InventoryRules.TryConsume(stacks, itemDefId, have, 1);
            if (quantity > 0)
                InventoryRules.TryMerge(stacks, ItemFactory.ToStack(ItemFactory.FromDef(itemDefId, quantity)), 60);

            writeList.Clear();
            foreach (var s in stacks)
                writeList.Add(ItemFactory.FromStack(s));
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
