using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    /// <summary>
    /// Lane fog: a new save shows only the spawn node. After a beacon is restored
    /// (campaign complete or a paid repair), that node stays visible and its adjacent
    /// combat nodes appear. Distant Unobserved bodies stay hidden.
    /// </summary>
    public static class GridVisibility
    {
        public static GridNodeState ReadState(PlayerWorldState world, string bodyId)
        {
            var node = FindNode(world, bodyId);
            return node?.state ?? GridNodeState.Unobserved;
        }

        public static bool IsBodyStable(PlayerWorldState world, string bodyId) =>
            ReadState(world, bodyId) >= GridNodeState.Stable;

        public static bool IsCombatBodyOnMap(PlayerWorldState world, string bodyId)
        {
            if (string.IsNullOrEmpty(bodyId) || SectorMapCatalog.IsPointOfInterest(bodyId))
                return false;
            if (bodyId == WorldConstants.OuterHavenBodyId)
                return true;
            if (ReadState(world, bodyId) >= GridNodeState.Fogged)
                return true;
            return HasNeighborAtLeast(world, bodyId, GridNodeState.Stable);
        }

        public static bool IsPoiOnMap(PlayerWorldState world, string poiId)
        {
            var anchor = SectorMapCatalog.FindPoiAnchorBodyId(poiId);
            if (string.IsNullOrEmpty(anchor))
                return false;
            return IsCombatBodyOnMap(world, anchor) && ReadState(world, anchor) >= GridNodeState.Stable;
        }

        public static bool CanRadarFog(PlayerWorldState world, string bodyId)
        {
            if (string.IsNullOrEmpty(bodyId) || SectorMapCatalog.IsPointOfInterest(bodyId))
                return false;
            if (ReadState(world, bodyId) != GridNodeState.Unobserved)
                return false;
            return HasNeighborAtLeast(world, bodyId, GridNodeState.Stable);
        }

        public static void SeedLaneGrid(PlayerWorldState world)
        {
            if (world == null) return;
            world.gridNodes ??= new List<GridNodeRuntime>();
            world.gridEdges ??= new List<GridEdgeRuntime>();

            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body == null) continue;
                var node = EnsureNode(world, body.bodyId);
                if (RegionCleared(world, PrimaryRegion(body)))
                    node.state = GridNodeState.Stable;
                else if (body.bodyId == WorldConstants.OuterHavenBodyId)
                    node.state = GridNodeState.Located;
                else
                    node.state = GridNodeState.Unobserved;
            }

            SyncEdgeTiers(world);
            world.gridSeeded = true;
        }

        /// <summary>
        /// Clamps over-revealed saves (spawn seeded Stable, radar painting the whole belt,
        /// leftover knownBodyIds). Chart owners keep silhouettes.
        /// </summary>
        public static bool SanitizeLaneFog(PlayerWorldState world)
        {
            if (world == null) return false;
            world.gridNodes ??= new List<GridNodeRuntime>();
            bool dirty = false;
            bool chart = HasChart(world);

            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body == null) continue;
                var node = EnsureNode(world, body.bodyId);
                var next = DesiredState(world, body, node.state, chart);
                if (next == node.state) continue;
                node.state = next;
                dirty = true;
            }

            if (dirty)
                SyncEdgeTiers(world);
            return dirty;
        }

        public static void UnlockAdjacentLocated(PlayerWorldState world, string bodyId)
        {
            if (world == null || string.IsNullOrEmpty(bodyId)) return;
            foreach (var edge in SectorMapCatalog.Edges)
            {
                if (edge == null) continue;
                if (edge.a != bodyId && edge.b != bodyId) continue;
                string other = edge.a == bodyId ? edge.b : edge.a;
                if (SectorMapCatalog.IsPointOfInterest(other)) continue;
                UpgradeState(world, other, GridNodeState.Located);
            }
        }

        public static void MarkStableAndUnlockAdjacent(PlayerWorldState world, string bodyId)
        {
            UpgradeState(world, bodyId, GridNodeState.Stable);
            UnlockAdjacentLocated(world, bodyId);
        }

        public static void RevealChartSilhouettes(PlayerWorldState world)
        {
            if (world == null) return;
            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body == null) continue;
                UpgradeState(world, body.bodyId, GridNodeState.Fogged);
            }
        }

        public static bool HasNeighborAtLeast(PlayerWorldState world, string bodyId, GridNodeState min)
        {
            foreach (var edge in SectorMapCatalog.Edges)
            {
                if (edge == null) continue;
                if (edge.a != bodyId && edge.b != bodyId) continue;
                string other = edge.a == bodyId ? edge.b : edge.a;
                if (ReadState(world, other) >= min)
                    return true;
            }

            return false;
        }

        private static GridNodeState DesiredState(
            PlayerWorldState world, StellarBodyDef body, GridNodeState current, bool chart)
        {
            if (RegionCleared(world, PrimaryRegion(body)))
                return current > GridNodeState.Stable ? current : GridNodeState.Stable;

            if (body.bodyId == WorldConstants.OuterHavenBodyId)
            {
                // Seed used to mark the spawn Stable. Keep Stable only when a neighbor was
                // actually located (paid repair / campaign unlock), otherwise drop to Located
                // so the first visit shows a single node.
                if (current >= GridNodeState.Stable
                    && HasNeighborAtLeast(world, body.bodyId, GridNodeState.Located))
                    return GridNodeState.Stable;
                return current >= GridNodeState.Located ? current : GridNodeState.Located;
            }

            if (chart)
                return current < GridNodeState.Fogged ? GridNodeState.Fogged : current;

            if (HasNeighborAtLeast(world, body.bodyId, GridNodeState.Stable))
                return current;

            return GridNodeState.Unobserved;
        }

        private static void UpgradeState(PlayerWorldState world, string bodyId, GridNodeState state)
        {
            if (world == null || string.IsNullOrEmpty(bodyId)) return;
            var node = EnsureNode(world, bodyId);
            if (state > node.state)
                node.state = state;
        }

        private static void SyncEdgeTiers(PlayerWorldState world)
        {
            foreach (var edge in SectorMapCatalog.Edges)
            {
                if (edge == null) continue;
                var rt = EnsureEdge(world, edge.edgeId);
                var a = ReadState(world, edge.a);
                var b = ReadState(world, edge.b);
                if (a >= GridNodeState.Stable && b >= GridNodeState.Stable)
                    rt.tier = GridEdgeTier.Stable;
                else if (a >= GridNodeState.Located && b >= GridNodeState.Located)
                    rt.tier = rt.tier > GridEdgeTier.Provisional ? rt.tier : GridEdgeTier.Provisional;
                else if (rt.tier > GridEdgeTier.Unknown && (a < GridNodeState.Located || b < GridNodeState.Located))
                    rt.tier = GridEdgeTier.Unknown;
            }
        }

        private static bool RegionCleared(PlayerWorldState world, string regionId)
        {
            if (string.IsNullOrEmpty(regionId)) return false;
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

        private static bool HasChart(PlayerWorldState world)
        {
            if (world?.unlockedCharts == null) return false;
            foreach (var id in world.unlockedCharts)
            {
                if (id == WorldConstants.FrontierChartId)
                    return true;
            }

            return false;
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

        private static GridNodeRuntime EnsureNode(PlayerWorldState world, string bodyId)
        {
            var node = FindNode(world, bodyId);
            if (node != null) return node;
            node = new GridNodeRuntime { bodyId = bodyId, state = GridNodeState.Unobserved };
            world.gridNodes.Add(node);
            return node;
        }

        private static GridEdgeRuntime FindEdge(PlayerWorldState world, string edgeId)
        {
            if (world?.gridEdges == null || string.IsNullOrEmpty(edgeId)) return null;
            foreach (var e in world.gridEdges)
            {
                if (e != null && e.edgeId == edgeId)
                    return e;
            }

            return null;
        }

        private static GridEdgeRuntime EnsureEdge(PlayerWorldState world, string edgeId)
        {
            var edge = FindEdge(world, edgeId);
            if (edge != null) return edge;
            edge = new GridEdgeRuntime { edgeId = edgeId, tier = GridEdgeTier.Unknown };
            world.gridEdges.Add(edge);
            return edge;
        }
    }
}
