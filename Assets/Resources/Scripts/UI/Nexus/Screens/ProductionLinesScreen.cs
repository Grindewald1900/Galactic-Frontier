using System;
using System.Collections.Generic;
using System.Text;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Automated production lines page (economy/15 §4.10). Lets the player build lines, assign the
    /// shared industry fleet, pick the active recipe per line, enable/disable, and upgrade tiers.
    /// Runs in the background via <see cref="ProductionLineService"/>.
    /// </summary>
    internal sealed class ProductionLinesScreen
    {
        private readonly Transform root;
        private readonly Action openFormation;

        private ProductionLinesScreen(Transform root, Action openFormation)
        {
            this.root = root;
            this.openFormation = openFormation;
        }

        public GameObject Root => root.gameObject;

        public static ProductionLinesScreen Build(Transform parent, Action openFormation)
        {
            var panel = NexusUiFactory.CreatePanel(
                parent, "Production Lines Screen", NexusTheme.Background,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var screen = new ProductionLinesScreen(panel.transform, openFormation);
            screen.Rebuild();
            return screen;
        }

        public void Rebuild()
        {
            if (DataUtil.Instance != null && CardListManager.Instance != null)
                DeckService.EnsureLoaded(DataUtil.Instance, CardListManager.Instance.cardEntities);
            ProductionLineService.EnsureLoaded();
            // Catch the lines up before drawing so the state/progress reflects reality.
            ProductionLineService.SettleOnline(CardListManager.Instance?.cardEntities);

            for (var i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);

            NexusUiFactory.CreateText(
                root, "Title", UiText.T("Production Lines", "自动化流水线"),
                new Vector2(28f, 20f), new Vector2(700f, 36f), 22f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(
                root, "Hint",
                UiText.T(
                    "Build lines, staff one shared industry fleet, then let them craft in the background.",
                    "建造产线、配置一支共享工业舰队，之后它们将在后台自动生产。"),
                new Vector2(28f, 56f), new Vector2(1200f, 24f), 13f, NexusTheme.MutedText);

            BuildFleetPanel();

            var cards = CardListManager.Instance?.cardEntities;
            float y = 150f;
            foreach (var def in ProductionLineCatalog.All)
            {
                var line = ProductionLineService.Find(def.lineId);
                if (line == null) continue;
                BuildLineCard(def, line, cards, y);
                y += 196f;
            }
        }

        private void BuildFleetPanel()
        {
            var box = NexusUiFactory.CreateBox(
                root, "FleetPanel", new Vector2(28f, 96f), new Vector2(1480f, 44f),
                NexusTheme.Surface, NexusTheme.BorderSoft);

            var deckId = ProductionLineService.ProductionDeckId;
            var deck = FindDeck(deckId);
            var occupied = deck?.action != null && deck.action.actionType == DeckActionType.Production && deck.IsActionBusy;
            var status = deck == null
                ? UiText.T("Industry fleet: unassigned", "工业舰队：未分配")
                : UiText.T(
                    $"Industry fleet: {deck.displayName} ({deck.MemberCount}/5){(occupied ? " · occupied" : "")}",
                    $"工业舰队：{deck.displayName}（{deck.MemberCount}/5）{(occupied ? " · 占用中" : "")}");
            NexusUiFactory.CreateText(
                box.transform, "Status", status,
                new Vector2(16f, 10f), new Vector2(520f, 24f), 13f,
                deck == null ? NexusTheme.Red : NexusTheme.Cyan);

            // Eligible decks to (re)assign as the shared industry fleet.
            float x = 560f;
            var combat = DeckService.GetActiveCombatDeck();
            foreach (var d in DeckService.GetDecks())
            {
                if (d == null || !d.unlocked || d.MemberCount < 1) continue;
                if (combat != null && d.deckId == combat.deckId) continue;
                var captured = d.deckId;
                var current = d.deckId == deckId;
                NexusUiFactory.CreateButton(
                    box.transform, "Pick_" + d.deckId,
                    current ? UiText.T("✓ " + d.displayName, "✓ " + d.displayName) : d.displayName,
                    new Vector2(x, 6f), new Vector2(180f, 32f),
                    () =>
                    {
                        var r = ProductionLineService.SetProductionDeck(captured, CardListManager.Instance?.cardEntities);
                        if (!r.Success) NexusSnackbar.Show(r.Message);
                        Rebuild();
                    },
                    current ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f) : NexusTheme.SurfaceRaised,
                    current ? NexusTheme.Gold : NexusTheme.Text, 12f);
                x += 188f;
            }

            NexusUiFactory.CreateButton(
                box.transform, "EditCrew", UiText.T("Edit crew", "编辑舰员"),
                new Vector2(1320f, 6f), new Vector2(150f, 32f),
                () => openFormation?.Invoke(),
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.16f), NexusTheme.Cyan, 12f);
        }

        private void BuildLineCard(ProductionLineDef def, ProductionLineInstance line, IList<CardEntity> cards, float y)
        {
            var box = NexusUiFactory.CreateBox(
                root, "Line_" + def.lineId, new Vector2(28f, y), new Vector2(1480f, 180f),
                NexusTheme.Surface, NexusTheme.BorderSoft);

            NexusUiFactory.CreateText(
                box.transform, "Name",
                $"{UiText.T(def.displayNameEn, def.displayNameZh)}   T{line.tier}/{def.tierCap}",
                new Vector2(16f, 12f), new Vector2(620f, 28f), 17f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(
                box.transform, "Desc", UiText.T(def.descriptionEn, def.descriptionZh),
                new Vector2(16f, 42f), new Vector2(760f, 22f), 12f, NexusTheme.MutedText);

            var view = ProductionLineService.GetView(line, cards);

            // Status line + animated progress bar.
            var stateColor = view.State switch
            {
                ProductionLineState.Running => NexusTheme.Green,
                ProductionLineState.PausedBlock => NexusTheme.Red,
                _ => NexusTheme.DimText
            };
            NexusUiFactory.CreateText(
                box.transform, "State", UiText.T(view.ReasonEn, view.ReasonZh),
                new Vector2(16f, 70f), new Vector2(420f, 22f), 12f, stateColor);

            var capturedLine = line;
            var capturedCards = cards;
            NexusTimedBar.Attach(
                box.transform, "Bar", new Vector2(16f, 96f), 420f, 18f,
                () =>
                {
                    var v = ProductionLineService.GetView(capturedLine, capturedCards);
                    if (v.State == ProductionLineState.Running) return v.Progress;
                    return v.State == ProductionLineState.PausedBlock ? 1f : 0f;
                },
                () =>
                {
                    var v = ProductionLineService.GetView(capturedLine, capturedCards);
                    if (v.State == ProductionLineState.Running && v.CycleSeconds > 0)
                    {
                        var remain = Mathf.Max(0, Mathf.CeilToInt(v.CycleSeconds * (1f - v.Progress)));
                        return UiText.T($"{remain}s to output", $"{remain}秒后产出");
                    }

                    return UiText.T(v.ReasonEn, v.ReasonZh);
                },
                view.State == ProductionLineState.Running ? NexusTheme.Green : NexusTheme.Cyan);

            BuildRecipeRow(box.transform, def, line);
            BuildLineActions(box.transform, def, line);
        }

        private void BuildRecipeRow(Transform card, ProductionLineDef def, ProductionLineInstance line)
        {
            NexusUiFactory.CreateText(
                card, "RecipeTitle", UiText.T("Recipe", "配方"),
                new Vector2(460f, 12f), new Vector2(200f, 22f), 12f, NexusTheme.MutedText);

            float x = 460f;
            foreach (var recipeId in def.allowedRecipeIds)
            {
                var recipe = RecipeCatalog.Get(recipeId);
                if (recipe == null) continue;
                var captured = recipeId;
                var selected = line.activeRecipeId == recipeId;
                var tierOk = line.tier >= recipe.requiredLineTier;
                var name = UiText.T(recipe.displayNameEn, recipe.displayNameZh);
                var label = $"{name}\n(T{recipe.requiredLineTier} · {recipe.cycleSeconds}s)";
                NexusUiFactory.CreateButton(
                    card, "Rcp_" + def.lineId + "_" + recipeId, label,
                    new Vector2(x, 40f), new Vector2(168f, 52f),
                    () =>
                    {
                        var r = ProductionLineService.SetActiveRecipe(def.lineId, captured);
                        if (!r.Success) NexusSnackbar.Show(r.Message);
                        Rebuild();
                    },
                    selected
                        ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f)
                        : tierOk ? NexusTheme.SurfaceRaised : NexusTheme.WithAlpha(NexusTheme.SurfaceRaised, 0.5f),
                    selected ? NexusTheme.Gold : tierOk ? NexusTheme.Text : NexusTheme.DimText,
                    11f);
                x += 176f;
            }
        }

        private void BuildLineActions(Transform card, ProductionLineDef def, ProductionLineInstance line)
        {
            if (!line.built)
            {
                NexusUiFactory.CreateRoleButton(
                    card, "Build", UiText.T($"Build line\n{CostText(def.buildCost)}", $"建造产线\n{CostText(def.buildCost)}"),
                    new Vector2(460f, 110f), new Vector2(220f, 52f),
                    () =>
                    {
                        var r = ProductionLineService.TryBuild(def.lineId);
                        NexusSnackbar.Show(r.Success ? UiText.T("Line built.", "产线已建造。") : r.Message);
                        Rebuild();
                    },
                    NexusButtonRole.Primary, 12f);
                return;
            }

            var enabled = line.enabled;
            NexusUiFactory.CreateRoleButton(
                card, "Toggle", enabled ? UiText.T("Stop line", "停用产线") : UiText.T("Start line", "启用产线"),
                new Vector2(460f, 118f), new Vector2(180f, 44f),
                () =>
                {
                    var r = ProductionLineService.SetEnabled(def.lineId, !enabled, CardListManager.Instance?.cardEntities);
                    if (!r.Success) NexusSnackbar.Show(r.Message);
                    Rebuild();
                },
                enabled ? NexusButtonRole.Danger : NexusButtonRole.Primary, 13f);

            var atMax = line.tier >= def.tierCap;
            NexusUiFactory.CreateButton(
                card, "Upgrade",
                atMax
                    ? UiText.T("Max tier", "已满级")
                    : UiText.T($"Upgrade → T{line.tier + 1}\n{CostText(UpgradeCost(def, line.tier))}",
                        $"升级 → T{line.tier + 1}\n{CostText(UpgradeCost(def, line.tier))}"),
                new Vector2(652f, 118f), new Vector2(220f, 44f),
                () =>
                {
                    if (atMax) return;
                    var r = ProductionLineService.TryUpgradeTier(def.lineId);
                    NexusSnackbar.Show(r.Success ? UiText.T("Line upgraded.", "产线已升级。") : r.Message);
                    Rebuild();
                },
                atMax ? NexusTheme.WithAlpha(NexusTheme.SurfaceRaised, 0.5f) : NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f),
                atMax ? NexusTheme.DimText : NexusTheme.Gold, 12f);
        }

        private static RecipeInput[] UpgradeCost(ProductionLineDef def, int tier)
        {
            if (def.buildCost == null) return Array.Empty<RecipeInput>();
            var mult = Mathf.Max(1, tier);
            var result = new RecipeInput[def.buildCost.Length];
            for (var i = 0; i < def.buildCost.Length; i++)
            {
                result[i] = new RecipeInput
                {
                    itemDefId = def.buildCost[i].itemDefId,
                    quantity = def.buildCost[i].quantity * mult,
                    minQuality = def.buildCost[i].minQuality
                };
            }

            return result;
        }

        private static string CostText(RecipeInput[] cost)
        {
            if (cost == null || cost.Length == 0) return "";
            var sb = new StringBuilder();
            for (var i = 0; i < cost.Length; i++)
            {
                if (cost[i] == null) continue;
                var def = ItemCatalog.Get(cost[i].itemDefId);
                var name = def != null ? UiText.T(def.displayNameEn, def.displayNameZh) : cost[i].itemDefId;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append($"{name}×{cost[i].quantity}");
            }

            return sb.ToString();
        }

        private static DeckEntity FindDeck(string deckId)
        {
            if (string.IsNullOrEmpty(deckId)) return null;
            foreach (var d in DeckService.GetDecks())
                if (d != null && d.deckId == deckId)
                    return d;
            return null;
        }
    }
}
