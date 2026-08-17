using System;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World.Domain;

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

        /// <summary>Card id the player picked as the ship mascot, or empty.</summary>
        public static string MascotCardId
        {
            get
            {
                EnsureReady();
                return State?.mascotCardId ?? "";
            }
        }

        /// <summary>Sets (or clears, with an empty id) the mascot card shown in the ship bay.</summary>
        public static void SetMascot(string cardId)
        {
            EnsureReady();
            if (State == null) return;
            State.mascotCardId = cardId ?? "";
            Save();
        }

        public static int GetModuleLevel(string moduleId)
        {
            EnsureReady();
            if (State?.modules == null || string.IsNullOrEmpty(moduleId))
                return 0;
            foreach (var m in State.modules)
            {
                if (m != null && m.moduleId == moduleId)
                    return m.level;
            }

            return 0;
        }

        public static WorldCommandResult TryUpgradeModule(string moduleId)
        {
            EnsureReady();
            var def = ShipModuleCatalog.Get(moduleId);
            if (def == null)
                return WorldCommandResult.Fail("Unknown module.");

            var current = GetModuleLevel(moduleId);
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

        /// <summary>True when the player can pay scrap + credits for the next module level.</summary>
        public static bool CanAffordModuleUpgrade(string moduleId)
        {
            EnsureReady();
            if (ShipModuleCatalog.Get(moduleId) == null) return false;
            var next = GetModuleLevel(moduleId) + 1;
            return CanAfford(
                ShipRules.ScrapCostForModule(moduleId, next),
                ShipRules.CreditCostForModule(moduleId, next));
        }

        public static bool CanAfford(int scrap, int credit)
        {
            if (credit > 0)
            {
                var credits = DataUtil.Instance?.currentPlayer?.creditPoints ?? 0;
                if (credits < credit) return false;
            }

            if (scrap > 0)
            {
                var items = Economy.ProductionService.GetLocalItems();
                foreach (var item in items)
                    Economy.ItemFactory.NormalizeLegacy(item);
                var have = Economy.Domain.InventoryRules.CountOf(
                    Economy.ItemFactory.ToStacks(items),
                    Economy.Domain.EconomyConstants.ScrapDefId,
                    1);
                if (have < scrap) return false;
            }

            return true;
        }

        private static bool TryPay(int scrap, int credit, out string error)
        {
            error = null;
            if (!CanAfford(scrap, credit))
            {
                if (credit > 0 &&
                    (DataUtil.Instance?.currentPlayer?.creditPoints ?? 0) < credit)
                {
                    error = $"Need {credit} credits.";
                    return false;
                }

                error = scrap > 0 ? $"Need {scrap} scrap." : "Cannot afford upgrade.";
                return false;
            }

            if (credit > 0 &&
                !Market.CurrencyService.TrySpendCredits(credit, out var creditErr))
            {
                error = creditErr;
                return false;
            }

            if (scrap > 0 &&
                !Economy.ProductionService.TryConsumeLocal(
                    Economy.Domain.EconomyConstants.ScrapDefId, scrap, 1))
            {
                if (credit > 0)
                    Market.CurrencyService.AddCredits(credit);
                error = $"Need {scrap} scrap.";
                return false;
            }

            return true;
        }
    }
}
