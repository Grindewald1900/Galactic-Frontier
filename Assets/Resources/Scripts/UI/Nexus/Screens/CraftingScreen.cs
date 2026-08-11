using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Utils;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Native crafting UI: recipe list, inputs/outputs, Start/Stop (P3).</summary>
    internal sealed class CraftingScreen
    {
        private readonly Transform root;
        private string selectedRecipeId = "";

        private CraftingScreen(Transform root)
        {
            this.root = root;
        }

        public GameObject Root => root.gameObject;

        public static CraftingScreen Build(Transform parent)
        {
            var panel = NexusUiFactory.CreatePanel(
                parent, "Crafting Screen", NexusTheme.Background,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var screen = new CraftingScreen(panel.transform);
            screen.Rebuild();
            return screen;
        }

        public void Rebuild()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            if (DataUtil.Instance != null && CardListManager.Instance != null)
                DeckService.EnsureLoaded(DataUtil.Instance, CardListManager.Instance.cardEntities);

            IdleSettlementService.EnsureLoaded();

            NexusUiFactory.CreateText(
                root, "Title", UiText.CraftingTitle,
                new Vector2(28f, 20f), new Vector2(600f, 36f), 22f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            NexusUiFactory.CreateText(
                root, "Hint", UiText.CraftingHint,
                new Vector2(28f, 56f), new Vector2(1100f, 28f), 13f, NexusTheme.MutedText);

            float y = 100f;
            foreach (var recipe in RecipeCatalog.All)
            {
                if (recipe == null) continue;
                var captured = recipe.recipeId;
                var selected = captured == selectedRecipeId;
                var name = UiText.T(recipe.displayNameEn, recipe.displayNameZh);
                var preview = ProductionService.PreviewQuality(captured, CardListManager.Instance?.cardEntities);
                var can = ProductionService.CanAfford(recipe);
                NexusUiFactory.CreateButton(
                    root, "Recipe " + captured,
                    $"{name}  [{preview}] {(can ? "" : UiText.CraftingMissingMats)}",
                    new Vector2(28f, y), new Vector2(720f, 40f),
                    () =>
                    {
                        selectedRecipeId = captured;
                        Rebuild();
                    },
                    selected
                        ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f)
                        : NexusTheme.SurfaceRaised,
                    selected ? NexusTheme.Gold : NexusTheme.Text,
                    12f);
                y += 46f;
            }

            BuildDetail();
            BuildRepairStrip();
        }

        private void BuildDetail()
        {
            GameObject box = NexusUiFactory.CreateBox(
                root, "Detail", new Vector2(780f, 100f), new Vector2(900f, 520f),
                NexusTheme.Surface, NexusTheme.BorderSoft);

            var recipe = RecipeCatalog.Get(selectedRecipeId);
            if (recipe == null)
            {
                NexusUiFactory.CreateText(
                    box.transform, "Empty", UiText.CraftingSelectRecipe,
                    new Vector2(24f, 24f), new Vector2(800f, 40f), 14f, NexusTheme.MutedText);
                return;
            }

            NexusUiFactory.CreateText(
                box.transform, "Name",
                UiText.T(recipe.displayNameEn, recipe.displayNameZh),
                new Vector2(24f, 20f), new Vector2(800f, 32f), 18f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            var inputs = new System.Text.StringBuilder();
            inputs.AppendLine(UiText.CraftingInputs);
            foreach (var input in recipe.inputs)
            {
                if (input == null) continue;
                var have = 0;
                if (ItemManagerOrZero(input.itemDefId, out have))
                {
                }

                have = CountLocal(input.itemDefId, input.minQuality);
                var def = ItemCatalog.Get(input.itemDefId);
                inputs.AppendLine($"  · {def?.displayNameEn ?? input.itemDefId} x{input.quantity} (Q≥{input.minQuality})  have {have}");
            }

            var outDef = ItemCatalog.Get(recipe.outputDefId);
            inputs.AppendLine();
            inputs.AppendLine(UiText.CraftingOutputs);
            inputs.AppendLine($"  · {outDef?.displayNameEn ?? recipe.outputDefId} x{recipe.outputQty}");
            inputs.AppendLine(UiText.CraftingExpectedQuality(
                ProductionService.PreviewQuality(recipe.recipeId, CardListManager.Instance?.cardEntities)));

            var body = NexusUiFactory.CreateText(
                box.transform, "Body", inputs.ToString(),
                new Vector2(24f, 60f), new Vector2(850f, 320f), 13f, NexusTheme.MutedText);
            body.textWrappingMode = TextWrappingModes.Normal;

            NexusUiFactory.CreateButton(
                box.transform, "Start", UiText.CraftingStart,
                new Vector2(24f, 420f), new Vector2(200f, 48f),
                () =>
                {
                    var r = ProductionService.TryStartRecipe(
                        recipe.recipeId, CardListManager.Instance?.cardEntities);
                    Debug.Log("[CRAFT] start: " + (r.Success ? "ok" : r.Message));
                    Rebuild();
                },
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f), NexusTheme.Gold, 14f);

            NexusUiFactory.CreateButton(
                box.transform, "Stop", UiText.StopAction,
                new Vector2(240f, 420f), new Vector2(200f, 48f),
                () =>
                {
                    foreach (var deck in DeckService.GetBusyDecks())
                    {
                        if (deck?.action == null) continue;
                        if (deck.action.actionType == DeckActionType.Process
                            || deck.action.actionType == DeckActionType.Manufacture)
                        {
                            DeckService.TryStop(deck.deckId);
                            break;
                        }
                    }

                    Rebuild();
                },
                NexusTheme.SurfaceRaised, NexusTheme.Text, 14f);
        }

        private void BuildRepairStrip()
        {
            NexusUiFactory.CreateButton(
                root, "AutoRepair", UiText.CraftingAutoRepair,
                new Vector2(780f, 640f), new Vector2(280f, 44f),
                () =>
                {
                    var n = DurabilityService.TryAutoRepairAll();
                    Debug.Log("[DURA] auto-repair count=" + n);
                    Rebuild();
                },
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.16f), NexusTheme.Cyan, 13f);

            NexusUiFactory.CreateButton(
                root, "RepairFirst", UiText.CraftingRepairFirst,
                new Vector2(1080f, 640f), new Vector2(280f, 44f),
                () =>
                {
                    foreach (var item in ProductionService.GetLocalItems())
                    {
                        if (item == null || string.IsNullOrEmpty(item.itemInstanceId)) continue;
                        if (item.durability >= item.maxDurability) continue;
                        if (DurabilityService.TryRepairInstance(item.itemInstanceId))
                            break;
                    }

                    Rebuild();
                },
                NexusTheme.SurfaceRaised, NexusTheme.Text, 13f);
        }

        private static bool ItemManagerOrZero(string defId, out int have)
        {
            have = CountLocal(defId, 1);
            return true;
        }

        private static int CountLocal(string defId, int minQ)
        {
            return InventoryRules.CountOf(
                ItemFactory.ToStacks(ProductionService.GetLocalItems()), defId, minQ);
        }
    }
}
