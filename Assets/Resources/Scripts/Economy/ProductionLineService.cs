using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Progression;
using Assets.Resources.Scripts.Progression.Domain;
using Assets.Resources.Scripts.World;
using UnityEngine;

namespace Assets.Resources.Scripts.Economy
{
    /// <summary>
    /// Automated production lines (economy/15 §4.10). Lines run in the background without the player
    /// hand-cranking batches. Per project rule they share ONE occupied "industry fleet" deck whose crew
    /// must satisfy each recipe's production requirements. Settlement is real-time (online ticker +
    /// offline resume) and reuses <see cref="ProductionService"/> warehouse + quality logic.
    /// State persists inside <see cref="PlayerIdleState"/> (idle.json).
    /// </summary>
    public static class ProductionLineService
    {
        private const string OccupyTargetId = "__production_lines__";
        private const long OnlineElapsedCapSeconds = 3600;

        public enum LineReady
        {
            Ok,
            NotBuilt,
            Disabled,
            NoRecipe,
            TierTooLow,
            NoDeck,
            DeckBusyElsewhere,
            SkillGate
        }

        public sealed class LineView
        {
            public ProductionLineState State;
            public LineReady Ready;
            public string ReasonEn = "";
            public string ReasonZh = "";
            public int CycleSeconds;
            public float Progress;
            public RecipeDef ActiveRecipe;
        }

        public static void EnsureLoaded()
        {
            IdleSettlementService.EnsureLoaded();
            var st = IdleSettlementService.State;
            st.productionLines ??= new List<ProductionLineInstance>();
            foreach (var def in ProductionLineCatalog.All)
            {
                if (def == null) continue;
                if (Find(def.lineId) == null)
                    st.productionLines.Add(new ProductionLineInstance { lineId = def.lineId, tier = 1 });
            }
        }

        public static IReadOnlyList<ProductionLineInstance> Lines
        {
            get
            {
                EnsureLoaded();
                return IdleSettlementService.State.productionLines;
            }
        }

        public static ProductionLineInstance Find(string lineId)
        {
            var st = IdleSettlementService.State;
            if (st?.productionLines == null || string.IsNullOrEmpty(lineId)) return null;
            foreach (var l in st.productionLines)
                if (l != null && l.lineId == lineId)
                    return l;
            return null;
        }

        public static string ProductionDeckId
        {
            get
            {
                EnsureLoaded();
                return IdleSettlementService.State.productionDeckId ?? "";
            }
        }

        // ---------------------------------------------------------------- config

        public static EconomyCommandResult SetProductionDeck(string deckId, IList<CardEntity> cards)
        {
            EnsureLoaded();
            DeckService.EnsureReady();
            var deck = FindDeck(deckId);
            if (deck == null || !deck.unlocked)
                return EconomyCommandResult.Fail("Deck not available.");
            if (deck.MemberCount < 1)
                return EconomyCommandResult.Fail("Assign crew to that fleet first.");
            var combat = DeckService.GetActiveCombatDeck();
            if (combat != null && deck.deckId == combat.deckId)
                return EconomyCommandResult.Fail("The active combat deck cannot be the industry fleet.");
            if (deck.IsActionBusy && !(deck.action != null && deck.action.actionType == DeckActionType.Production))
                return EconomyCommandResult.Fail("That fleet is busy with another action.");

            var previous = IdleSettlementService.State.productionDeckId;
            if (!string.IsNullOrEmpty(previous) && previous != deckId)
            {
                var old = FindDeck(previous);
                if (old?.action != null && old.action.actionType == DeckActionType.Production && old.IsActionBusy)
                    DeckService.TryStop(previous);
            }

            IdleSettlementService.State.productionDeckId = deckId;
            SyncDeckOccupation(cards);
            IdleSettlementService.Save();
            return EconomyCommandResult.Ok();
        }

        public static EconomyCommandResult TryBuild(string lineId)
        {
            EnsureLoaded();
            var def = ProductionLineCatalog.Get(lineId);
            var line = Find(lineId);
            if (def == null || line == null)
                return EconomyCommandResult.Fail("Unknown production line.");
            if (line.built)
                return EconomyCommandResult.Fail("Line already built.");

            var cost = new RecipeDef { inputs = def.buildCost };
            if (!ProductionService.CanAfford(cost))
                return EconomyCommandResult.Fail("Missing materials to build the line.");
            if (!ProductionService.TryConsumeRecipeBatch(cost))
                return EconomyCommandResult.Fail("Could not consume build materials.");

            line.built = true;
            line.tier = Mathf.Max(1, line.tier);
            IdleSettlementService.Save();
            Debug.Log($"[LINE] built {lineId}");
            return EconomyCommandResult.Ok();
        }

        public static EconomyCommandResult SetActiveRecipe(string lineId, string recipeId)
        {
            EnsureLoaded();
            var def = ProductionLineCatalog.Get(lineId);
            var line = Find(lineId);
            if (def == null || line == null)
                return EconomyCommandResult.Fail("Unknown production line.");
            if (!line.built)
                return EconomyCommandResult.Fail("Build the line first.");
            if (!IsRecipeAllowed(def, recipeId))
                return EconomyCommandResult.Fail("Recipe not supported by this line.");

            line.activeRecipeId = recipeId ?? "";
            line.lastSettledAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            IdleSettlementService.Save();
            return EconomyCommandResult.Ok();
        }

        public static EconomyCommandResult SetEnabled(string lineId, bool enabled, IList<CardEntity> cards)
        {
            EnsureLoaded();
            var line = Find(lineId);
            if (line == null)
                return EconomyCommandResult.Fail("Unknown production line.");
            if (enabled && !line.built)
                return EconomyCommandResult.Fail("Build the line first.");
            if (enabled && string.IsNullOrEmpty(line.activeRecipeId))
                return EconomyCommandResult.Fail("Select a recipe first.");

            line.enabled = enabled;
            line.lastSettledAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (!enabled)
                line.state = ProductionLineState.Idle;
            SyncDeckOccupation(cards);
            IdleSettlementService.Save();
            return EconomyCommandResult.Ok();
        }

        public static EconomyCommandResult TryUpgradeTier(string lineId)
        {
            EnsureLoaded();
            var def = ProductionLineCatalog.Get(lineId);
            var line = Find(lineId);
            if (def == null || line == null)
                return EconomyCommandResult.Fail("Unknown production line.");
            if (!line.built)
                return EconomyCommandResult.Fail("Build the line first.");
            if (line.tier >= def.tierCap)
                return EconomyCommandResult.Fail("Line already at max tier.");

            var cost = new RecipeDef { inputs = ScaleCost(def.buildCost, line.tier) };
            if (!ProductionService.CanAfford(cost))
                return EconomyCommandResult.Fail("Missing materials to upgrade.");
            if (!ProductionService.TryConsumeRecipeBatch(cost))
                return EconomyCommandResult.Fail("Could not consume upgrade materials.");

            line.tier += 1;
            IdleSettlementService.Save();
            Debug.Log($"[LINE] {lineId} -> T{line.tier}");
            return EconomyCommandResult.Ok();
        }

        // ---------------------------------------------------------------- settlement

        /// <summary>Full-rate online settlement (called by the idle ticker).</summary>
        public static void SettleOnline(IList<CardEntity> cards)
        {
            SettleInternal(cards, OnlineElapsedCapSeconds, 1f, offline: false, ownGrantScope: true);
        }

        /// <summary>Offline catch-up on app resume; scaled by the shared offline cap + yield.</summary>
        public static void SettleOffline(long elapsedSeconds, IList<CardEntity> cards)
        {
            if (elapsedSeconds < 5) return;
            ShipService.EnsureReady();
            var cargo = 0;
            if (ShipService.State?.modules != null)
            {
                foreach (var m in ShipService.State.modules)
                    if (m != null && m.moduleId == "mod_cargo")
                        cargo = m.level;
            }

            var cap = OfflineRules.EffectiveCapSeconds(
                ShipService.State?.level ?? 1,
                cargo,
                Math.Max(1, Assets.Resources.Scripts.Utils.DataUtil.Instance?.currentPlayer?.level ?? 1));
            var effective = Math.Min(elapsedSeconds, cap);
            var yield = OfflineRules.YieldRatio(effective, cap);
            SettleInternal(cards, effective, yield, offline: true, ownGrantScope: true);
        }

        private static void SettleInternal(
            IList<CardEntity> cards, long maxElapsedPerLine, float yield, bool offline, bool ownGrantScope)
        {
            EnsureLoaded();
            var st = IdleSettlementService.State;
            SyncDeckOccupation(cards);

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var deck = ResolveOccupiedDeck();
            var deckReady = deck != null;
            var members = deckReady
                ? DeckService.GetOrderedMembers(deck.deckId, cards)
                : new List<CardEntity>();

            var dirty = false;
            var grantOpen = false;

            foreach (var line in st.productionLines)
            {
                if (line == null) continue;
                var def = ProductionLineCatalog.Get(line.lineId);
                if (def == null)
                {
                    dirty |= SetState(line, ProductionLineState.Idle);
                    continue;
                }

                var ready = Classify(line, def, deckReady, members, out var recipe, out var cycleSeconds);
                if (ready != LineReady.Ok)
                {
                    var blocked = ready is LineReady.NoDeck or LineReady.DeckBusyElsewhere or LineReady.SkillGate or LineReady.TierTooLow;
                    dirty |= SetState(line, blocked ? ProductionLineState.PausedBlock : ProductionLineState.Idle);
                    continue;
                }

                if (line.lastSettledAtUtc <= 0)
                {
                    line.lastSettledAtUtc = now;
                    dirty = true;
                }

                var elapsed = Math.Min(now - line.lastSettledAtUtc, maxElapsedPerLine);
                var cyclesPossible = (int)(elapsed / Math.Max(1, cycleSeconds));
                if (cyclesPossible <= 0)
                {
                    dirty |= SetState(line, ProductionLineState.Running);
                    continue;
                }

                var produced = 0;
                var blockedRun = false;
                for (var i = 0; i < cyclesPossible; i++)
                {
                    if (!offline && !ProductionService.WarehouseHasRoom())
                    {
                        blockedRun = true;
                        break;
                    }

                    var inputMinQ = offline
                        ? EconomyConstants.DefaultQuality
                        : ProductionService.RecipeInputMinQuality(recipe);
                    if (!ProductionService.TryConsumeRecipeBatch(recipe))
                    {
                        blockedRun = true;
                        break;
                    }

                    var qty = offline ? OfflineRules.ScaleReward(recipe.outputQty, yield) : recipe.outputQty;
                    if (qty > 0)
                    {
                        if (offline)
                        {
                            IdleSettlementService.EnqueuePending(
                                recipe.outputDefId, qty, EconomyConstants.DefaultQuality, "line:" + line.lineId);
                        }
                        else
                        {
                            var facility = ModuleLevel(recipe.facilityModuleId);
                            var skill = Mathf.Max(1, Mathf.RoundToInt(
                                ProgressionService.TeamPower(members, ProfessionSkill.Craft, facility)));
                            var mastery = OfflineRules.MasteryFor(st, recipe.recipeId);
                            var quality = (int)QualityRules.Roll(
                                skill, facility, inputMinQ, mastery, () => UnityEngine.Random.value);
                            var item = ItemFactory.FromDef(recipe.outputDefId, qty, quality);
                            if (!ProductionService.TryAddLocal(item))
                            {
                                blockedRun = true;
                                break;
                            }
                        }
                    }

                    produced++;
                }

                // Discard missed cycles (including any blocked tail) so a stall never hoards backlog.
                line.lastSettledAtUtc += (long)cyclesPossible * Math.Max(1, cycleSeconds);
                dirty = true;
                SetState(line, blockedRun ? ProductionLineState.PausedBlock : ProductionLineState.Running);

                if (produced > 0)
                {
                    if (ownGrantScope && !grantOpen)
                    {
                        ProgressionService.BeginGrant();
                        grantOpen = true;
                    }

                    var seconds = produced * Math.Max(1, recipe.cycleSeconds);
                    ProgressionService.GrantProfessionToParty(
                        members, ProfessionSkill.Craft, recipe.requiredSkillLevel, seconds);
                    ProgressionService.GrantCommander(ProgressionCatalog.CommanderCraftXp * produced);
                    OfflineRules.AddMastery(st, recipe.recipeId);
                    Debug.Log($"[LINE] {line.lineId} +{produced}x {recipe.outputDefId} ({(offline ? "offline" : "online")})");
                }
            }

            if (grantOpen)
                ProgressionService.EndGrant(presentUi: false);

            if (dirty)
                IdleSettlementService.Save();
        }

        // ---------------------------------------------------------------- UI query

        public static LineView GetView(ProductionLineInstance line, IList<CardEntity> cards)
        {
            EnsureLoaded();
            var view = new LineView();
            var def = ProductionLineCatalog.Get(line?.lineId);
            if (def == null || line == null)
                return view;

            var deck = ResolveOccupiedDeck();
            var deckReady = deck != null;
            var members = deckReady
                ? DeckService.GetOrderedMembers(deck.deckId, cards)
                : new List<CardEntity>();

            view.Ready = Classify(line, def, deckReady, members, out var recipe, out var cycleSeconds);
            view.ActiveRecipe = recipe;
            view.CycleSeconds = cycleSeconds;
            view.State = line.state;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (view.Ready == LineReady.Ok && cycleSeconds > 0 && line.lastSettledAtUtc > 0)
                view.Progress = Mathf.Clamp01((now - line.lastSettledAtUtc) / (float)cycleSeconds);

            FillReasonText(view, line);
            return view;
        }

        // ---------------------------------------------------------------- internals

        private static LineReady Classify(
            ProductionLineInstance line, ProductionLineDef def, bool deckReady, IList<CardEntity> members,
            out RecipeDef recipe, out int cycleSeconds)
        {
            recipe = null;
            cycleSeconds = 0;
            if (!line.built) return LineReady.NotBuilt;
            if (!line.enabled) return LineReady.Disabled;

            recipe = RecipeCatalog.Get(line.activeRecipeId);
            if (recipe == null || !IsRecipeAllowed(def, line.activeRecipeId))
                return LineReady.NoRecipe;
            if (line.tier < recipe.requiredLineTier)
                return LineReady.TierTooLow;

            if (string.IsNullOrEmpty(ProductionDeckId))
                return LineReady.NoDeck;
            if (!deckReady)
                return LineReady.DeckBusyElsewhere;
            if (!ProgressionService.LeaderMeetsGate(members, recipe.requiredProfession, recipe.requiredSkillLevel))
                return LineReady.SkillGate;

            var facility = ModuleLevel(recipe.facilityModuleId);
            cycleSeconds = Math.Max(1, (int)Math.Round(
                ProgressionService.AdjustedCycleSeconds(recipe.cycleSeconds, members, ProfessionSkill.Craft, facility)));
            return LineReady.Ok;
        }

        private static void FillReasonText(LineView view, ProductionLineInstance line)
        {
            switch (view.Ready)
            {
                case LineReady.NotBuilt:
                    view.ReasonEn = "Not built"; view.ReasonZh = "未建造"; break;
                case LineReady.Disabled:
                    view.ReasonEn = "Disabled"; view.ReasonZh = "已停用"; break;
                case LineReady.NoRecipe:
                    view.ReasonEn = "No recipe selected"; view.ReasonZh = "未选择配方"; break;
                case LineReady.TierTooLow:
                    view.ReasonEn = "Line tier too low for this recipe"; view.ReasonZh = "产线等级不足以生产该配方"; break;
                case LineReady.NoDeck:
                    view.ReasonEn = "Assign an industry fleet"; view.ReasonZh = "请分配工业舰队"; break;
                case LineReady.DeckBusyElsewhere:
                    view.ReasonEn = "Industry fleet is busy elsewhere"; view.ReasonZh = "工业舰队被其它行动占用"; break;
                case LineReady.SkillGate:
                    view.ReasonEn = "Crew crafting skill too low"; view.ReasonZh = "舰队制造技能不足"; break;
                default:
                    if (line.state == ProductionLineState.PausedBlock)
                    {
                        view.ReasonEn = "Blocked: missing materials or warehouse full";
                        view.ReasonZh = "阻塞：缺少材料或仓库已满";
                    }
                    else
                    {
                        view.ReasonEn = "Running"; view.ReasonZh = "运行中";
                    }

                    break;
            }
        }

        private static bool SetState(ProductionLineInstance line, ProductionLineState state)
        {
            if (line.state == state) return false;
            line.state = state;
            return true;
        }

        /// <summary>Any built + enabled line with a valid selected recipe wants the fleet occupied.</summary>
        private static bool AnyLineWantsOccupation()
        {
            var st = IdleSettlementService.State;
            if (st?.productionLines == null) return false;
            foreach (var line in st.productionLines)
            {
                if (line == null || !line.built || !line.enabled) continue;
                var def = ProductionLineCatalog.Get(line.lineId);
                if (def == null) continue;
                var recipe = RecipeCatalog.Get(line.activeRecipeId);
                if (recipe == null || !IsRecipeAllowed(def, line.activeRecipeId)) continue;
                if (line.tier < recipe.requiredLineTier) continue;
                return true;
            }

            return false;
        }

        private static void SyncDeckOccupation(IList<CardEntity> cards)
        {
            if (!DeckService.IsLoaded) return;
            var deckId = IdleSettlementService.State.productionDeckId ?? "";
            var deck = FindDeck(deckId);
            var isProd = deck?.action != null
                         && deck.action.actionType == DeckActionType.Production
                         && deck.IsActionBusy;
            var wantOccupy = deck != null && deck.unlocked && deck.MemberCount > 0 && AnyLineWantsOccupation();

            if (wantOccupy)
            {
                if (!isProd && !deck.IsActionBusy)
                    DeckService.TryStart(deckId, DeckActionType.Production, OccupyTargetId, cards);
            }
            else if (isProd)
            {
                DeckService.TryStop(deckId);
            }

            // Clean up stray Production occupancy on any deck that is no longer the industry fleet.
            foreach (var d in DeckService.GetDecks())
            {
                if (d?.action == null) continue;
                if (d.action.actionType == DeckActionType.Production && d.deckId != deckId && d.IsActionBusy)
                    DeckService.TryStop(d.deckId);
            }
        }

        /// <summary>The industry fleet only when it is actually occupied by a Production action.</summary>
        private static DeckEntity ResolveOccupiedDeck()
        {
            var deck = FindDeck(IdleSettlementService.State.productionDeckId ?? "");
            if (deck?.action != null
                && deck.action.actionType == DeckActionType.Production
                && deck.action.status == DeckActionStatus.Running)
                return deck;
            return null;
        }

        private static DeckEntity FindDeck(string deckId)
        {
            if (string.IsNullOrEmpty(deckId)) return null;
            foreach (var d in DeckService.GetDecks())
                if (d != null && d.deckId == deckId)
                    return d;
            return null;
        }

        public static bool IsRecipeAllowed(ProductionLineDef def, string recipeId)
        {
            if (def?.allowedRecipeIds == null || string.IsNullOrEmpty(recipeId)) return false;
            foreach (var id in def.allowedRecipeIds)
                if (id == recipeId)
                    return true;
            return false;
        }

        private static RecipeInput[] ScaleCost(RecipeInput[] baseCost, int multiplier)
        {
            if (baseCost == null) return Array.Empty<RecipeInput>();
            var mult = Mathf.Max(1, multiplier);
            var result = new RecipeInput[baseCost.Length];
            for (var i = 0; i < baseCost.Length; i++)
            {
                var input = baseCost[i];
                result[i] = new RecipeInput
                {
                    itemDefId = input.itemDefId,
                    quantity = input.quantity * mult,
                    minQuality = input.minQuality
                };
            }

            return result;
        }

        private static int ModuleLevel(string moduleId)
        {
            ShipService.EnsureReady();
            if (ShipService.State?.modules == null || string.IsNullOrEmpty(moduleId)) return 0;
            foreach (var m in ShipService.State.modules)
                if (m != null && m.moduleId == moduleId)
                    return m.level;
            return 0;
        }
    }
}
