using Assets.Resources.Scripts.Economy.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class QualityRulesTests
    {
        [Test]
        public void Floor_Raises_With_Facility()
        {
            Assert.AreEqual(2, QualityRules.FloorFromInputs(2, 0));
            Assert.AreEqual(3, QualityRules.FloorFromInputs(2, 3));
        }

        [Test]
        public void ScoreToTier_RespectsFloorCeiling()
        {
            var tier = QualityRules.ScoreToTier(10, 2, 0);
            Assert.GreaterOrEqual((int)tier, 2);
            Assert.LessOrEqual((int)tier, 5);
        }

        [Test]
        public void Roll_Deterministic_WithoutRng()
        {
            var a = QualityRules.Roll(5, 1, 2, 0, null);
            var b = QualityRules.Roll(5, 1, 2, 0, null);
            Assert.AreEqual(a, b);
        }

        [Test]
        public void PreviewRange_Format()
        {
            var preview = QualityRules.PreviewRange(1, 0, 2, 0);
            Assert.IsTrue(preview.StartsWith("Q"));
        }
    }
}
