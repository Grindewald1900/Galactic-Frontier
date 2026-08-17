using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    public static class WorldRules
    {
        public static PlayerWorldState CreateNewPlayerWorld()
        {
            var world = new PlayerWorldState
            {
                currentSectorId = WorldConstants.SectorId,
                regions = new List<RegionRuntimeState>(),
                navX = SectorMapCatalog.SpawnX,
                navY = SectorMapCatalog.SpawnY,
                knownBodyIds = new List<string> { "body_outer_haven" }
            };
            foreach (var cfg in RegionCatalog.All)
            {
                world.regions.Add(new RegionRuntimeState
                {
                    regionId = cfg.regionId,
                    cleared = false,
                    clearCount = 0,
                    bossDefeated = false
                });
            }

            world.explorationProgress = ComputeExplorationProgress(world);
            world.count = world.regions.Count;
            return world;
        }

        public static RegionRuntimeState FindRuntime(PlayerWorldState world, string regionId)
        {
            if (world?.regions == null || string.IsNullOrEmpty(regionId))
                return null;
            foreach (var r in world.regions)
            {
                if (r != null && r.regionId == regionId)
                    return r;
            }

            return null;
        }

        public static bool PrerequisitesCleared(PlayerWorldState world, RegionConfig config)
        {
            if (config?.prereqRegionIds == null || config.prereqRegionIds.Length == 0)
                return true;
            foreach (var prereq in config.prereqRegionIds)
            {
                var rt = FindRuntime(world, prereq);
                if (rt == null || !rt.cleared)
                    return false;
            }

            return true;
        }

        public static RegionProgressState GetProgressState(PlayerWorldState world, RegionConfig config)
        {
            if (config == null)
                return RegionProgressState.Locked;
            var rt = FindRuntime(world, config.regionId) ?? new RegionRuntimeState { regionId = config.regionId };

            if (config.bossRegion)
            {
                if (rt.bossDefeated)
                    return RegionProgressState.BossDefeated;
                if (PrerequisitesCleared(world, config))
                    return RegionProgressState.BossAvailable;
                return RegionProgressState.Locked;
            }

            if (rt.cleared)
                return RegionProgressState.Cleared;
            if (PrerequisitesCleared(world, config))
                return RegionProgressState.Challengeable;
            return RegionProgressState.Locked;
        }

        public static RegionView BuildView(PlayerWorldState world, ShipEntity ship, string regionId)
        {
            var config = RegionCatalog.Get(regionId);
            var view = new RegionView
            {
                Config = config,
                Runtime = FindRuntime(world, regionId) ?? new RegionRuntimeState { regionId = regionId ?? "" },
                Progress = GetProgressState(world, config)
            };
            if (config == null)
            {
                view.CanEnter = false;
                view.BlockReasons.Add("Unknown region.");
                return view;
            }

            if (!PrerequisitesCleared(world, config))
            {
                foreach (var prereq in config.prereqRegionIds)
                {
                    var p = RegionCatalog.Get(prereq);
                    view.BlockReasons.Add("Need clear: " + (p?.displayNameEn ?? prereq));
                }
            }

            var gate = ShipRules.MeetsGate(ship, config.shipGate);
            if (!gate.Ok)
            {
                view.ShipGaps.AddRange(gate.Missing);
                foreach (var g in gate.Missing)
                    view.BlockReasons.Add("Ship: " + g);
            }

            var progress = view.Progress;
            view.CanEnter = progress != RegionProgressState.Locked &&
                            PrerequisitesCleared(world, config) &&
                            gate.Ok;
            view.FarmUnlocked = IsFarmUnlocked(world, config);
            return view;
        }

        public static bool IsFarmUnlocked(PlayerWorldState world, RegionConfig config)
        {
            if (config == null) return false;
            var rt = FindRuntime(world, config.regionId);
            if (rt == null) return false;
            if (config.bossRegion)
                return rt.bossDefeated;
            return rt.cleared;
        }

        public static WorldCommandResult RegisterVictory(
            PlayerWorldState world,
            string regionId,
            string encounterId,
            long nowUtc)
        {
            var config = RegionCatalog.Get(regionId);
            if (config == null)
                return WorldCommandResult.Fail("Unknown region.");
            var rt = FindRuntime(world, regionId);
            if (rt == null)
            {
                rt = new RegionRuntimeState { regionId = regionId };
                world.regions ??= new List<RegionRuntimeState>();
                world.regions.Add(rt);
            }

            var enc = EncounterCatalog.Get(encounterId);
            var isBossFight = config.bossRegion || (enc != null && enc.isBoss);

            bool wasFirstClear;
            if (isBossFight)
            {
                wasFirstClear = !rt.bossDefeated;
                rt.bossDefeated = true;
                rt.cleared = true;
                if (wasFirstClear)
                    rt.firstClearedAtUtc = nowUtc;
            }
            else
            {
                wasFirstClear = !rt.cleared;
                if (wasFirstClear)
                {
                    rt.cleared = true;
                    rt.firstClearedAtUtc = nowUtc;
                }
            }

            rt.clearCount++;
            world.explorationProgress = ComputeExplorationProgress(world);
            world.count = world.regions.Count;
            return WorldCommandResult.Ok(wasFirstClear, regionId, encounterId ?? "");
        }

        public static int ComputeExplorationProgress(PlayerWorldState world)
        {
            var total = RegionCatalog.All.Count;
            if (total <= 0) return 0;
            var cleared = 0;
            foreach (var cfg in RegionCatalog.All)
            {
                var rt = FindRuntime(world, cfg.regionId);
                if (rt == null) continue;
                if (cfg.bossRegion)
                {
                    if (rt.bossDefeated) cleared++;
                }
                else if (rt.cleared)
                {
                    cleared++;
                }
            }

            return (int)(100f * cleared / total);
        }

        public static bool IsSectorComplete(PlayerWorldState world)
        {
            var boss = FindRuntime(world, WorldConstants.FrontierBossId);
            return boss != null && boss.bossDefeated;
        }
    }
}
