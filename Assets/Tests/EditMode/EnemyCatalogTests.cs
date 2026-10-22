using Assets.Resources.Scripts.World.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class EnemyCatalogTests
    {
        [Test]
        public void PrologueDrone_IsStableAndNotAPlayerCard()
        {
            var encounter = EncounterCatalog.Get("enc_prologue_sweep");
            var first = EnemyCatalog.Resolve(encounter, encounter.enemies[0], 0);
            var second = EnemyCatalog.Resolve(encounter, encounter.enemies[0], 0);

            Assert.AreEqual("drone_sweeper", first.ProfileId);
            Assert.AreEqual("Sweep Drone", first.DisplayNameEn);
            Assert.AreEqual(8f, first.Attack);
            Assert.AreEqual(6f, first.Defense);
            Assert.AreEqual(72f, first.Health);
            Assert.AreEqual(first.Attack, second.Attack);
            Assert.AreEqual(first.Defense, second.Defense);
            Assert.AreEqual(first.Health, second.Health);
            Assert.AreEqual(first.Power, second.Power);
            Assert.AreNotEqual("Asra", first.DisplayNameEn);
        }

        [Test]
        public void OuterBoss_HasTwoVisiblePhases()
        {
            var profile = EnemyCatalog.Get("outer_flagship");
            Assert.IsNotNull(profile);
            Assert.AreEqual(2, profile.phases.Length);
            Assert.AreEqual("Shield Hull", profile.phases[0].displayNameEn);
            Assert.AreEqual("Core Breach", profile.phases[1].displayNameEn);
            Assert.AreEqual("Magki", profile.phases[0].skillKey);
            Assert.AreEqual("Sernia", profile.phases[1].skillKey);
            Assert.AreNotEqual(profile.phases[0].attack, profile.phases[1].attack);
            Assert.IsTrue(EnemyCatalog.ShouldLeavePhase(0f, profile.phases[0].health, 0f));
            Assert.IsFalse(EnemyCatalog.ShouldLeavePhase(1f, profile.phases[0].health, 0f));
        }

        [Test]
        public void MiningBoss_IsTheNextPhasedBoss()
        {
            var profile = EnemyCatalog.Get("mining_leviathan");
            Assert.IsNotNull(profile);
            Assert.AreEqual(2, profile.phases.Length);
            Assert.AreEqual("Shell Plate", profile.phases[0].displayNameEn);
            Assert.AreEqual("Overload Core", profile.phases[1].displayNameEn);
            Assert.AreEqual("Magki", profile.phases[0].skillKey);
            Assert.AreEqual("Asra", profile.phases[1].skillKey);
        }

        [Test]
        public void RecommendedPower_UsesConfiguredEnemyScale()
        {
            var stage = NodeCampaignCatalog.Get(WorldConstants.OuterBeltId).stages[0];
            var expected = EnemyCatalog.RecommendedPower("enc_outer_main");
            Assert.AreEqual(expected, stage.recommendedPower);
            Assert.Greater(stage.recommendedPower, 200);
            Assert.Less(stage.recommendedPower, 800);
            Assert.AreNotEqual(80, stage.recommendedPower);

            var region = RegionCatalog.Get(WorldConstants.OuterBeltId);
            Assert.AreEqual(stage.recommendedPower, region.recommendedPower);

            var boss = NodeCampaignCatalog.Get(WorldConstants.OuterBeltId).stages[5];
            Assert.Greater(boss.recommendedPower, stage.recommendedPower);
        }

        [Test]
        public void FarmAndCurveEncounters_AreDeterministic()
        {
            var farm = EncounterCatalog.Get("enc_outer_farm");
            var a = EnemyCatalog.Resolve(farm, farm.enemies[0], 0);
            var b = EnemyCatalog.Resolve(farm, farm.enemies[0], 0);
            Assert.AreEqual("outer_scrap_drone", a.ProfileId);
            Assert.AreEqual(a.Power, b.Power);

            var rift = EncounterCatalog.Get("enc_rift_boss");
            var left = EnemyCatalog.Resolve(rift, rift.enemies[0], 0);
            var right = EnemyCatalog.Resolve(rift, rift.enemies[0], 0);
            Assert.AreEqual(left.Attack, right.Attack);
            Assert.AreEqual(left.Defense, right.Defense);
            Assert.AreEqual(left.Health, right.Health);
            Assert.IsFalse(left.DisplayNameEn.Equals("Sernia"));
            Assert.Greater(left.Power, 0);
        }

        [Test]
        public void EveryEncounter_ResolvesWithoutEmptyStats()
        {
            foreach (var encounter in EncounterCatalog.All)
            {
                Assert.IsNotNull(encounter.enemies);
                Assert.Greater(encounter.enemies.Length, 0, encounter.encounterId);
                var any = false;
                for (var i = 0; i < encounter.enemies.Length; i++)
                {
                    var resolved = EnemyCatalog.Resolve(encounter, encounter.enemies[i], i);
                    if (resolved == null) continue;
                    any = true;
                    Assert.Greater(resolved.Health, 0f, encounter.encounterId);
                    Assert.Greater(resolved.Attack, 0f, encounter.encounterId);
                    Assert.Greater(resolved.Defense, 0f, encounter.encounterId);
                    Assert.IsFalse(string.IsNullOrEmpty(resolved.DisplayNameEn), encounter.encounterId);
                }

                Assert.IsTrue(any, encounter.encounterId);
            }
        }
    }
}
