using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Inventory;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.Save;
using Assets.Resources.Scripts.World;
using UnityEngine;

namespace Assets.Resources.Scripts.Economy
{
    /// <summary>Starts gather/craft jobs and settles craft cycles.</summary>
    public static class ProductionService
    {
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

            var start = DeckService.TryStart(deck.deckId, DeckActionType.Gather, nodeId, cards);
            if (start.Success)
                OnboardingService.NotifyGatherProgress();
            return start.Success
                ? EconomyCommandResult.Ok()
                : EconomyCommandResult.Fail(start.Message);
        }

        public static EconomyCommandResult TryStartRecipe(string recipeId, IList<CardEntity> cards)
        {
            var recipe = RecipeCatalog.Get(recipeId);
            if (recipe == null)
                return EconomyCommandResult.Fail("Unknown recipe.");

            var deck = PreferEconomyDeck(cards);
            if (deck == null || deck.MemberCount < 1)
                return EconomyCommandResult.Fail("No free deck with members for craft.");

            var inv = GetLocalItems();
            if (inv == null)
                return EconomyCommandResult.Fail("Inventory not ready.");

            foreach (var input in recipe.inputs)
            {
                if (input == null) continue;
                if (InventoryRules.CountOf(ItemFactory.ToStacks(inv), input.itemDefId, input.minQuality) < input.quantity)
                    return EconomyCommandResult.Fail($"Missing {input.itemDefId} x{input.quantity}.");
            }

            var snapshot = CloneItems(inv);
            var stacks = ItemFactory.ToStacks(inv);
            var inputMinQ = InventoryRules.MinInputQuality(stacks, recipe.inputs);

            foreach (var input in recipe.inputs)
            {
                if (input == null) continue;
                if (!InventoryRules.TryConsume(stacks, input.itemDefId, input.quantity, input.minQuality))
                    return EconomyCommandResult.Fail("Could not consume materials.");
            }

            ReplaceLocalFromStacks(stacks);

            var actionType = recipe.kind == RecipeKind.Manufacture
                ? DeckActionType.Manufacture
                : DeckActionType.Process;
            var start = DeckService.TryStart(deck.deckId, actionType, recipeId, cards);
            if (!start.Success)
            {
                RestoreLocal(snapshot);
                return EconomyCommandResult.Fail(start.Message);
            }

            deck = FindDeck(deck.deckId);
            if (deck?.action != null)
            {
                deck.action.progressPayload = $"{recipeId}|{inputMinQ}";
                DeckService.Save();
            }

            // First cycle is delivered now so warehouse updates when the player taps Start.
            if (!TrySettleCraftCycle(deck, cards))
            {
                RestoreLocal(snapshot);
                if (deck != null)
                    DeckService.TryStop(deck.deckId);
                return EconomyCommandResult.Fail("Could not deliver craft output.");
            }

            OnboardingService.NotifyCraftedOnce();
            return EconomyCommandResult.Ok($"Crafted {recipe.outputQty}× {recipe.outputDefId}.");
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
            var skill = members.Count;
            var avgLevel = 0;
            if (members.Count > 0)
            {
                foreach (var m in members)
                    if (m != null) avgLevel += m.Level;
                avgLevel /= members.Count;
            }

            skill += avgLevel / 10;
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

        public static string PreviewQuality(string recipeId, IList<CardEntity> cards)
        {
            var recipe = RecipeCatalog.Get(recipeId);
            if (recipe == null) return "—";
            var stacks = ItemFactory.ToStacks(GetLocalItems());
            var inputMin = InventoryRules.MinInputQuality(stacks, recipe.inputs);
            var facility = ShipServiceModuleLevel(recipe.facilityModuleId);
            var deck = PreferEconomyDeck(cards) ?? DeckService.GetActiveCombatDeck();
            var members = deck != null
                ? DeckService.GetOrderedMembers(deck.deckId, cards)
                : new List<CardEntity>();
            var skill = members.Count;
            IdleSettlementService.EnsureLoaded();
            var mastery = OfflineRules.MasteryFor(IdleSettlementService.State, recipeId);
            return QualityRules.PreviewRange(skill, facility, inputMin, mastery);
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
            foreach (var deck in DeckService.GetDecks())
            {
                if (deck == null || !deck.unlocked) continue;
                if (deck.IsActionBusy) continue;
                if (combat != null && deck.deckId == combat.deckId) continue;
                if (deck.MemberCount > 0)
                    return deck;
            }

            // Fallback: any unlocked idle deck with members
            foreach (var deck in DeckService.GetDecks())
            {
                if (deck == null || !deck.unlocked || deck.IsActionBusy) continue;
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
            return true;
        }

        public static bool TryConsumeLocal(string itemDefId, int quantity, int minQuality = 1)
        {
            if (string.IsNullOrEmpty(itemDefId) || quantity <= 0) return false;
            var items = GetLocalItems();
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
            }
        }
    }
}
