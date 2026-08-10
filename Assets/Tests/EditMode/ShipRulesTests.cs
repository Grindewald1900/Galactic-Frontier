using Assets.Resources.Scripts.World.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class ShipRulesTests
    {
        [Test]
        public void StarterShip_MeetsOuterBeltGate()
        {
            var ship = ShipRules.CreateStarterShip();
            var gate = RegionCatalog.Get(WorldConstants.OuterBeltId).shipGate;
            Assert.IsTrue(ShipRules.MeetsGate(ship, gate).Ok);
        }

        [Test]
        public void ModuleUpgrade_RaisesPrimaryStat()
        {
            var ship = ShipRules.CreateStarterShip();
            var before = ShipRules.GetEffectiveStats(ship).range;
            Assert.IsTrue(ShipRules.TryUpgradeModule(ship, "mod_propulsion").Success);
            Assert.Greater(ShipRules.GetEffectiveStats(ship).range, before);
        }

        [Test]
        public void ShipLevelUpgrade_RaisesBaseStats()
        {
            var ship = ShipRules.CreateStarterShip();
            Assert.AreEqual(1, ship.level);
            Assert.IsTrue(ShipRules.TryUpgradeShipLevel(ship).Success);
            Assert.AreEqual(2, ship.level);
            Assert.GreaterOrEqual(ShipRules.GetEffectiveStats(ship).range, 1);
        }
    }

    public class CombatStrategyRulesTests
    {
        [Test]
        public void FocusLowestHp_PicksWeakestAlive()
        {
            var enemies = new[]
            {
                new CombatantSnapshot { Id = "a", CurrentHp = 50, MaxHp = 100, Power = 10, IsPlayer = false },
                new CombatantSnapshot { Id = "b", CurrentHp = 10, MaxHp = 100, Power = 40, IsPlayer = false },
                new CombatantSnapshot { Id = "c", CurrentHp = 0, MaxHp = 100, Power = 99, IsPlayer = false }
            };
            Assert.AreEqual(1, CombatStrategyRules.PickEnemyTargetIndex(CombatStrategyId.FocusLowestHp, enemies));
        }

        [Test]
        public void FocusHighestThreat_PicksStrongestAlive()
        {
            var enemies = new[]
            {
                new CombatantSnapshot { Id = "a", CurrentHp = 50, MaxHp = 100, Power = 10, IsPlayer = false },
                new CombatantSnapshot { Id = "b", CurrentHp = 40, MaxHp = 100, Power = 80, IsPlayer = false }
            };
            Assert.AreEqual(1, CombatStrategyRules.PickEnemyTargetIndex(CombatStrategyId.FocusHighestThreat, enemies));
        }
    }
}
