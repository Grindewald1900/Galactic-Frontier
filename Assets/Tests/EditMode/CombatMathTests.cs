using Assets.Resources.Scripts.Battle.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class CombatMathTests
    {
        [Test]
        public void HitRate_ClampsToUnitInterval()
        {
            Assert.AreEqual(0f, CombatMath.HitRate(0.2f, 0.5f), 1e-5f);
            Assert.AreEqual(1f, CombatMath.HitRate(1.5f, 0.1f), 1e-5f);
            Assert.AreEqual(0.3f, CombatMath.HitRate(0.8f, 0.5f), 1e-5f);
        }

        [Test]
        public void DamageReductionRate_MatchesContractShape()
        {
            var withDefense = CombatMath.DamageReductionRate(100f, 0f);
            Assert.Greater(withDefense, 0f);
            Assert.Less(withDefense, 1f);

            var capped = CombatMath.DamageReductionRate(1f, 0.95f);
            Assert.AreEqual(1f, capped, 1e-5f);
        }

        [Test]
        public void ResolveAttack_MissConsumesOnlyHitRoll()
        {
            // Force miss: hitRate 0 → first Value always misses.
            var rng = new BattleRng(12345);
            var miss = CombatMath.ResolveAttack(
                attackerAccuracy: 0f,
                defenderDodge: 0f,
                criticalChance: 1f,
                criticalDamageMultiplier: 2f,
                battleAttack: 50f,
                battleDefense: 10f,
                fixedDamageReduction: 0f,
                attackMultiplier: 1f,
                rng: rng);

            Assert.IsFalse(miss.Hit);
            Assert.AreEqual(0f, miss.Damage, 1e-5f);
            Assert.AreEqual(1f, miss.CriticalMultiplier, 1e-5f);
        }

        [Test]
        public void ResolveAttack_GuaranteedHitAndCrit_IsDeterministic()
        {
            const long seed = 424242L;
            AttackRollResult First()
            {
                var rng = new BattleRng(seed);
                return CombatMath.ResolveAttack(
                    attackerAccuracy: 1f,
                    defenderDodge: 0f,
                    criticalChance: 1f,
                    criticalDamageMultiplier: 2f,
                    battleAttack: 100f,
                    battleDefense: 1f,
                    fixedDamageReduction: 0f,
                    attackMultiplier: 1f,
                    rng: rng);
            }

            var a = First();
            var b = First();

            Assert.IsTrue(a.Hit);
            Assert.IsTrue(a.IsCritical);
            Assert.AreEqual(a.Damage, b.Damage, 1e-4f);
            Assert.AreEqual(a.CriticalMultiplier, b.CriticalMultiplier, 1e-5f);
            Assert.AreEqual(a.HitRoll, b.HitRoll, 1e-6f);
            Assert.AreEqual(a.CritRoll, b.CritRoll, 1e-6f);
        }

        [Test]
        public void ResolveAttack_SameSeedSameSequenceAcrossMultipleRolls()
        {
            float[] Run()
            {
                var rng = new BattleRng(99);
                var damages = new float[5];
                for (var i = 0; i < damages.Length; i++)
                {
                    damages[i] = CombatMath.ResolveAttack(
                        0.85f, 0.1f, 0.25f, 1.5f, 40f + i, 20f, 0.05f, 1f, rng).Damage;
                }

                return damages;
            }

            var left = Run();
            var right = Run();
            CollectionAssert.AreEqual(left, right);
        }
    }
}
