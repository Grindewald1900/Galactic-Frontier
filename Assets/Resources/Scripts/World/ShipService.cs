using System;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Inventory;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.World
{
    public static class ShipService
    {
        public static ShipEntity State { get; private set; }
        public static bool IsLoaded => State != null;

        public static void Clear() => State = null;

        public static void EnsureLoaded(DataUtil dataUtil)
        {
            if (dataUtil == null)
                throw new ArgumentNullException(nameof(dataUtil));
            var loaded = dataUtil.LoadShipState();
            State = loaded != null && !string.IsNullOrEmpty(loaded.shipId)
                ? loaded
                : ShipRules.CreateStarterShip();
            ShipRules.RecomputeStats(State);
            if (loaded == null || string.IsNullOrEmpty(loaded.shipId))
                dataUtil.SaveShipState(State, touchMeta: true);
        }

        public static void CreateForNewPlayer(DataUtil dataUtil)
        {
            State = ShipRules.CreateStarterShip();
            dataUtil.SaveShipState(State, touchMeta: false);
        }

        public static void EnsureReady()
        {
            if (State != null)
            {
                ShipRules.RecomputeStats(State);
                return;
            }

            if (DataUtil.Instance != null)
                EnsureLoaded(DataUtil.Instance);
            else
                State = ShipRules.CreateStarterShip();
        }

        public static void Save(DataUtil dataUtil = null)
        {
            if (State == null) return;
            var util = dataUtil ?? DataUtil.Instance;
            if (util == null) return;
            ShipRules.RecomputeStats(State);
            util.SaveShipState(State, touchMeta: true);
        }

        public static ShipStats GetEffectiveStats()
        {
            EnsureReady();
            return ShipRules.GetEffectiveStats(State);
        }

        public static GateCheckResult MeetsGate(ShipGate gate)
        {
            EnsureReady();
            return ShipRules.MeetsGate(State, gate);
        }

        public static WorldCommandResult TryUpgradeModule(string moduleId)
        {
            EnsureReady();
            var def = ShipModuleCatalog.Get(moduleId);
            if (def == null)
                return WorldCommandResult.Fail("Unknown module.");

            var current = 0;
            if (State.modules != null)
            {
                foreach (var m in State.modules)
                {
                    if (m != null && m.moduleId == moduleId)
                        current = m.level;
                }
            }

            var next = current + 1;
            var scrap = ShipRules.ScrapCostForModule(moduleId, next);
            var credit = ShipRules.CreditCostForModule(moduleId, next);
            if (!TryPay(scrap, credit, out var payError))
                return WorldCommandResult.Fail(payError);

            var result = ShipRules.TryUpgradeModule(State, moduleId);
            if (result.Success)
                Save();
            return result;
        }

        public static WorldCommandResult TryUpgradeShipLevel()
        {
            EnsureReady();
            var next = State.level + 1;
            var scrap = ShipRules.ScrapCostForShipLevel(next);
            var credit = ShipRules.CreditCostForShipLevel(next);
            if (!TryPay(scrap, credit, out var payError))
                return WorldCommandResult.Fail(payError);

            var result = ShipRules.TryUpgradeShipLevel(State);
            if (result.Success)
                Save();
            return result;
        }

        private static bool TryPay(int scrap, int credit, out string error)
        {
            error = null;
            if (DataUtil.Instance?.currentPlayer != null && credit > 0)
            {
                if (!Market.CurrencyService.TrySpendCredits(credit, out var creditErr))
                {
                    error = creditErr;
                    return false;
                }

                // Scrap paid below; credits already spent — if scrap fails we need refund.
            }

            if (scrap > 0)
            {
                var items = ItemManager.Instance != null
                    ? ItemManager.Instance.GetItems()
                    : DataUtil.Instance?.LoadInventory(Assets.Resources.Scripts.Utils.Save.InventoryStore.Local);
                if (items == null)
                {
                    Debug.LogWarning("[SHIP] Inventory missing; skipping scrap spend.");
                }
                else
                {
                    var have = Economy.Domain.InventoryRules.CountOf(
                        Economy.ItemFactory.ToStacks(items),
                        Economy.Domain.EconomyConstants.ScrapDefId,
                        1);
                    if (have < scrap)
                    {
                        if (credit > 0)
                            Market.CurrencyService.AddCredits(credit);
                        error = $"Need {scrap} scrap.";
                        return false;
                    }

                    var stacks = Economy.ItemFactory.ToStacks(items);
                    Economy.Domain.InventoryRules.TryConsume(
                        stacks, Economy.Domain.EconomyConstants.ScrapDefId, scrap, 1);
                    items.Clear();
                    foreach (var s in stacks)
                        items.Add(Economy.ItemFactory.FromStack(s));
                    if (ItemManager.Instance != null)
                        ItemManager.Instance.RefreshSlotsFromMemory();
                    DataUtil.Instance?.SaveInventory(
                        Assets.Resources.Scripts.Utils.Save.InventoryStore.Local, items, touchMeta: false);
                }
            }

            return true;
        }
    }
}
