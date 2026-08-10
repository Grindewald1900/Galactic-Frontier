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
                if (DataUtil.Instance.currentPlayer.creditPoints < credit)
                {
                    error = $"Need {credit} credits.";
                    return false;
                }
            }

            if (scrap > 0 && ItemManager.Instance != null)
            {
                var items = ItemManager.Instance.GetItems();
                ItemEntity scrapItem = null;
                foreach (var item in items)
                {
                    if (item != null && item.itemName == WorldConstants.FarmLootItemName)
                    {
                        scrapItem = item;
                        break;
                    }
                }

                if (scrapItem == null || scrapItem.quantity < scrap)
                {
                    error = $"Need {scrap} {WorldConstants.FarmLootItemName}.";
                    return false;
                }

                scrapItem.quantity -= scrap;
                if (scrapItem.quantity <= 0)
                    items.Remove(scrapItem);
                DataUtil.Instance?.SaveInventory(
                    Assets.Resources.Scripts.Utils.Save.InventoryStore.Local, items, touchMeta: false);
            }
            else if (scrap > 0 && ItemManager.Instance == null)
            {
                // Allow upgrade in headless / early boot without inventory manager.
                Debug.LogWarning("[SHIP] ItemManager missing; skipping scrap spend.");
            }

            if (DataUtil.Instance?.currentPlayer != null && credit > 0)
            {
                DataUtil.Instance.currentPlayer.creditPoints -= credit;
                DataUtil.Instance.SavePlayerData(DataUtil.Instance.currentPlayer);
            }

            return true;
        }
    }
}
