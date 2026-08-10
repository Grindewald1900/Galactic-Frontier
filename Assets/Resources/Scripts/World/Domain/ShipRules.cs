using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    public static class ShipRules
    {
        public static ShipEntity CreateStarterShip()
        {
            var ship = new ShipEntity
            {
                shipId = Guid.NewGuid().ToString("N"),
                displayName = "Frontier Skiff",
                level = 1,
                stats = BaseStatsForLevel(1),
                modules = new List<ShipModuleState>()
            };
            foreach (var def in ShipModuleCatalog.All)
                ship.modules.Add(new ShipModuleState { moduleId = def.moduleId, level = 0 });
            RecomputeStats(ship);
            return ship;
        }

        public static ShipStats BaseStatsForLevel(int level)
        {
            var lv = Math.Max(1, level);
            return new ShipStats
            {
                range = lv - 1,
                energy = Math.Max(0, lv - 1),
                hull = Math.Max(0, lv - 1),
                entropyResist = Math.Max(0, (lv - 1) / 2),
                cargo = Math.Max(0, lv - 1),
                scan = Math.Max(0, (lv - 1) / 2),
                lifeSupport = Math.Max(0, (lv - 1) / 2)
            };
        }

        public static void RecomputeStats(ShipEntity ship)
        {
            if (ship == null) return;
            var stats = BaseStatsForLevel(ship.level);
            if (ship.modules != null)
            {
                foreach (var mod in ship.modules)
                {
                    if (mod == null || mod.level <= 0) continue;
                    var def = ShipModuleCatalog.Get(mod.moduleId);
                    if (def == null) continue;
                    var bonus = mod.level * Math.Max(1, def.statPerLevel);
                    ApplyStat(stats, def.primaryStat, bonus);
                }
            }

            ship.stats = stats;
            ship.count = ship.modules?.Count ?? 0;
        }

        public static ShipStats GetEffectiveStats(ShipEntity ship)
        {
            if (ship == null) return new ShipStats();
            RecomputeStats(ship);
            return ship.stats ?? new ShipStats();
        }

        public static GateCheckResult MeetsGate(ShipEntity ship, ShipGate gate)
        {
            var result = new GateCheckResult { Ok = true };
            if (gate == null)
                return result;
            var stats = GetEffectiveStats(ship);
            var level = ship?.level ?? 0;

            Check(result, "Level", level, Math.Max(1, gate.level));
            Check(result, "Range", stats.range, gate.range);
            Check(result, "Energy", stats.energy, gate.energy);
            Check(result, "Hull", stats.hull, gate.hull);
            Check(result, "EntropyResist", stats.entropyResist, gate.entropyResist);
            Check(result, "Cargo", stats.cargo, gate.cargo);
            Check(result, "Scan", stats.scan, gate.scan);
            Check(result, "LifeSupport", stats.lifeSupport, gate.lifeSupport);
            result.Ok = result.Missing.Count == 0;
            return result;
        }

        /// <summary>Instant upgrade — caller must verify scrap/credit spend.</summary>
        public static WorldCommandResult TryUpgradeModule(ShipEntity ship, string moduleId)
        {
            if (ship == null)
                return WorldCommandResult.Fail("No ship.");
            var def = ShipModuleCatalog.Get(moduleId);
            if (def == null)
                return WorldCommandResult.Fail("Unknown module.");
            ship.modules ??= new List<ShipModuleState>();
            var state = ship.modules.Find(m => m != null && m.moduleId == moduleId);
            if (state == null)
            {
                state = new ShipModuleState { moduleId = moduleId, level = 0 };
                ship.modules.Add(state);
            }

            state.level++;
            RecomputeStats(ship);
            return WorldCommandResult.Ok();
        }

        public static WorldCommandResult TryUpgradeShipLevel(ShipEntity ship)
        {
            if (ship == null)
                return WorldCommandResult.Fail("No ship.");
            ship.level = Math.Max(1, ship.level) + 1;
            RecomputeStats(ship);
            return WorldCommandResult.Ok();
        }

        public static int ScrapCostForModule(string moduleId, int nextLevel)
        {
            var def = ShipModuleCatalog.Get(moduleId);
            if (def == null) return 0;
            return def.scrapCostPerLevel * Math.Max(1, nextLevel);
        }

        public static int CreditCostForModule(string moduleId, int nextLevel)
        {
            var def = ShipModuleCatalog.Get(moduleId);
            if (def == null) return 0;
            return def.creditCostPerLevel * Math.Max(1, nextLevel);
        }

        public static int ScrapCostForShipLevel(int nextLevel) => 10 * Math.Max(1, nextLevel);
        public static int CreditCostForShipLevel(int nextLevel) => 25 * Math.Max(1, nextLevel);

        private static void Check(GateCheckResult result, string name, int have, int need)
        {
            if (need <= 0) return;
            if (have < need)
                result.Missing.Add($"{name} {have}/{need}");
        }

        private static void ApplyStat(ShipStats stats, string primary, int bonus)
        {
            switch (primary)
            {
                case "range": stats.range += bonus; break;
                case "energy": stats.energy += bonus; break;
                case "hull": stats.hull += bonus; break;
                case "entropyResist": stats.entropyResist += bonus; break;
                case "cargo": stats.cargo += bonus; break;
                case "scan": stats.scan += bonus; break;
                case "lifeSupport": stats.lifeSupport += bonus; break;
            }
        }
    }
}
