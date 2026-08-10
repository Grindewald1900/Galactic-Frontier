using Assets.Resources.Scripts.Battle.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class BattleRngTests
    {
        [Test]
        public void SameSeed_ProducesIdenticalValueStream()
        {
            var a = new BattleRng(20260809);
            var b = new BattleRng(20260809);
            for (var i = 0; i < 32; i++)
                Assert.AreEqual(a.Value, b.Value, 1e-7f, $"Mismatch at roll {i}");
        }

        [Test]
        public void DifferentSeeds_Diverge()
        {
            var a = new BattleRng(1);
            var b = new BattleRng(2);
            var same = true;
            for (var i = 0; i < 8; i++)
            {
                if (System.Math.Abs(a.Value - b.Value) > 1e-7f)
                {
                    same = false;
                    break;
                }
            }

            Assert.IsFalse(same);
        }

        [Test]
        public void DeriveFightSeed_IsStableAndDistinct()
        {
            var s0 = BattleRng.DeriveFightSeed(1000, 0);
            var s1 = BattleRng.DeriveFightSeed(1000, 1);
            var s0Again = BattleRng.DeriveFightSeed(1000, 0);
            Assert.AreEqual(s0, s0Again);
            Assert.AreNotEqual(s0, s1);
            Assert.AreNotEqual(0, s0);
        }
    }
}
