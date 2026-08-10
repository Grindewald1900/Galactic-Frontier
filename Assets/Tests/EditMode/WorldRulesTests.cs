using Assets.Resources.Scripts.World.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class WorldRulesTests
    {
        [Test]
        public void OuterBelt_IsChallengeableWithStarterShip()
        {
            var world = WorldRules.CreateNewPlayerWorld();
            var ship = ShipRules.CreateStarterShip();
            var view = WorldRules.BuildView(world, ship, WorldConstants.OuterBeltId);
            Assert.AreEqual(RegionProgressState.Challengeable, view.Progress);
            Assert.IsTrue(view.CanEnter);
            Assert.IsFalse(view.FarmUnlocked);
        }

        [Test]
        public void MiningSpur_LockedUntilOuterBeltCleared()
        {
            var world = WorldRules.CreateNewPlayerWorld();
            var ship = ShipRules.CreateStarterShip();
            // Boost ship to meet mining gate so only prereq blocks.
            ship.level = 2;
            ShipRules.TryUpgradeModule(ship, "mod_propulsion");
            ShipRules.TryUpgradeModule(ship, "mod_cargo");

            var locked = WorldRules.BuildView(world, ship, WorldConstants.MiningSpurId);
            Assert.AreEqual(RegionProgressState.Locked, locked.Progress);
            Assert.IsFalse(locked.CanEnter);

            Assert.IsTrue(WorldRules.RegisterVictory(world, WorldConstants.OuterBeltId, "enc_outer_main", 10).Success);
            var open = WorldRules.BuildView(world, ship, WorldConstants.MiningSpurId);
            Assert.AreEqual(RegionProgressState.Challengeable, open.Progress);
            Assert.IsTrue(open.CanEnter);
        }

        [Test]
        public void FirstClear_UnlocksFarm()
        {
            var world = WorldRules.CreateNewPlayerWorld();
            Assert.IsTrue(WorldRules.RegisterVictory(world, WorldConstants.OuterBeltId, "enc_outer_main", 1).Success);
            var cfg = RegionCatalog.Get(WorldConstants.OuterBeltId);
            Assert.IsTrue(WorldRules.IsFarmUnlocked(world, cfg));
            var view = WorldRules.BuildView(world, ShipRules.CreateStarterShip(), WorldConstants.OuterBeltId);
            Assert.AreEqual(RegionProgressState.Cleared, view.Progress);
        }

        [Test]
        public void BossVictory_SetsBossDefeated()
        {
            var world = WorldRules.CreateNewPlayerWorld();
            // clear chain
            string[] chain =
            {
                WorldConstants.OuterBeltId, WorldConstants.MiningSpurId, WorldConstants.QuantumRiftId,
                WorldConstants.AbyssalEdgeId, WorldConstants.ConvoyLaneId
            };
            foreach (var id in chain)
                Assert.IsTrue(WorldRules.RegisterVictory(world, id, "enc_outer_main", 1).Success);

            Assert.IsTrue(WorldRules.RegisterVictory(world, WorldConstants.FrontierBossId, "enc_frontier_boss", 2).Success);
            Assert.IsTrue(WorldRules.IsSectorComplete(world));
            var view = WorldRules.BuildView(world, ShipRules.CreateStarterShip(), WorldConstants.FrontierBossId);
            Assert.AreEqual(RegionProgressState.BossDefeated, view.Progress);
        }

        [Test]
        public void ShipGate_BlocksWhenStatsMissing()
        {
            var world = WorldRules.CreateNewPlayerWorld();
            Assert.IsTrue(WorldRules.RegisterVictory(world, WorldConstants.OuterBeltId, "enc_outer_main", 1).Success);
            var ship = ShipRules.CreateStarterShip(); // cannot meet mining gate
            var view = WorldRules.BuildView(world, ship, WorldConstants.MiningSpurId);
            Assert.IsFalse(view.CanEnter);
            Assert.Greater(view.ShipGaps.Count, 0);
        }
    }
}
