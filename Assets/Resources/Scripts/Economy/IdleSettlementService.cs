using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using UnityEngine;

namespace Assets.Resources.Scripts.Economy
{
    /// <summary>Offline settlement + pending loot claim (systems/04).</summary>
    public static class IdleSettlementService
    {
        public static PlayerIdleState State { get; private set; }
        public static bool IsLoaded => State != null;

        public static void Clear() => State = null;

        public static void EnsureLoaded(DataUtil dataUtil = null)
        {
            var util = dataUtil ?? DataUtil.Instance;
            if (util == null)
            {
                State ??= new PlayerIdleState();
                return;
            }

            if (State != null) return;
            var loaded = util.LoadIdleState();
            State = loaded ?? new PlayerIdleState
            {
                lastSeenAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                pendingLoot = new List<PendingLootEntry>(),
                mastery = new List<RecipeMasteryEntry>()
            };
            if (loaded == null)
                util.SaveIdleState(State, touchMeta: false);
        }

        public static void CreateForNewPlayer(DataUtil dataUtil)
        {
            State = new PlayerIdleState
            {
                lastSeenAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                pendingLoot = new List<PendingLootEntry>(),
                mastery = new List<RecipeMasteryEntry>()
            };
            dataUtil.SaveIdleState(State, touchMeta: false);
        }

        public static void Save(DataUtil dataUtil = null)
        {
            if (State == null) return;
            var util = dataUtil ?? DataUtil.Instance;
            util?.SaveIdleState(State, touchMeta: true);
        }

        public static void OnAppPause()
        {
            EnsureLoaded();
            State.lastPauseAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            State.lastSeenAtUtc = State.lastPauseAtUtc;
            Save();
        }

        public static void OnAppResume(IList<CardEntity> cards = null)
        {
            EnsureLoaded();
            WorldService.EnsureReady();
            ShipService.EnsureReady();
            DeckService.EnsureReady();

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var last = State.lastSeenAtUtc > 0 ? State.lastSeenAtUtc : now;
            var elapsed = Math.Max(0, now - last);
            SettleOffline(elapsed, cards);
            DurabilityService.TryAutoRepairAll();
            State.lastSeenAtUtc = now;
            Save();
        }

        public static void TickOnlineSeen()
        {
            EnsureLoaded();
            State.lastSeenAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public static int PendingCount => State?.pendingLoot?.Count ?? 0;

        public static EconomyCommandResult ClaimAllPending()
        {
            EnsureLoaded();
            if (State.pendingLoot == null || State.pendingLoot.Count == 0)
                return EconomyCommandResult.Fail("No pending loot.");

            var remaining = new List<PendingLootEntry>();
            foreach (var loot in State.pendingLoot)
            {
                if (loot == null) continue;
                var item = ItemFactory.FromDef(loot.itemDefId, loot.quantity, loot.quality);
                if (loot.isEquipment && loot.maxDurability > 0)
                {
                    item.maxDurability = loot.maxDurability;
                    item.durability = loot.maxDurability;
                }

                if (!ProductionService.TryAddLocal(item))
                    remaining.Add(loot);
            }

            State.pendingLoot = remaining;
            Save();
            return remaining.Count == 0
                ? EconomyCommandResult.Ok()
                : EconomyCommandResult.Fail("Warehouse full; some loot remains pending.");
        }

        private static void SettleOffline(long elapsedSeconds, IList<CardEntity> cards)
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

            var cap = OfflineRules.EffectiveCapSeconds(ShipService.State?.level ?? 1, cargo);
            var effective = Math.Min(elapsedSeconds, cap);
            var yield = OfflineRules.YieldRatio(effective, cap);

            if (!DeckService.IsLoaded) return;
            foreach (var deck in DeckService.GetBusyDecks())
            {
                if (deck?.action == null) continue;
                if (deck.action.status != DeckActionStatus.Running) continue;

                var cycle = EconomyConstants.GatherCycleSeconds;
                if (deck.action.actionType == DeckActionType.Gather)
                {
                    var node = GatherNodeCatalog.Get(deck.action.targetId);
                    if (node != null) cycle = node.cycleSeconds;
                    var cycles = (int)(effective / Math.Max(1, cycle));
                    if (cycles <= 0) continue;
                    DurabilityService.ApplyGatherWear(node?.riskLevel ?? 1); // full wear offline
                    var qty = OfflineRules.ScaleReward((node?.outputQty ?? 1) * cycles, yield);
                    if (qty > 0)
                        EnqueueLoot(node?.outputDefId ?? "mat_scrap", node?.outputQuality ?? 2, qty, false, 0);
                }
                else if (deck.action.actionType == DeckActionType.AutoCombat)
                {
                    cycle = EconomyConstants.OnlineFarmCycleSeconds;
                    var cycles = (int)(effective / Math.Max(1, cycle));
                    if (cycles <= 0) continue;
                    DurabilityService.ApplyCombatWearToEquipped(cards);
                    var qty = OfflineRules.ScaleReward(World.Domain.WorldConstants.FarmLootQuantity * cycles, yield);
                    if (qty > 0)
                        EnqueueLoot(EconomyConstants.ScrapDefId, EconomyConstants.DefaultQuality, qty, false, 0);
                }
                else if (deck.action.actionType == DeckActionType.Process
                         || deck.action.actionType == DeckActionType.Manufacture)
                {
                    // Offline craft: one synthetic output scaled (materials already consumed online)
                    cycle = EconomyConstants.CraftCycleSeconds;
                    var cycles = (int)(effective / Math.Max(1, cycle));
                    if (cycles <= 0) continue;
                    var recipe = RecipeCatalog.Get(deck.action.targetId);
                    if (recipe == null) continue;
                    var qty = OfflineRules.ScaleReward(recipe.outputQty * Math.Min(cycles, 1), yield);
                    if (qty > 0)
                    {
                        var def = ItemCatalog.Get(recipe.outputDefId);
                        EnqueueLoot(
                            recipe.outputDefId,
                            EconomyConstants.DefaultQuality,
                            qty,
                            recipe.outputIsEquipment,
                            def?.baseMaxDurability ?? 0);
                    }

                    DeckService.TryStop(deck.deckId);
                }
            }
        }

        public static void EnqueuePending(string defId, int qty, int quality, string source = "")
        {
            EnsureLoaded();
            var def = ItemCatalog.Get(defId);
            var equipment = def != null &&
                            (def.category == ItemCategory.Equipment || def.category == ItemCategory.ShipModule);
            EnqueueLoot(defId, quality, qty, equipment, def?.baseMaxDurability ?? 0);
            if (!string.IsNullOrEmpty(source))
                Debug.Log($"[IDLE] pending enqueue source={source} {defId} x{qty} Q{quality}");
        }

        private static void EnqueueLoot(string defId, int quality, int qty, bool equipment, int maxDura)
        {
            if (string.IsNullOrEmpty(defId) || qty <= 0) return;
            State.pendingLoot ??= new List<PendingLootEntry>();
            var def = ItemCatalog.Get(defId);
            if (!equipment)
            {
                foreach (var e in State.pendingLoot)
                {
                    if (e != null && e.itemDefId == defId && e.quality == quality && !e.isEquipment)
                    {
                        e.quantity += qty;
                        return;
                    }
                }
            }

            State.pendingLoot.Add(new PendingLootEntry
            {
                itemDefId = defId,
                quality = quality,
                quantity = qty,
                displayName = def?.displayNameEn ?? defId,
                isEquipment = equipment,
                maxDurability = maxDura
            });
        }
    }
}
