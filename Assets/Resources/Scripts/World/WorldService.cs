using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.World
{
    public static class WorldService
    {
        public static PlayerWorldState State { get; private set; }
        public static bool IsLoaded => State != null;

        public static void Clear() => State = null;

        public static void EnsureLoaded(DataUtil dataUtil)
        {
            if (dataUtil == null)
                throw new ArgumentNullException(nameof(dataUtil));
            var loaded = dataUtil.LoadWorldState();
            State = loaded != null && loaded.regions != null && loaded.regions.Count > 0
                ? Normalize(loaded)
                : WorldRules.CreateNewPlayerWorld();
            ApplyUniverseCatalog(dataUtil);
            if (loaded == null || loaded.regions == null || loaded.regions.Count == 0)
                dataUtil.SaveWorldState(State, touchMeta: true);
            NavigationService.EnsureReady();
        }

        public static void CreateForNewPlayer(DataUtil dataUtil)
        {
            State = WorldRules.CreateNewPlayerWorld();
            ApplyUniverseCatalog(dataUtil);
            dataUtil.SaveWorldState(State, touchMeta: false);
            NavigationService.EnsureReady();
        }

        /// <summary>Regenerates universe graph from seed (Debug). Resets universe-layer discovery.</summary>
        public static void RegenerateUniverse(int seed, DataUtil dataUtil = null)
        {
            EnsureReady();
            if (seed == 0) seed = 1;
            State.universeSeed = seed;
            UniverseMapCatalog.ResetCache();
            UniverseMapCatalog.Configure(seed);
            State.planeModifiers = ClonePlane(UniverseMapCatalog.ActiveUniverse?.planeModifiers);
            State.sectorNodes = new List<GridNodeRuntime>();
            GridService.EnsureReady();
            Save(dataUtil);
        }

        private static void ApplyUniverseCatalog(DataUtil dataUtil)
        {
            if (State.universeSeed == 0)
            {
                string pid = dataUtil?.currentPlayer?.playerID;
                State.universeSeed = UniverseSeedUtil.FromPlayerId(pid);
            }

            State.planeModifiers ??= new PlaneModifiersState();
            UniverseMapCatalog.Configure(State.universeSeed, State.planeModifiers);
            var active = UniverseMapCatalog.ActiveUniverse;
            if (active?.planeModifiers != null)
                State.planeModifiers = ClonePlane(active.planeModifiers);
        }

        private static PlaneModifiersState ClonePlane(PlaneModifiersState src)
        {
            if (src == null) return new PlaneModifiersState();
            return new PlaneModifiersState
            {
                primary = src.primary,
                secondary = src.secondary,
                gap = src.gap
            };
        }

        public static void Save(DataUtil dataUtil = null)
        {
            if (State == null) return;
            var util = dataUtil ?? DataUtil.Instance;
            if (util == null) return;
            State.count = State.regions?.Count ?? 0;
            util.SaveWorldState(State, touchMeta: true);
            SyncPlayerExploration();
        }

        public static RegionView GetRegionView(string regionId)
        {
            EnsureReady();
            ShipService.EnsureReady();
            return WorldRules.BuildView(State, ShipService.State, regionId);
        }

        public static IEnumerable<RegionView> GetAllRegionViews()
        {
            EnsureReady();
            ShipService.EnsureReady();
            foreach (var cfg in RegionCatalog.All)
                yield return WorldRules.BuildView(State, ShipService.State, cfg.regionId);
        }

        public static bool CanEnter(string regionId, out RegionView view)
        {
            view = GetRegionView(regionId);
            return view != null && view.CanEnter;
        }

        public static bool IsFarmUnlocked(string regionId)
        {
            EnsureReady();
            return WorldRules.IsFarmUnlocked(State, RegionCatalog.Get(regionId));
        }

        public static WorldCommandResult RegisterBattleVictory(string regionId, string encounterId)
        {
            EnsureReady();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var result = WorldRules.RegisterVictory(State, regionId, encounterId, now);
            if (result.Success)
            {
                GridService.OnRegionCleared(regionId);
                GridService.RecalcExplore();
                Save();
                SyncPlayerExploration();
                Assets.Resources.Scripts.Unlock.FeatureUnlockService.Evaluate();
            }

            return result;
        }

        public static void MarkFarmTick(string regionId)
        {
            EnsureReady();
            var rt = WorldRules.FindRuntime(State, regionId);
            if (rt == null) return;
            rt.lastFarmAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Save();
        }

        public static bool IsSectorComplete()
        {
            EnsureReady();
            return WorldRules.IsSectorComplete(State);
        }

        private static void SyncPlayerExploration()
        {
            if (DataUtil.Instance?.currentPlayer == null || State == null) return;
            DataUtil.Instance.currentPlayer.explorationProgress = State.explorationProgress;
            DataUtil.Instance.SavePlayerData(DataUtil.Instance.currentPlayer);
        }

        public static void EnsureReady()
        {
            if (State != null) return;
            if (DataUtil.Instance != null)
                EnsureLoaded(DataUtil.Instance);
            else
                State = WorldRules.CreateNewPlayerWorld();
        }

        private static PlayerWorldState Normalize(PlayerWorldState world)
        {
            world.regions ??= new List<RegionRuntimeState>();
            foreach (var cfg in RegionCatalog.All)
            {
                if (WorldRules.FindRuntime(world, cfg.regionId) == null)
                {
                    world.regions.Add(new RegionRuntimeState { regionId = cfg.regionId });
                }
            }

            world.currentSectorId = string.IsNullOrEmpty(world.currentSectorId)
                ? WorldConstants.SectorId
                : world.currentSectorId;
            world.knownBodyIds ??= new List<string>();
            world.gridNodes ??= new List<GridNodeRuntime>();
            world.gridEdges ??= new List<GridEdgeRuntime>();
            world.sectorNodes ??= new List<GridNodeRuntime>();
            world.unlockedCharts ??= new List<string>();
            world.planeModifiers ??= new PlaneModifiersState();
            if (world.navX <= 0.01f && world.navY <= 0.01f)
            {
                world.navX = SectorMapCatalog.SpawnX;
                world.navY = SectorMapCatalog.SpawnY;
            }

            world.count = world.regions.Count;
            return world;
        }
    }
}
