using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Inventory;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Progression;
using Assets.Resources.Scripts.Progression.Domain;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.Save;
using Assets.Resources.Scripts.World;
using UnityEngine;

namespace Assets.Resources.Scripts.Economy
{
    /// <summary>Starts gather/craft jobs and settles craft cycles.</summary>
    public static class ProductionService
    {
        /// <summary>Fired after the local warehouse list changes so open inventory UI can refresh live.</summary>
        public static event Action WarehouseChanged;

        public static void NotifyWarehouseChanged() => WarehouseChanged?.Invoke();

        public static EconomyCommandResult TryStartGather(string nodeId, IList<CardEntity> cards)
        {
            var node = GatherNodeCatalog.Get(nodeId);
            if (node == null)
                return EconomyCommandResult.Fail("Unknown gather node.");
            if (!WorldService.IsFarmUnlocked(node.regionId) && !IsRegionCleared(node.regionId))
                return EconomyCommandResult.Fail("Region not cleared for gather.");

            var deck = PreferEconomyDeck(cards);
            if (deck == null || deck.MemberCount < 1)
                return EconomyCommandResult.Fail("No free deck with members for gather.");

            var members = DeckService.GetOrderedMembers(deck.deckId, cards);
            if (!ProgressionService.LeaderMeetsGate(members, node.requiredProfession, node.requiredSkillLevel))
                return ProgressionService.SkillGateFailure(
                    node.requiredProfession, node.requiredSkillLevel, members);

            var start = DeckService.TryStart(deck.deckId, DeckActionType.Gather, nodeId, cards);
            if (start.Success)
                OnboardingService.NotifyGatherProgress();
            return start.Success
                ? EconomyCommandResult.Ok()
                : EconomyCommandResult.Fail(start.Message);
        }

        public static ManualCraftJob ActiveManualJob
        {
            get
            {
                IdleSettlementService.EnsureLoaded();
                var job = IdleSettlementService.State?.manualCraftJob;
                return job == null || string.IsNullOrEmpty(job.recipeId) ? null : job;
            }
        }

        public static bool IsManualCrafting => ActiveManualJob != null;

        public static EconomyCommandResult TryStartRecipe(string recipeId, IList<CardEntity> cards = null)
        {
            var recipe = RecipeCatalog.Get(recipeId);
            if (recipe == null)
                return EconomyCommandResult.Fail("Unknown recipe.");
            if (!IsManualRecipeUnlocked(recipe))
                return EconomyCommandResult.Fail("Recipe is locked.");
            if (IsManualCrafting)
                return EconomyCommandResult.Fail("Already crafting another item.");
            if (!WarehouseHasRoom())
                return EconomyCommandResult.Fail("Warehouse full.");
            if (!CanAfford(recipe))
                return EconomyCommandResult.Fail($"Missing materials for {recipe.outputDefId}.");

            var inputMinQ = RecipeInputMinQuality(recipe);
            if (!TryConsumeRecipeBatch(recipe))
                return EconomyCommandResult.Fail("Could not consume materials.");

            IdleSettlementService.EnsureLoaded();
            IdleSettlementService.State.manualCraftJob = new ManualCraftJob
            {
                recipeId = recipe.recipeId,
                lastSettledAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                inputMinQuality = inputMinQ,
                state = ManualCraftState.Running,
                reservedInputs = CloneInputs(recipe.inputs)
            };
            IdleSettlementService.Save();
            OnboardingService.NotifyCraftedOnce();
            return EconomyCommandResult.Ok($"Started crafting {recipe.outputQty}× {recipe.outputDefId}.");
        }

        public static EconomyCommandResult TryStopManualCraft()
        {
            var job = ActiveManualJob;
            if (job == null)
                return EconomyCommandResult.Fail("Nothing is being crafted.");

            RefundReserved(job);
            IdleSettlementService.State.manualCraftJob = null;
            IdleSettlementService.Save();
            return EconomyCommandResult.Ok();
        }

        public static bool IsManualRecipeUnlocked(RecipeDef recipe)
        {
            if (recipe == null) return false;
            if (recipe.requiredLineTier <= 1) return true;
            return ShipServiceModuleLevel(recipe.facilityModuleId) >= recipe.requiredLineTier - 1;
        }

        public static float ResolveManualCycleSeconds(RecipeDef recipe)
        {
            if (recipe == null) return EconomyConstants.CraftCycleSeconds;
            var facility = ShipServiceModuleLevel(recipe.facilityModuleId);
            return ProgressionService.AdjustedCycleSeconds(
                recipe.cycleSeconds, null, ProfessionSkill.Craft, facility);
        }

        public static float ManualProgressRatio()
        {
            var job = ActiveManualJob;
            if (job == null) return 0f;
            if (job.state == ManualCraftState.BlockedFull) return 1f;
            var recipe = RecipeCatalog.Get(job.recipeId);
            var cycle = ResolveManualCycleSeconds(recipe);
            if (cycle <= 0f) return 0f;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var last = job.lastSettledAtUtc > 0 ? job.lastSettledAtUtc : now;
            return Mathf.Clamp01((now - last) / cycle);
        }

        public static int PreviewQualityFloor(string recipeId)
        {
            ComputeManualQualityPreview(recipeId, out var floor, out _);
            return floor;
        }

        public static void SettleManualOnline()
        {
            TrySettleManualJob(maxCycles: 1, offline: false, yield: 1f);
        }

        public static void SettleManualOffline(long elapsedSeconds, float yield)
        {
            if (elapsedSeconds < 5) return;
            var job = ActiveManualJob;
            if (job == null) return;
            var recipe = RecipeCatalog.Get(job.recipeId);
            var cycle = Math.Max(1, (int)Math.Round(ResolveManualCycleSeconds(recipe)));
            var cycles = (int)(elapsedSeconds / cycle);
            if (recipe != null && recipe.kind == RecipeKind.Manufacture)
                cycles = Math.Min(cycles, 1);
            if (cycles <= 0) return;
            TrySettleManualJob(maxCycles: cycles, offline: true, yield: yield);
        }

        public static float ResolveJobCycleSeconds(DeckEntity deck, IList<CardEntity> cards)
        {
            if (deck?.action == null) return EconomyConstants.CraftCycleSeconds;
            var members = DeckService.GetOrderedMembers(deck.deckId, cards);
            if (deck.action.actionType == DeckActionType.Gather)
            {
                var node = GatherNodeCatalog.Get(deck.action.targetId);
                var baseCycle = node != null ? node.cycleSeconds : EconomyConstants.GatherCycleSeconds;
                return ProgressionService.AdjustedCycleSeconds(
                    baseCycle, members, ProfessionSkill.Gather, 0);
            }

            if (deck.action.actionType == DeckActionType.Process
                || deck.action.actionType == DeckActionType.Manufacture)
            {
                var recipeId = deck.action.targetId;
                var payload = deck.action.progressPayload ?? "";
                var parts = payload.Split('|');
                if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
                    recipeId = parts[0];
                var recipe = RecipeCatalog.Get(recipeId);
                var baseCycle = recipe != null ? recipe.cycleSeconds : EconomyConstants.CraftCycleSeconds;
                var facility = recipe != null ? ShipServiceModuleLevel(recipe.facilityModuleId) : 0;
                return ProgressionService.AdjustedCycleSeconds(
                    baseCycle, members, ProfessionSkill.Craft, facility);
            }

            return EconomyConstants.OnlineFarmCycleSeconds;
        }

        public static bool TrySettleCraftCycle(DeckEntity deck, IList<CardEntity> cards)
        {
            if (deck?.action == null) return false;
            if (deck.action.actionType != DeckActionType.Process
                && deck.action.actionType != DeckActionType.Manufacture)
                return false;

            var payload = deck.action.progressPayload ?? "";
            var parts = payload.Split('|');
            var recipeId = parts.Length > 0 && !string.IsNullOrEmpty(parts[0])
                ? parts[0]
                : deck.action.targetId;
            var inputMinQ = EconomyConstants.DefaultQuality;
            if (parts.Length > 1)
                int.TryParse(parts[1], out inputMinQ);

            var recipe = RecipeCatalog.Get(recipeId);
            if (recipe == null)
            {
                DeckService.TryStop(deck.deckId);
                return false;
            }

            var inv = GetLocalItems();
            if (inv == null) return false;
            if (inv.Count >= EconomyConstants.WarehouseCapacity)
            {
                ActionScheduler.TryPauseBlock(
                    DeckService.State, deck.deckId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                DeckService.Save();
                return false;
            }

            var facility = ShipServiceModuleLevel(recipe.facilityModuleId);
            var members = DeckService.GetOrderedMembers(deck.deckId, cards);
            var teamPower = ProgressionService.TeamPower(members, ProfessionSkill.Craft, facility);
            var skill = Mathf.Max(1, Mathf.RoundToInt(teamPower));
            IdleSettlementService.EnsureLoaded();
            var mastery = OfflineRules.MasteryFor(IdleSettlementService.State, recipeId);
            var quality = (int)QualityRules.Roll(skill, facility, inputMinQ, mastery, () => UnityEngine.Random.value);

            var output = ItemFactory.FromDef(recipe.outputDefId, recipe.outputQty, quality);
            if (!TryAddLocal(output))
            {
                ActionScheduler.TryPauseBlock(
                    DeckService.State, deck.deckId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                DeckService.Save();
                return false;
            }

            Debug.Log($"[CRAFT] +{recipe.outputQty} {recipe.outputDefId} Q{quality} → warehouse");

            OfflineRules.AddMastery(IdleSettlementService.State, recipeId);
            IdleSettlementService.Save();
            deck.action.lastSettledAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            ProgressionService.BeginGrant();
            var cycleSeconds = Math.Max(1, recipe.cycleSeconds);
            ProgressionService.GrantProfessionToParty(
                members, ProfessionSkill.Craft, recipe.requiredSkillLevel, cycleSeconds);
            ProgressionService.GrantCommander(ProgressionCatalog.CommanderCraftXp);
            ProgressionService.EndGrant(presentUi: false);

            // Keep running for next cycle — re-consume next tick if materials remain
            // For MVP: auto-stop after one cycle (Manufacture) or continue Process if mats remain
            if (recipe.kind == RecipeKind.Manufacture || !CanAfford(recipe))
            {
                DeckService.TryStop(deck.deckId);
            }
            else
            {
                // Pre-consume next batch
                var stacks = ItemFactory.ToStacks(GetLocalItems());
                foreach (var input in recipe.inputs)
                {
                    if (input == null) continue;
                    InventoryRules.TryConsume(stacks, input.itemDefId, input.quantity, input.minQuality);
                }

                var nextMin = InventoryRules.MinInputQuality(stacks, recipe.inputs);
                ReplaceLocalFromStacks(stacks);
                deck.action.progressPayload = $"{recipeId}|{nextMin}";
                DeckService.Save();
            }

            return true;
        }

        public static bool CanAfford(RecipeDef recipe)
        {
            if (recipe?.inputs == null) return false;
            var stacks = ItemFactory.ToStacks(GetLocalItems());
            foreach (var input in recipe.inputs)
            {
                if (input == null) continue;
                if (InventoryRules.CountOf(stacks, input.itemDefId, input.minQuality) < input.quantity)
                    return false;
            }

            return true;
        }

        /// <summary>True when the local warehouse can still accept new stacks.</summary>
        public static bool WarehouseHasRoom() => LocalItemCount() < EconomyConstants.WarehouseCapacity;

        public static int LocalItemCount() => GetLocalItems()?.Count ?? 0;

        /// <summary>Lowest quality among the inputs a recipe currently draws from the warehouse.</summary>
        public static int RecipeInputMinQuality(RecipeDef recipe)
        {
            if (recipe?.inputs == null) return EconomyConstants.DefaultQuality;
            var stacks = ItemFactory.ToStacks(GetLocalItems());
            return InventoryRules.MinInputQuality(stacks, recipe.inputs);
        }

        /// <summary>Atomically consumes one batch of a recipe's inputs from the local warehouse.</summary>
        public static bool TryConsumeRecipeBatch(RecipeDef recipe)
        {
            if (recipe?.inputs == null) return false;
            var items = GetLocalItems();
            if (items == null) return false;
            foreach (var item in items)
                ItemFactory.NormalizeLegacy(item);
            var stacks = ItemFactory.ToStacks(items);
            foreach (var input in recipe.inputs)
            {
                if (input == null) continue;
                if (InventoryRules.CountOf(stacks, input.itemDefId, input.minQuality) < input.quantity)
                    return false;
            }

            foreach (var input in recipe.inputs)
            {
                if (input == null) continue;
                if (!InventoryRules.TryConsume(stacks, input.itemDefId, input.quantity, input.minQuality))
                    return false;
            }

            ReplaceLocalFromStacks(stacks);
            return true;
        }

        public static string PreviewQuality(string recipeId, IList<CardEntity> cards = null)
        {
            ComputeManualQualityPreview(recipeId, out var floor, out var ceiling);
            if (floor <= 0) return "—";
            return floor == ceiling ? $"Q{floor}" : $"Q{floor}–Q{ceiling}";
        }

        private static void ComputeManualQualityPreview(string recipeId, out int floor, out int ceiling)
        {
            floor = 0;
            ceiling = 0;
            var recipe = RecipeCatalog.Get(recipeId);
            if (recipe == null) return;
            var stacks = ItemFactory.ToStacks(GetLocalItems());
            var inputMin = InventoryRules.MinInputQuality(stacks, recipe.inputs);
            var facility = ShipServiceModuleLevel(recipe.facilityModuleId);
            var skill = Mathf.Max(1, Mathf.RoundToInt(
                ProgressionService.TeamPower(null, ProfessionSkill.Craft, facility)));
            IdleSettlementService.EnsureLoaded();
            var mastery = OfflineRules.MasteryFor(IdleSettlementService.State, recipeId);
            var score = QualityRules.ComputeScore(skill, facility, inputMin, mastery);
            floor = QualityRules.FloorFromInputs(inputMin, facility);
            ceiling = Math.Max(floor, QualityRules.CeilingFromScore(score));
        }

        private static void TrySettleManualJob(int maxCycles, bool offline, float yield)
        {
            var job = ActiveManualJob;
            if (job == null || maxCycles <= 0) return;
            var recipe = RecipeCatalog.Get(job.recipeId);
            if (recipe == null)
            {
                IdleSettlementService.State.manualCraftJob = null;
                IdleSettlementService.Save();
                return;
            }

            var cycle = Math.Max(1, (int)Math.Round(ResolveManualCycleSeconds(recipe)));
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var last = job.lastSettledAtUtc > 0 ? job.lastSettledAtUtc : now;
            if (!offline && now - last < cycle && job.state != ManualCraftState.BlockedFull)
                return;

            var produced = 0;
            for (var i = 0; i < maxCycles; i++)
            {
                if (!offline && !WarehouseHasRoom())
                {
                    job.state = ManualCraftState.BlockedFull;
                    IdleSettlementService.Save();
                    return;
                }

                var inputMinQ = job.inputMinQuality > 0 ? job.inputMinQuality : EconomyConstants.DefaultQuality;
                var facility = ShipServiceModuleLevel(recipe.facilityModuleId);
                var skill = Mathf.Max(1, Mathf.RoundToInt(
                    ProgressionService.TeamPower(null, ProfessionSkill.Craft, facility)));
                IdleSettlementService.EnsureLoaded();
                var mastery = OfflineRules.MasteryFor(IdleSettlementService.State, recipe.recipeId);
                var quality = (int)QualityRules.Roll(skill, facility, inputMinQ, mastery, () => UnityEngine.Random.value);
                var qty = offline ? OfflineRules.ScaleReward(recipe.outputQty, yield) : recipe.outputQty;
                if (qty > 0)
                {
                    if (offline)
                    {
                        IdleSettlementService.EnqueuePending(
                            recipe.outputDefId, qty, quality, "manual:" + recipe.recipeId);
                    }
                    else
                    {
                        var output = ItemFactory.FromDef(recipe.outputDefId, qty, quality);
                        if (!TryAddLocal(output))
                        {
                            job.state = ManualCraftState.BlockedFull;
                            IdleSettlementService.Save();
                            return;
                        }
                    }
                }

                ClearReserved(job);
                OfflineRules.AddMastery(IdleSettlementService.State, recipe.recipeId);
                produced++;
                job.lastSettledAtUtc = offline ? last + (long)cycle * produced : now;
                job.state = ManualCraftState.Running;

                if (recipe.kind == RecipeKind.Manufacture || !CanAfford(recipe))
                {
                    IdleSettlementService.State.manualCraftJob = null;
                    break;
                }

                var nextMin = RecipeInputMinQuality(recipe);
                if (!TryConsumeRecipeBatch(recipe))
                {
                    IdleSettlementService.State.manualCraftJob = null;
                    break;
                }

                job.inputMinQuality = nextMin;
                job.reservedInputs = CloneInputs(recipe.inputs);
            }

            if (produced > 0)
            {
                ProgressionService.BeginGrant();
                ProgressionService.GrantCommander(ProgressionCatalog.CommanderCraftXp * produced);
                ProgressionService.EndGrant(presentUi: false);
                Debug.Log($"[CRAFT] manual +{produced}x {recipe.outputDefId} ({(offline ? "offline" : "online")})");
            }

            IdleSettlementService.Save();
        }

        private static List<RecipeInput> CloneInputs(RecipeInput[] inputs)
        {
            var list = new List<RecipeInput>();
            if (inputs == null) return list;
            foreach (var input in inputs)
            {
                if (input == null) continue;
                list.Add(new RecipeInput
                {
                    itemDefId = input.itemDefId,
                    quantity = input.quantity,
                    minQuality = input.minQuality
                });
            }

            return list;
        }

        private static void RefundReserved(ManualCraftJob job)
        {
            if (job?.reservedInputs == null) return;
            foreach (var input in job.reservedInputs)
            {
                if (input == null || input.quantity <= 0) continue;
                TryAddLocal(ItemFactory.FromDef(input.itemDefId, input.quantity, input.minQuality));
            }

            ClearReserved(job);
        }

        private static void ClearReserved(ManualCraftJob job)
        {
            if (job == null) return;
            job.reservedInputs ??= new List<RecipeInput>();
            job.reservedInputs.Clear();
        }

        private static bool IsRegionCleared(string regionId)
        {
            foreach (var v in WorldService.GetAllRegionViews())
            {
                if (v.Config != null && v.Config.regionId == regionId)
                    return v.Progress == World.Domain.RegionProgressState.Cleared
                           || v.Progress == World.Domain.RegionProgressState.BossAvailable
                           || v.Progress == World.Domain.RegionProgressState.BossDefeated
                           || v.FarmUnlocked;
            }

            return false;
        }

        private static DeckEntity PreferEconomyDeck(IList<CardEntity> cards)
        {
            DeckService.EnsureReady();
            DeckEntity combat = DeckService.GetActiveCombatDeck();
            var industryDeckId = ProductionLineService.ProductionDeckId;
            foreach (var deck in DeckService.GetDecks())
            {
                if (deck == null || !deck.unlocked) continue;
                if (deck.IsActionBusy) continue;
                if (combat != null && deck.deckId == combat.deckId) continue;
                if (!string.IsNullOrEmpty(industryDeckId) && deck.deckId == industryDeckId) continue;
                if (deck.MemberCount > 0)
                    return deck;
            }

            // Fallback: any unlocked idle deck with members (still never the dedicated industry fleet)
            foreach (var deck in DeckService.GetDecks())
            {
                if (deck == null || !deck.unlocked || deck.IsActionBusy) continue;
                if (!string.IsNullOrEmpty(industryDeckId) && deck.deckId == industryDeckId) continue;
                if (deck.MemberCount > 0)
                    return deck;
            }

            return null;
        }

        private static DeckEntity FindDeck(string deckId)
        {
            foreach (var d in DeckService.GetDecks())
                if (d != null && d.deckId == deckId)
                    return d;
            return null;
        }

        private static int ShipServiceModuleLevel(string moduleId)
        {
            ShipService.EnsureReady();
            if (ShipService.State?.modules == null || string.IsNullOrEmpty(moduleId)) return 0;
            foreach (var m in ShipService.State.modules)
            {
                if (m != null && m.moduleId == moduleId)
                    return m.level;
            }

            return 0;
        }

        public static List<ItemEntity> GetLocalItems()
        {
            if (ItemManager.Instance != null && ItemManager.Instance.HasLoaded)
                return ItemManager.Instance.GetItems();
            return DataUtil.Instance?.LoadInventory(InventoryStore.Local) ?? new List<ItemEntity>();
        }

        public static bool TryAddLocal(ItemEntity item)
        {
            if (item == null) return false;
            if (ItemManager.Instance != null && ItemManager.Instance.HasLoaded)
                return ItemManager.Instance.AddItem(item);

            var list = DataUtil.Instance?.LoadInventory(InventoryStore.Local) ?? new List<ItemEntity>();
            var stacks = ItemFactory.ToStacks(list);
            if (!InventoryRules.TryMerge(stacks, ItemFactory.ToStack(item), EconomyConstants.WarehouseCapacity))
                return false;
            list.Clear();
            foreach (var s in stacks)
                list.Add(ItemFactory.FromStack(s));
            DataUtil.Instance?.SaveInventory(InventoryStore.Local, list, touchMeta: true);
            NotifyWarehouseChanged();
            return true;
        }

        public static bool TryConsumeLocal(string itemDefId, int quantity, int minQuality = 1)
        {
            if (string.IsNullOrEmpty(itemDefId) || quantity <= 0) return false;
            var items = GetLocalItems();
            foreach (var item in items)
                ItemFactory.NormalizeLegacy(item);
            var stacks = ItemFactory.ToStacks(items);
            if (!InventoryRules.TryConsume(stacks, itemDefId, quantity, minQuality))
                return false;
            ReplaceLocalFromStacks(stacks);
            return true;
        }

        private static List<ItemEntity> CloneItems(List<ItemEntity> source)
        {
            var list = new List<ItemEntity>();
            if (source == null) return list;
            foreach (var item in source)
            {
                if (item == null) continue;
                list.Add(item.CloneOwnership(item.isRemote));
            }

            return list;
        }

        private static void RestoreLocal(List<ItemEntity> snapshot)
        {
            ReplaceLocalFromStacks(ItemFactory.ToStacks(snapshot));
        }

        private static void ReplaceLocalFromStacks(List<InventoryStack> stacks)
        {
            var list = new List<ItemEntity>();
            foreach (var s in stacks)
                list.Add(ItemFactory.FromStack(s));

            if (ItemManager.Instance != null && ItemManager.Instance.HasLoaded)
            {
                var current = ItemManager.Instance.GetItems();
                current.Clear();
                current.AddRange(list);
                ItemManager.Instance.RefreshSlotsFromMemory();
                DataUtil.Instance?.SaveInventory(InventoryStore.Local, current, touchMeta: true);
            }
            else
            {
                DataUtil.Instance?.SaveInventory(InventoryStore.Local, list, touchMeta: true);
                NotifyWarehouseChanged();
            }
        }
    }
}
