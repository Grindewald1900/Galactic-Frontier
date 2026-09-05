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
                knownBodyIds = new List<string> { WorldConstants.OuterHavenBodyId },
                gridNodes = new List<GridNodeRuntime>(),
                gridEdges = new List<GridEdgeRuntime>(),
                sectorNodes = new List<GridNodeRuntime>(),
                unlockedCharts = new List<string>(),
                gridSeeded = false,
                universeSeed = 0,
                planeModifiers = new PlaneModifiersState()
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

        private static bool IsRegionCleared(PlayerWorldState world, string regionId)
        {
            var cfg = RegionCatalog.Get(regionId);
            var rt = FindRuntime(world, regionId);
            if (cfg == null || rt == null) return false;
            return cfg.bossRegion ? rt.bossDefeated : rt.cleared;
        }

        /// <summary>
        /// Spatial challenge gate (doc 20 §2.2): a node can be challenged only if it is the spawn,
        /// already cleared, or lane-adjacent to a cleared node. The linear <c>prereqRegionIds</c>
        /// chain is only a "recommended route" and no longer hard-blocks combat — the ship gate
        /// (doc 13) remains the hard limit.
        /// </summary>
        public static bool CanChallengeRegion(PlayerWorldState world, RegionConfig config)
        {
            if (config == null) return false;
            if (IsRegionCleared(world, config.regionId))
                return true;

            var body = SectorMapCatalog.FindByRegion(config.regionId);
            if (body == null)
                return true; // No map node → do not spatially gate (fallback).
            if (body.bodyId == WorldConstants.OuterHavenBodyId)
                return true; // Spawn node is the initial challengeable entry point.

            foreach (var edge in SectorMapCatalog.Edges)
            {
                if (edge == null) continue;
                if (edge.a != body.bodyId && edge.b != body.bodyId) continue;
                string otherId = edge.a == body.bodyId ? edge.b : edge.a;
                var otherBody = SectorMapCatalog.Get(otherId);
                if (otherBody?.regionIds == null) continue;
                foreach (var rid in otherBody.regionIds)
                {
                    if (IsRegionCleared(world, rid))
                        return true;
                }
            }

            return false;
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
                if (CanChallengeRegion(world, config))
                    return RegionProgressState.BossAvailable;
                return RegionProgressState.Locked;
            }

            if (rt.cleared)
                return RegionProgressState.Cleared;
            if (CanChallengeRegion(world, config))
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

            var gate = ShipRules.MeetsGate(ship, config.shipGate);
            if (!gate.Ok)
            {
                view.ShipGaps.AddRange(gate.Missing);
                foreach (var g in gate.Missing)
                    view.BlockReasons.Add("Ship: " + g);
            }

            var progress = view.Progress;
            if (progress == RegionProgressState.Locked)
                view.BlockReasons.Add("Clear an adjacent node first.");

            view.CanEnter = progress != RegionProgressState.Locked && gate.Ok;
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
