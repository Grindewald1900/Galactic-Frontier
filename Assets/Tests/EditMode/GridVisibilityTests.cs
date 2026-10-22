using Assets.Resources.Scripts.World.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class GridVisibilityTests
    {
        [Test]
        public void NewSave_ShowsOnlySpawnNode()
        {
            var world = WorldRules.CreateNewPlayerWorld();
            GridVisibility.SeedLaneGrid(world);

            Assert.IsTrue(GridVisibility.IsCombatBodyOnMap(world, WorldConstants.OuterHavenBodyId));
            Assert.AreEqual(GridNodeState.Located,
                GridVisibility.ReadState(world, WorldConstants.OuterHavenBodyId));

            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body == null || body.bodyId == WorldConstants.OuterHavenBodyId)
                    continue;
                Assert.IsFalse(GridVisibility.IsCombatBodyOnMap(world, body.bodyId), body.bodyId);
            }

            foreach (var poi in SectorMapCatalog.PointsOfInterest)
            {
                if (poi == null) continue;
                Assert.IsFalse(GridVisibility.IsPoiOnMap(world, poi.bodyId), poi.bodyId);
            }
        }

        [Test]
        public void OverSeededSpawn_SanitizesBackToSingleNode()
        {
            var world = WorldRules.CreateNewPlayerWorld();
            GridVisibility.SeedLaneGrid(world);
            Find(world, WorldConstants.OuterHavenBodyId).state = GridNodeState.Stable;

            Assert.IsTrue(GridVisibility.SanitizeLaneFog(world));
            Assert.AreEqual(GridNodeState.Located,
                GridVisibility.ReadState(world, WorldConstants.OuterHavenBodyId));
            Assert.IsFalse(GridVisibility.IsCombatBodyOnMap(world, "body_mining_spur"));
            Assert.IsFalse(GridVisibility.IsCombatBodyOnMap(world, "body_quantum_rift"));
        }

        [Test]
        public void RestoredBeacon_RevealsAdjacentAndUnlocksChallenge()
        {
            var world = WorldRules.CreateNewPlayerWorld();
            GridVisibility.SeedLaneGrid(world);
            var ship = ShipRules.CreateStarterShip();
            ship.level = 2;
            ShipRules.TryUpgradeModule(ship, "mod_propulsion");
            ShipRules.TryUpgradeModule(ship, "mod_cargo");

            Assert.AreEqual(RegionProgressState.Locked,
                WorldRules.BuildView(world, ship, WorldConstants.MiningSpurId).Progress);

            GridVisibility.MarkStableAndUnlockAdjacent(world, WorldConstants.OuterHavenBodyId);

            Assert.IsTrue(GridVisibility.IsCombatBodyOnMap(world, "body_mining_spur"));
            Assert.IsTrue(GridVisibility.IsCombatBodyOnMap(world, "body_convoy_lane"));
            Assert.IsTrue(GridVisibility.IsCombatBodyOnMap(world, "body_quantum_rift"));
            Assert.IsFalse(GridVisibility.IsCombatBodyOnMap(world, "body_abyssal_edge"));
            Assert.IsFalse(GridVisibility.IsCombatBodyOnMap(world, "body_frontier_anchor"));

            var mining = WorldRules.BuildView(world, ship, WorldConstants.MiningSpurId);
            Assert.AreEqual(RegionProgressState.Challengeable, mining.Progress);
            Assert.IsTrue(mining.CanEnter);
            Assert.Greater(mining.StageCount, 1);
        }

        [Test]
        public void TradePost_AppearsOnlyAfterHavenBeacon()
        {
            var world = WorldRules.CreateNewPlayerWorld();
            GridVisibility.SeedLaneGrid(world);
            Assert.IsFalse(GridVisibility.IsPoiOnMap(world, "poi_trade_post"));

            GridVisibility.MarkStableAndUnlockAdjacent(world, WorldConstants.OuterHavenBodyId);
            Assert.IsTrue(GridVisibility.IsPoiOnMap(world, "poi_trade_post"));
            Assert.IsFalse(GridVisibility.IsPoiOnMap(world, "poi_wanderer"));
        }

        private static GridNodeRuntime Find(PlayerWorldState world, string bodyId)
        {
            foreach (var n in world.gridNodes)
            {
                if (n != null && n.bodyId == bodyId)
                    return n;
            }

            return null;
        }
    }
}
