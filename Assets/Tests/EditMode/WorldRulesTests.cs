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
            Assert.AreEqual(NodeCampaignState.Advancing, view.CampaignState);
            Assert.AreEqual(6, view.StageCount);
            Assert.AreEqual(1, view.CurrentStage);
            Assert.IsTrue(view.CanEnter);
            Assert.IsFalse(view.FarmUnlocked);
            Assert.IsFalse(view.GatherUnlocked);
        }

        [Test]
        public void MiningSpur_LockedUntilOuterBeltBeaconRestored()
        {
            var world = WorldRules.CreateNewPlayerWorld();
            var ship = ShipRules.CreateStarterShip();
            ship.level = 2;
            ShipRules.TryUpgradeModule(ship, "mod_propulsion");
            ShipRules.TryUpgradeModule(ship, "mod_cargo");

            var locked = WorldRules.BuildView(world, ship, WorldConstants.MiningSpurId);
            Assert.AreEqual(RegionProgressState.Locked, locked.Progress);
            Assert.IsFalse(locked.CanEnter);

            Assert.IsTrue(WorldRules.RegisterVictory(world, WorldConstants.OuterBeltId, "enc_outer_main", 10).Success);
            var stillLocked = WorldRules.BuildView(world, ship, WorldConstants.MiningSpurId);
            Assert.AreEqual(RegionProgressState.Locked, stillLocked.Progress);

            Assert.IsTrue(WorldRules.CompleteCampaign(world, WorldConstants.OuterBeltId, 11).Success);
            var open = WorldRules.BuildView(world, ship, WorldConstants.MiningSpurId);
            Assert.AreEqual(RegionProgressState.Challengeable, open.Progress);
            Assert.IsTrue(open.CanEnter);
        }

        [Test]
        public void OuterCampaign_UnlocksIdleByStage()
        {
            var world = WorldRules.CreateNewPlayerWorld();
            var cfg = RegionCatalog.Get(WorldConstants.OuterBeltId);
            var ship = ShipRules.CreateStarterShip();

            Assert.IsTrue(WorldRules.RegisterVictory(world, WorldConstants.OuterBeltId, "enc_outer_main", 1).Success);
            Assert.IsFalse(WorldRules.IsFarmUnlocked(world, cfg));
            Assert.IsFalse(WorldRules.IsGatherUnlocked(world, cfg));
            var after1 = WorldRules.BuildView(world, ship, WorldConstants.OuterBeltId);
            Assert.AreEqual(RegionProgressState.Challengeable, after1.Progress);
            Assert.AreEqual(2, after1.CurrentStage);

            Assert.IsTrue(WorldRules.RegisterVictory(world, WorldConstants.OuterBeltId, "enc_outer_s2", 2).Success);
            Assert.IsTrue(WorldRules.IsGatherUnlocked(world, cfg));
            Assert.IsFalse(WorldRules.IsFarmUnlocked(world, cfg));
            Assert.AreEqual(0.4f, WorldRules.IdleRewardTier(world, cfg), 0.01f);

            Assert.IsTrue(WorldRules.RegisterVictory(world, WorldConstants.OuterBeltId, "enc_outer_s3", 3).Success);
            Assert.IsTrue(WorldRules.IsFarmUnlocked(world, cfg));
            Assert.AreEqual(0.6f, WorldRules.IdleRewardTier(world, cfg), 0.01f);
            Assert.AreEqual(NodeCampaignState.Staging,
                WorldRules.GetCampaignState(world, cfg));

            Assert.IsTrue(WorldRules.RegisterVictory(world, WorldConstants.OuterBeltId, "enc_outer_s4", 4).Success);
            Assert.IsTrue(WorldRules.RegisterVictory(world, WorldConstants.OuterBeltId, "enc_outer_s5", 5).Success);
            Assert.AreEqual(NodeCampaignState.BossReady, WorldRules.GetCampaignState(world, cfg));

            var boss = WorldRules.RegisterVictory(world, WorldConstants.OuterBeltId, "enc_outer_boss", 6);
            Assert.IsTrue(boss.Success);
            Assert.IsTrue(boss.WasFirstClear);
            Assert.IsTrue(boss.BeaconRestored);
            var cleared = WorldRules.BuildView(world, ship, WorldConstants.OuterBeltId);
            Assert.AreEqual(RegionProgressState.Cleared, cleared.Progress);
            Assert.AreEqual(NodeCampaignState.BeaconRestored, cleared.CampaignState);
            Assert.AreEqual(1f, WorldRules.IdleRewardTier(world, cfg), 0.01f);
        }

        [Test]
        public void FarmEncounter_DoesNotAdvanceCampaign()
        {
            var world = WorldRules.CreateNewPlayerWorld();
            Assert.IsTrue(WorldRules.RegisterVictory(world, WorldConstants.OuterBeltId, "enc_outer_main", 1).Success);
            Assert.IsTrue(WorldRules.RegisterVictory(world, WorldConstants.OuterBeltId, "enc_outer_farm", 2).Success);
            var rt = WorldRules.FindRuntime(world, WorldConstants.OuterBeltId);
            Assert.AreEqual(1, rt.highestClearedStage);
            Assert.IsFalse(rt.cleared);
        }

        [Test]
        public void BossVictory_SetsBossDefeated()
        {
            var world = WorldRules.CreateNewPlayerWorld();
            string[] chain =
            {
                WorldConstants.OuterBeltId, WorldConstants.MiningSpurId, WorldConstants.QuantumRiftId,
                WorldConstants.AbyssalEdgeId, WorldConstants.ConvoyLaneId
            };
            foreach (var id in chain)
                Assert.IsTrue(WorldRules.CompleteCampaign(world, id, 1).Success);

            Assert.IsTrue(WorldRules.CompleteCampaign(world, WorldConstants.FrontierBossId, 2).Success);
            Assert.IsTrue(WorldRules.IsSectorComplete(world));
            var view = WorldRules.BuildView(world, ShipRules.CreateStarterShip(), WorldConstants.FrontierBossId);
            Assert.AreEqual(RegionProgressState.BossDefeated, view.Progress);
        }

        [Test]
        public void CombatRegions_HaveMultiStageCampaigns()
        {
            foreach (var region in RegionCatalog.All)
            {
                var campaign = NodeCampaignCatalog.Get(region.regionId);
                Assert.IsNotNull(campaign, region.regionId);
                Assert.Greater(campaign.StageCount, 1, region.regionId);
                foreach (var stage in campaign.stages)
                {
                    Assert.IsNotNull(EncounterCatalog.Get(stage.encounterId), stage.encounterId);
                    Assert.IsNotNull(RewardCatalog.Get(stage.firstClearRewardId), stage.firstClearRewardId);
                }
            }
        }

        [Test]
        public void ShipGate_BlocksWhenStatsMissing()
        {
            var world = WorldRules.CreateNewPlayerWorld();
            Assert.IsTrue(WorldRules.CompleteCampaign(world, WorldConstants.OuterBeltId, 1).Success);
            var ship = ShipRules.CreateStarterShip();
            var view = WorldRules.BuildView(world, ship, WorldConstants.MiningSpurId);
            Assert.IsFalse(view.CanEnter);
            Assert.Greater(view.ShipGaps.Count, 0);
        }
    }
}
