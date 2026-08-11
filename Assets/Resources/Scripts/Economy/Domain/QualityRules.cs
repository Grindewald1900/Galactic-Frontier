using System;

namespace Assets.Resources.Scripts.Economy.Domain
{
    /// <summary>Quality score → tier with floor/ceiling (systems/05).</summary>
    public static class QualityRules
    {
        public static int ClampQuality(int q) => Math.Max(1, Math.Min(5, q));

        public static int ComputeScore(
            int craftSkillProxy,
            int facilityLevel,
            int inputMinQuality,
            int masteryCount)
        {
            var skill = Math.Max(0, craftSkillProxy);
            var facility = Math.Max(0, facilityLevel);
            var input = ClampQuality(inputMinQuality);
            var mastery = Math.Max(0, masteryCount);
            return skill * 2 + facility * 3 + input * 10 + Math.Min(20, mastery);
        }

        public static int FloorFromInputs(int inputMinQuality, int facilityLevel)
        {
            var floor = ClampQuality(inputMinQuality);
            if (facilityLevel >= 3 && floor < 5)
                floor = Math.Min(5, floor + 1);
            return floor;
        }

        public static int CeilingFromScore(int score)
        {
            if (score >= 70) return 5;
            if (score >= 55) return 4;
            if (score >= 40) return 3;
            if (score >= 25) return 2;
            return 1;
        }

        public static QualityTier ScoreToTier(int score, int inputMinQuality, int facilityLevel)
        {
            var floor = FloorFromInputs(inputMinQuality, facilityLevel);
            var ceiling = Math.Max(floor, CeilingFromScore(score));
            // Deterministic mid bias toward ceiling when score is high within band
            var tier = floor;
            var span = ceiling - floor;
            if (span > 0)
            {
                var band = (CeilingFromScore(score) - floor);
                if (band <= 0) tier = floor;
                else
                {
                    var t = Math.Min(span, Math.Max(0, (score % (span + 1))));
                    tier = floor + t;
                }
            }

            return (QualityTier)ClampQuality(tier);
        }

        /// <summary>Roll with optional RNG [0,1). Null rng → deterministic ScoreToTier.</summary>
        public static QualityTier Roll(
            int craftSkillProxy,
            int facilityLevel,
            int inputMinQuality,
            int masteryCount,
            Func<float> nextUnit01 = null)
        {
            var score = ComputeScore(craftSkillProxy, facilityLevel, inputMinQuality, masteryCount);
            var floor = FloorFromInputs(inputMinQuality, facilityLevel);
            var ceiling = Math.Max(floor, CeilingFromScore(score));
            if (nextUnit01 == null || ceiling <= floor)
                return (QualityTier)ClampQuality(Math.Max(floor, Math.Min(ceiling, (int)ScoreToTier(score, inputMinQuality, facilityLevel))));

            var u = nextUnit01();
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            var span = ceiling - floor;
            var offset = (int)Math.Floor(u * (span + 1));
            if (offset > span) offset = span;
            return (QualityTier)ClampQuality(floor + offset);
        }

        public static string PreviewRange(int craftSkillProxy, int facilityLevel, int inputMinQuality, int masteryCount)
        {
            var score = ComputeScore(craftSkillProxy, facilityLevel, inputMinQuality, masteryCount);
            var floor = FloorFromInputs(inputMinQuality, facilityLevel);
            var ceiling = Math.Max(floor, CeilingFromScore(score));
            return floor == ceiling ? $"Q{floor}" : $"Q{floor}–Q{ceiling}";
        }
    }
}
