using System.Collections.Generic;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Market;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Progression;
using Assets.Resources.Scripts.Progression.Domain;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.World
{
    /// <summary>
    /// Two-layer Astral Grid: in-sector planets + universe sectors.
    /// Victory unlocks adjacent planet nodes; capital victory unlocks adjacent sectors.
    /// </summary>
    public static class GridService
    {
        private static bool ensuring;

        public static void EnsureReady()
        {
            if (ensuring) return;
            ensuring = true;
            try
            {
                WorldService.EnsureReady();
                var world = WorldService.State;
                if (world == null) return;
                NormalizeLists(world);
                bool seededNow = false;
                if (!world.gridSeeded)
                {
                    SeedFromLegacy(world);
                    RecalcExplore();
                    seededNow = true;
                }

                bool missingUniverse = world.sectorNodes == null || world.sectorNodes.Count == 0;
                SeedUniverseSectors(world);
                SyncKnownBodies(world);
                ReplayAdjacencyUnlocks(world);
                if (seededNow || missingUniverse)
                    WorldService.Save();
            }
            finally
            {
                ensuring = false;
            }
        }

        public static GridNodeState GetState(string bodyId)
        {
            EnsureReady();
            var node = FindNode(WorldService.State, bodyId);
            return node?.state ?? GridNodeState.Unobserved;
        }

        public static GridEdgeTier GetEdgeTier(string edgeId)
        {
            EnsureReady();
            var edge = FindEdgeRuntime(WorldService.State, edgeId);
            return edge?.tier ?? GridEdgeTier.Unknown;
        }

        public static bool HasChart(string chartId = WorldConstants.FrontierChartId)
        {
            EnsureReady();
            var list = WorldService.State?.unlockedCharts;
            if (list == null || string.IsNullOrEmpty(chartId)) return false;
            foreach (var id in list)
            {
                if (id == chartId) return true;
            }

            return false;
        }

        public static void EnsureLocated(string bodyId)
        {
            EnsureReady();
            if (string.IsNullOrEmpty(bodyId)) return;
            if (GetState(bodyId) < GridNodeState.Located)
                SetNodeState(bodyId, GridNodeState.Located);
        }

        public static bool ShowsFog(string bodyId) => GetState(bodyId) < GridNodeState.Located;

        /// <summary>Unlocked (fogged+) or adjacent to a located node. Distant unobserved stays hidden.</summary>
        public static bool IsBodyOnMap(string bodyId)
        {
            if (string.IsNullOrEmpty(bodyId)) return false;
            if (GetState(bodyId) >= GridNodeState.Fogged) return true;
            return HasLocatedNeighbor(bodyId);
        }

        public static bool IsSectorOnMap(string sectorId)
        {
            if (string.IsNullOrEmpty(sectorId)) return false;
            if (GetSectorState(sectorId) >= GridNodeState.Fogged) return true;
            return HasLocatedSectorNeighbor(sectorId);
        }

        public static bool IsFogged(string bodyId) => GetState(bodyId) == GridNodeState.Fogged;

        public static GridNodeState GetSectorState(string sectorId)
        {
            EnsureReady();
            var node = FindSectorNode(WorldService.State, sectorId);
            return node?.state ?? GridNodeState.Unobserved;
        }

        public static bool SectorShowsFog(string sectorId) => GetSectorState(sectorId) < GridNodeState.Located;

        public static bool CanEnterSector(string sectorId)
        {
            var def = UniverseMapCatalog.Get(sectorId);
            if (def == null || !def.playable) return false;
            return GetSectorState(sectorId) >= GridNodeState.Located;
        }

        public static bool IsLocatedOrBetter(string bodyId) => GetState(bodyId) >= GridNodeState.Located;

        public static bool CanProbe(string bodyId)
        {
            if (GetState(bodyId) != GridNodeState.Fogged)
                return false;
            return HasLocatedNeighbor(bodyId);
        }

        public static bool CanStabilize(string bodyId)
        {
            if (!IsLocatedOrBetter(bodyId) || GetState(bodyId) >= GridNodeState.Stable)
                return false;
            return SectorMapCatalog.Get(bodyId) != null;
        }

        public static bool CanBuyChart()
        {
            if (HasChart()) return false;
            return GetState(WorldConstants.OuterHavenBodyId) >= GridNodeState.Located;
        }

        public static WorldCommandResult TryProbe(string bodyId)
        {
            EnsureReady();
            var body = SectorMapCatalog.Get(bodyId);
            if (body == null)
                return WorldCommandResult.Fail("Unknown body.");
            if (GetState(bodyId) != GridNodeState.Fogged)
                return WorldCommandResult.Fail("No fogged signal to lock.");
            if (!CanProbe(bodyId))
                return WorldCommandResult.Fail("Signal too faint — probe from a located neighbor.");

            SetNodeState(bodyId, GridNodeState.Located);
            RecalcExplore();
            WorldService.Save();
            if (bodyId == "body_mining_spur")
                Assets.Resources.Scripts.ChapterQuest.ChapterQuestService.NotifyMiningSignalScanned();
            GrantScanProgression();
            return WorldCommandResult.OkMessage("Signal locked. Node located — challenge when ready.");
        }

        public static WorldCommandResult TryStabilize(string bodyId)
        {
            EnsureReady();
            var body = SectorMapCatalog.Get(bodyId);
            if (body == null)
                return WorldCommandResult.Fail("Unknown body.");
            if (!CanStabilize(bodyId))
                return WorldCommandResult.Fail("Locate the node before repairing its beacon.");

            string regionId = PrimaryRegion(body);
            bool cleared = !string.IsNullOrEmpty(regionId) && RegionCleared(regionId);
            if (!cleared)
            {
                if (!TryPayStabilize(out var err))
                    return WorldCommandResult.Fail(err);
            }

            PromoteNodeAndIncident(bodyId, GridNodeState.Stable, GridEdgeTier.Stable);
            RecalcExplore();
            WorldService.Save();
            GrantScanProgression();
            return WorldCommandResult.OkMessage(
                cleared
                    ? "Beacon restored. Lane is stable."
                    : "Beacon patched with scrap. Lane is stable.");
        }

        public static WorldCommandResult TryBuyChart()
        {
            EnsureReady();
            if (HasChart())
                return WorldCommandResult.Fail("Sector chart already unlocked.");
            if (!CanBuyChart())
                return WorldCommandResult.Fail("Outer Haven must be located to buy the sector chart.");

            if (!CurrencyService.TrySpendCredits(WorldConstants.ChartCreditCost, out var err))
                return WorldCommandResult.Fail(err);

            var world = WorldService.State;
            world.unlockedCharts.Add(WorldConstants.FrontierChartId);
            ApplyChartReveal(world);
            RecalcExplore();
            WorldService.Save();
            return WorldCommandResult.OkMessage("Chart burned in. More silhouettes revealed on the grid.");
        }

        public static void OnArrived(string fromBodyId, string toBodyId)
        {
            EnsureReady();
            if (string.IsNullOrEmpty(toBodyId)) return;
            var state = GetState(toBodyId);
            if (state < GridNodeState.Located)
                SetNodeState(toBodyId, GridNodeState.Located);
            if (GetState(toBodyId) < GridNodeState.Provisional)
                SetNodeState(toBodyId, GridNodeState.Provisional);

            var edge = SectorMapCatalog.FindEdge(fromBodyId, toBodyId);
            if (edge != null && GetEdgeTier(edge.edgeId) < GridEdgeTier.Provisional)
                SetEdgeTier(edge.edgeId, GridEdgeTier.Provisional);

            RecalcExplore();
        }

        public static void OnRegionCleared(string regionId)
        {
            EnsureReady();
            var body = SectorMapCatalog.FindByRegion(regionId);
            if (body == null) return;
            PromoteNodeAndIncident(body.bodyId, GridNodeState.Stable, GridEdgeTier.Stable);
            UnlockAdjacentBodies(body.bodyId);
            if (body.capitalHub)
                UnlockAdjacentSectors(body.sectorId);
            RecalcExplore();
        }

        public static int FogBodiesInRadar()
        {
            EnsureReady();
            int newly = 0;
            float range = NavigationService.GetRadarRange();
            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body == null || GetState(body.bodyId) != GridNodeState.Unobserved)
                    continue;
                if (NavigationService.DistanceToBody(body) <= range)
                {
                    SetNodeState(body.bodyId, GridNodeState.Fogged);
                    newly++;
                }
            }

            return newly;
        }

        public static int CountFogged()
        {
            EnsureReady();
            int n = 0;
            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body != null && GetState(body.bodyId) == GridNodeState.Fogged)
                    n++;
            }

            return n;
        }

        public static int CountLocatedOrBetter()
        {
            EnsureReady();
            int n = 0;
            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body != null && GetState(body.bodyId) >= GridNodeState.Located)
                    n++;
            }

            return n;
        }

        public static CruiseCost EstimateCruise(StellarBodyDef target)
        {
            EnsureReady();
            int rec = target?.recommendedExpeditionLv ?? 1;
            int shipLv = Mathf.Max(1, ShipService.State?.level ?? 1);
            var fromId = NavigationService.NearestLocatedBodyId();
            var edge = target == null ? null : SectorMapCatalog.FindEdge(fromId, target.bodyId);

            float mult = 1f;
            if (edge != null)
            {
                mult += edge.entropy;
                if (GetEdgeTier(edge.edgeId) < GridEdgeTier.Stable)
                    mult += 0.25f;
                if (edge.rift)
                    mult += 0.15f;
            }
            else
            {
                mult += 0.35f;
            }

            if (GetState(target?.bodyId) < GridNodeState.Stable)
                mult += 0.15f;

            bool autoReturn = true;
            if (shipLv < rec)
            {
                mult += 0.15f * (rec - shipLv);
                autoReturn = false;
            }

            return new CruiseCost(mult, autoReturn, rec, shipLv, edge);
        }

        public static void RecalcExplore()
        {
            var world = WorldService.State;
            if (world == null) return;
            int regionPct = WorldRules.ComputeExplorationProgress(world);
            int gridPct = ComputeGridPercent(world);
            world.explorePoints = gridPct;
            world.explorationProgress = Mathf.Clamp((regionPct * 2 + gridPct * 3) / 5, 0, 100);
            SyncKnownBodies(world);
        }

        private static int ComputeGridPercent(PlayerWorldState world)
        {
            int points = 0;
            int max = 0;
            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body == null) continue;
                max += 18;
                var state = FindNode(world, body.bodyId)?.state ?? GridNodeState.Unobserved;
                if (state == GridNodeState.Fogged) points += 4;
                else if (state == GridNodeState.Located) points += 10;
                else if (state == GridNodeState.Provisional) points += 14;
                else if (state == GridNodeState.Stable) points += 18;
            }

            foreach (var edge in SectorMapCatalog.Edges)
            {
                if (edge == null) continue;
                max += 8;
                var tier = FindEdgeRuntime(world, edge.edgeId)?.tier ?? GridEdgeTier.Unknown;
                if (tier == GridEdgeTier.Provisional) points += 4;
                else if (tier == GridEdgeTier.Stable) points += 8;
            }

            max += 10;
            if (HasChartId(world, WorldConstants.FrontierChartId))
                points += 10;

            if (max <= 0) return 0;
            return Mathf.Clamp(points * 100 / max, 0, 100);
        }

        private static void SeedFromLegacy(PlayerWorldState world)
        {
            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body == null) continue;
                var node = EnsureNode(world, body.bodyId);
                bool known = ContainsId(world.knownBodyIds, body.bodyId);
                bool cleared = false;
                string regionId = PrimaryRegion(body);
                if (!string.IsNullOrEmpty(regionId))
                    cleared = RegionCleared(world, regionId);

                if (body.bodyId == WorldConstants.OuterHavenBodyId || cleared)
                    node.state = GridNodeState.Stable;
                else if (known)
                    node.state = GridNodeState.Located;
                else
                    node.state = GridNodeState.Unobserved;
            }

            foreach (var edge in SectorMapCatalog.Edges)
            {
                if (edge == null) continue;
                var rt = EnsureEdge(world, edge.edgeId);
                var a = FindNode(world, edge.a)?.state ?? GridNodeState.Unobserved;
                var b = FindNode(world, edge.b)?.state ?? GridNodeState.Unobserved;
                if (a >= GridNodeState.Stable && b >= GridNodeState.Stable)
                    rt.tier = GridEdgeTier.Stable;
                else if (a >= GridNodeState.Located && b >= GridNodeState.Located)
                    rt.tier = GridEdgeTier.Provisional;
                else
                    rt.tier = GridEdgeTier.Unknown;
            }

            world.gridSeeded = true;
        }

        private static void SeedUniverseSectors(PlayerWorldState world)
        {
            foreach (var sector in UniverseMapCatalog.Sectors)
            {
                if (sector == null) continue;
                var node = EnsureSectorNode(world, sector.sectorId);
                if (sector.playable && node.state < GridNodeState.Located)
                    node.state = GridNodeState.Stable;
            }
        }

        private static void ReplayAdjacencyUnlocks(PlayerWorldState world)
        {
            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body == null) continue;
                string regionId = PrimaryRegion(body);
                if (string.IsNullOrEmpty(regionId) || !RegionCleared(world, regionId))
                    continue;
                UnlockAdjacentBodies(body.bodyId);
                if (body.capitalHub)
                    UnlockAdjacentSectors(body.sectorId);
            }
        }

        private static void UnlockAdjacentBodies(string bodyId)
        {
            foreach (var edge in SectorMapCatalog.Edges)
            {
                if (edge == null) continue;
                if (edge.a != bodyId && edge.b != bodyId) continue;
                string other = edge.a == bodyId ? edge.b : edge.a;
                SetNodeState(other, GridNodeState.Located);
            }
        }

        private static void UnlockAdjacentSectors(string sectorId)
        {
            foreach (var edge in UniverseMapCatalog.Edges)
            {
                if (edge == null) continue;
                if (edge.a != sectorId && edge.b != sectorId) continue;
                string other = edge.a == sectorId ? edge.b : edge.a;
                SetSectorState(other, GridNodeState.Located);
            }
        }

        private static void ApplyChartReveal(PlayerWorldState world)
        {
            // Buying the sector chart at the merchant unlocks the ENTIRE sector's nodes — every
            // body becomes fully located/visible. Challengeability stays gated by lane adjacency
            // to cleared nodes (WorldRules.CanChallengeRegion), so revealing != challengeable.
            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body == null) continue;
                var node = EnsureNode(world, body.bodyId);
                if (node.state < GridNodeState.Located)
                    node.state = GridNodeState.Located;
            }
        }

        private static void PromoteNodeAndIncident(string bodyId, GridNodeState nodeState, GridEdgeTier minEdge)
        {
            SetNodeState(bodyId, nodeState);
            foreach (var edge in SectorMapCatalog.Edges)
            {
                if (edge == null) continue;
                if (edge.a != bodyId && edge.b != bodyId) continue;
                string other = edge.a == bodyId ? edge.b : edge.a;
                if (GetState(other) < GridNodeState.Located) continue;
                if (GetEdgeTier(edge.edgeId) < minEdge)
                    SetEdgeTier(edge.edgeId, minEdge);
            }
        }

        private static bool HasLocatedSectorNeighbor(string sectorId)
        {
            foreach (var edge in UniverseMapCatalog.Edges)
            {
                if (edge == null) continue;
                if (edge.a != sectorId && edge.b != sectorId) continue;
                string other = edge.a == sectorId ? edge.b : edge.a;
                if (GetSectorState(other) >= GridNodeState.Located)
                    return true;
            }

            return false;
        }

        private static bool HasLocatedNeighbor(string bodyId)
        {
            foreach (var edge in SectorMapCatalog.Edges)
            {
                if (edge == null) continue;
                if (edge.a != bodyId && edge.b != bodyId) continue;
                string other = edge.a == bodyId ? edge.b : edge.a;
                if (GetState(other) >= GridNodeState.Located)
                    return true;
            }

            return false;
        }

        private static void SetNodeState(string bodyId, GridNodeState state)
        {
            var world = WorldService.State;
            if (world == null || string.IsNullOrEmpty(bodyId)) return;
            var node = EnsureNode(world, bodyId);
            if (state > node.state)
                node.state = state;
        }

        private static void SetSectorState(string sectorId, GridNodeState state)
        {
            var world = WorldService.State;
            if (world == null || string.IsNullOrEmpty(sectorId)) return;
            var node = EnsureSectorNode(world, sectorId);
            if (state > node.state)
                node.state = state;
        }

        private static void SetEdgeTier(string edgeId, GridEdgeTier tier)
        {
            var world = WorldService.State;
            if (world == null || string.IsNullOrEmpty(edgeId)) return;
            var edge = EnsureEdge(world, edgeId);
            if (tier > edge.tier)
                edge.tier = tier;
        }

        private static void SyncKnownBodies(PlayerWorldState world)
        {
            if (world == null) return;
            world.knownBodyIds ??= new List<string>();
            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body == null) continue;
                var state = FindNode(world, body.bodyId)?.state ?? GridNodeState.Unobserved;
                bool located = state >= GridNodeState.Located;
                bool listed = ContainsId(world.knownBodyIds, body.bodyId);
                if (located && !listed)
                    world.knownBodyIds.Add(body.bodyId);
            }
        }

        private static void NormalizeLists(PlayerWorldState world)
        {
            world.knownBodyIds ??= new List<string>();
            world.gridNodes ??= new List<GridNodeRuntime>();
            world.gridEdges ??= new List<GridEdgeRuntime>();
            world.sectorNodes ??= new List<GridNodeRuntime>();
            world.unlockedCharts ??= new List<string>();
        }

        private static GridNodeRuntime FindNode(PlayerWorldState world, string bodyId)
        {
            if (world?.gridNodes == null || string.IsNullOrEmpty(bodyId)) return null;
            foreach (var n in world.gridNodes)
            {
                if (n != null && n.bodyId == bodyId)
                    return n;
            }

            return null;
        }

        private static GridEdgeRuntime FindEdgeRuntime(PlayerWorldState world, string edgeId)
        {
            if (world?.gridEdges == null || string.IsNullOrEmpty(edgeId)) return null;
            foreach (var e in world.gridEdges)
            {
                if (e != null && e.edgeId == edgeId)
                    return e;
            }

            return null;
        }

        private static GridNodeRuntime FindSectorNode(PlayerWorldState world, string sectorId)
        {
            if (world?.sectorNodes == null || string.IsNullOrEmpty(sectorId)) return null;
            foreach (var n in world.sectorNodes)
            {
                if (n != null && n.bodyId == sectorId)
                    return n;
            }

            return null;
        }

        private static GridNodeRuntime EnsureNode(PlayerWorldState world, string bodyId)
        {
            var node = FindNode(world, bodyId);
            if (node != null) return node;
            node = new GridNodeRuntime { bodyId = bodyId, state = GridNodeState.Unobserved };
            world.gridNodes.Add(node);
            return node;
        }

        private static GridNodeRuntime EnsureSectorNode(PlayerWorldState world, string sectorId)
        {
            var node = FindSectorNode(world, sectorId);
            if (node != null) return node;
            node = new GridNodeRuntime { bodyId = sectorId, state = GridNodeState.Unobserved };
            world.sectorNodes.Add(node);
            return node;
        }

        private static GridEdgeRuntime EnsureEdge(PlayerWorldState world, string edgeId)
        {
            var edge = FindEdgeRuntime(world, edgeId);
            if (edge != null) return edge;
            edge = new GridEdgeRuntime { edgeId = edgeId, tier = GridEdgeTier.Unknown };
            world.gridEdges.Add(edge);
            return edge;
        }

        private static bool ContainsId(List<string> list, string id)
        {
            if (list == null || string.IsNullOrEmpty(id)) return false;
            foreach (var x in list)
            {
                if (x == id) return true;
            }

            return false;
        }

        private static bool HasChartId(PlayerWorldState world, string chartId)
        {
            if (world?.unlockedCharts == null) return false;
            foreach (var id in world.unlockedCharts)
            {
                if (id == chartId) return true;
            }

            return false;
        }

        private static bool RegionCleared(string regionId) =>
            RegionCleared(WorldService.State, regionId);

        private static bool RegionCleared(PlayerWorldState world, string regionId)
        {
            var cfg = RegionCatalog.Get(regionId);
            var rt = WorldRules.FindRuntime(world, regionId);
            if (cfg == null || rt == null) return false;
            return cfg.bossRegion ? rt.bossDefeated : rt.cleared;
        }

        private static string PrimaryRegion(StellarBodyDef body)
        {
            if (body?.regionIds == null || body.regionIds.Length == 0)
                return "";
            return body.regionIds[0] ?? "";
        }

        private static bool TryPayStabilize(out string error)
        {
            error = null;
            int scrap = WorldConstants.StabilizeScrapCost;
            int credit = WorldConstants.StabilizeCreditCost;
            if (credit > 0 && (DataUtil.Instance?.currentPlayer?.creditPoints ?? 0) < credit)
            {
                error = $"Need {credit} credits to patch the beacon.";
                return false;
            }

            var items = ProductionService.GetLocalItems();
            foreach (var item in items)
                ItemFactory.NormalizeLegacy(item);
            int have = InventoryRules.CountOf(
                ItemFactory.ToStacks(items), EconomyConstants.ScrapDefId, 1);
            if (have < scrap)
            {
                error = $"Need {scrap} scrap to patch the beacon.";
                return false;
            }

            if (credit > 0 && !CurrencyService.TrySpendCredits(credit, out var creditErr))
            {
                error = creditErr;
                return false;
            }

            if (!ProductionService.TryConsumeLocal(EconomyConstants.ScrapDefId, scrap, 1))
            {
                if (credit > 0)
                    CurrencyService.AddCredits(credit);
                error = $"Need {scrap} scrap to patch the beacon.";
                return false;
            }

            return true;
        }

        private static void GrantScanProgression()
        {
            var cards = CardListManager.Instance?.cardEntities;
            var deck = DeckService.GetActiveCombatDeck();
            var members = deck != null
                ? DeckService.GetOrderedMembers(deck.deckId, cards)
                : null;
            ProgressionService.BeginGrant();
            ProgressionService.GrantProfessionToParty(members, ProfessionSkill.Scan, 1, 15f);
            ProgressionService.GrantCommander(ProgressionCatalog.CommanderScanXp);
            ProgressionService.EndGrant(presentUi: false);
        }
    }
}
