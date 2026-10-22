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
                    bossDefeated = false,
                    highestClearedStage = 0
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

        public static bool IsRegionBeaconRestored(PlayerWorldState world, string regionId) =>
            IsRegionCleared(world, regionId);

        private static bool IsRegionCleared(PlayerWorldState world, string regionId)
        {
            var cfg = RegionCatalog.Get(regionId);
            var rt = FindRuntime(world, regionId);
            if (cfg == null || rt == null) return false;
            return cfg.bossRegion ? rt.bossDefeated : rt.cleared;
        }

        /// <summary>
        /// Spatial challenge gate (doc 20 §2.2): a node can be challenged only if it is the spawn,
        /// already cleared, or lane-adjacent to a restored beacon (campaign complete or paid
        /// repair). The linear <c>prereqRegionIds</c> chain is a recommended route only — the
        /// ship gate (doc 13) remains the hard limit.
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
                if (GridVisibility.IsBodyStable(world, otherId))
                    return true;
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
            FillCampaignView(view, world, config);
            return view;
        }

        public static void FillCampaignView(RegionView view, PlayerWorldState world, RegionConfig config)
        {
            if (view == null) return;
            var campaign = NodeCampaignCatalog.Get(config?.regionId);
            var rt = view.Runtime ?? FindRuntime(world, config?.regionId);
            SyncLegacyClear(rt, campaign);
            view.StageCount = campaign?.StageCount ?? 1;
            view.HighestClearedStage = rt?.highestClearedStage ?? 0;
            view.CurrentStage = NodeCampaignCatalog.NextStageIndex(rt, campaign);
            view.CurrentStageDef = NodeCampaignCatalog.CurrentStage(rt, config?.regionId);
            view.CampaignState = GetCampaignState(world, config);
            view.BeaconRestored = rt != null && IsRegionCleared(world, config?.regionId);
            view.GatherUnlocked = IsGatherUnlocked(world, config);
            view.IdleRewardTier = IdleRewardTier(world, config);
        }

        public static NodeCampaignState GetCampaignState(PlayerWorldState world, RegionConfig config)
        {
            if (config == null) return NodeCampaignState.Unscouted;
            var rt = FindRuntime(world, config.regionId) ?? new RegionRuntimeState { regionId = config.regionId };
            var campaign = NodeCampaignCatalog.Get(config.regionId);
            SyncLegacyClear(rt, campaign);
            if (rt.cleared)
                return NodeCampaignState.BeaconRestored;
            if (!CanChallengeRegion(world, config))
                return NodeCampaignState.Unscouted;
            var count = campaign?.StageCount ?? 1;
            var cleared = rt.highestClearedStage;
            if (count > 1 && cleared >= count - 1)
                return NodeCampaignState.BossReady;
            if (count > 1 && cleared >= 2)
                return NodeCampaignState.Staging;
            return NodeCampaignState.Advancing;
        }

        public static bool IsFarmUnlocked(PlayerWorldState world, RegionConfig config)
        {
            if (config == null) return false;
            var rt = FindRuntime(world, config.regionId);
            if (rt == null) return false;
            var campaign = NodeCampaignCatalog.Get(config.regionId);
            SyncLegacyClear(rt, campaign);
            if (config.bossRegion)
                return rt.bossDefeated;
            if (campaign == null || campaign.StageCount <= 1)
                return rt.cleared;
            return rt.highestClearedStage >= NodeCampaignCatalog.CombatIdleMinStage(campaign);
        }

        public static bool IsGatherUnlocked(PlayerWorldState world, RegionConfig config)
        {
            if (config == null) return false;
            var rt = FindRuntime(world, config.regionId);
            if (rt == null) return false;
            var campaign = NodeCampaignCatalog.Get(config.regionId);
            SyncLegacyClear(rt, campaign);
            if (campaign == null || campaign.StageCount <= 1)
                return rt.cleared || IsFarmUnlocked(world, config);
            return rt.highestClearedStage >= NodeCampaignCatalog.GatherIdleMinStage(campaign);
        }

        public static float IdleRewardTier(PlayerWorldState world, RegionConfig config)
        {
            if (config == null) return 0f;
            var rt = FindRuntime(world, config.regionId);
            if (rt == null) return 0f;
            var campaign = NodeCampaignCatalog.Get(config.regionId);
            SyncLegacyClear(rt, campaign);
            if (rt.cleared) return 1f;
            if (campaign == null || campaign.StageCount <= 1) return 0f;
            if (rt.highestClearedStage >= 5) return 0.8f;
            if (rt.highestClearedStage >= 3) return 0.6f;
            if (rt.highestClearedStage >= 2) return 0.4f;
            return 0f;
        }

        public static string CurrentEncounterId(PlayerWorldState world, string regionId)
        {
            var cfg = RegionCatalog.Get(regionId);
            var rt = FindRuntime(world, regionId);
            var stage = NodeCampaignCatalog.CurrentStage(rt, regionId);
            if (stage != null && !string.IsNullOrEmpty(stage.encounterId))
                return stage.encounterId;
            return cfg?.mainEncounterId ?? "";
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

            var campaign = NodeCampaignCatalog.Get(regionId);
            SyncLegacyClear(rt, campaign);
            encounterId ??= "";

            if (!string.IsNullOrEmpty(config.farmEncounterId) && encounterId == config.farmEncounterId)
            {
                rt.clearCount++;
                world.explorationProgress = ComputeExplorationProgress(world);
                world.count = world.regions.Count;
                return WorldCommandResult.Ok(false, regionId, encounterId);
            }

            var result = WorldCommandResult.Ok(false, regionId, encounterId);
            var alreadyComplete = campaign != null && rt.highestClearedStage >= campaign.StageCount;
            if (alreadyComplete)
            {
                rt.clearCount++;
                return result;
            }

            var nextIndex = NodeCampaignCatalog.NextStageIndex(rt, campaign);
            var expected = campaign != null && nextIndex >= 1 && nextIndex <= campaign.StageCount
                ? campaign.stages[nextIndex - 1]
                : null;
            var canAdvance = campaign == null
                || campaign.StageCount <= 1
                || expected == null
                || string.IsNullOrEmpty(encounterId)
                || encounterId == expected.encounterId
                || (nextIndex == 1 && encounterId == config.mainEncounterId);

            if (!canAdvance)
            {
                rt.clearCount++;
                return result;
            }

            rt.highestClearedStage = nextIndex;
            result.WasStageFirstClear = true;
            result.StageIndex = nextIndex;
            result.StageRewardTableId = expected?.firstClearRewardId ?? "";

            var complete = campaign == null || rt.highestClearedStage >= campaign.StageCount;
            if (complete)
            {
                var wasFirst = !rt.cleared;
                rt.cleared = true;
                var last = campaign != null && campaign.StageCount > 0
                    ? campaign.stages[campaign.StageCount - 1]
                    : null;
                if (config.bossRegion || last?.stageType == BattleStageType.Boss)
                    rt.bossDefeated = true;
                if (wasFirst)
                    rt.firstClearedAtUtc = nowUtc;
                result.WasFirstClear = wasFirst;
                result.BeaconRestored = wasFirst;
            }

            rt.clearCount++;
            world.explorationProgress = ComputeExplorationProgress(world);
            world.count = world.regions.Count;
            return result;
        }

        /// <summary>Completes every remaining stage. Used by tests and debug seeds.</summary>
        public static WorldCommandResult CompleteCampaign(PlayerWorldState world, string regionId, long nowUtc)
        {
            var campaign = NodeCampaignCatalog.Get(regionId);
            WorldCommandResult last = WorldCommandResult.Fail("Unknown region.");
            var guard = 0;
            while (guard++ < 16)
            {
                var rt = FindRuntime(world, regionId);
                if (rt != null && campaign != null && rt.highestClearedStage >= campaign.StageCount && rt.cleared)
                    return last.Success ? last : WorldCommandResult.Ok(false, regionId, "");
                var encounter = CurrentEncounterId(world, regionId);
                last = RegisterVictory(world, regionId, encounter, nowUtc);
                if (!last.Success) return last;
                if (last.BeaconRestored || last.WasFirstClear) return last;
            }

            return last;
        }

        public static void SyncLegacyClear(RegionRuntimeState rt, NodeCampaignDef campaign)
        {
            if (rt == null) return;
            var count = campaign?.StageCount ?? 1;
            if (rt.cleared && rt.highestClearedStage < count)
                rt.highestClearedStage = count;
            if (!rt.cleared && count > 0 && rt.highestClearedStage >= count)
                rt.cleared = true;
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
