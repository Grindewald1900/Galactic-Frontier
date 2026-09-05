using System;

namespace Assets.Resources.Scripts.Battle.Domain
{
    /// <summary>
    /// Seeded RNG for battle resolution. Settlement code must use this instead of
    /// <c>UnityEngine.Random</c> so the same seed yields the same rolls.
    /// </summary>
    public sealed class BattleRng
    {
        private readonly Random random;

        public long Seed { get; }
        public int CallCount { get; private set; }

        public BattleRng(long seed)
        {
            Seed = seed;
            random = new Random(MixToInt(seed));
            CallCount = 0;
        }

        /// <summary>Uniform value in [0, 1).</summary>
        public float Value
        {
            get
            {
                CallCount++;
                return (float)random.NextDouble();
            }
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            CallCount++;
            return random.Next(minInclusive, maxExclusive);
        }

        /// <summary>Advances the stream to match a previously captured call count.</summary>
        public void Burn(int calls)
        {
            for (var i = 0; i < calls; i++)
            {
                CallCount++;
                random.NextDouble();
            }
        }

        /// <summary>Derives a per-fight seed for AFK multi-fight loops (P2).</summary>
        public static long DeriveFightSeed(long baseSeed, int fightIndex)
        {
            unchecked
            {
                var h = baseSeed;
                h = (h * 397L) ^ fightIndex;
                h ^= h >> 17;
                h *= unchecked((long)0x5851f42d4c957f2dUL);
                h ^= h >> 13;
                return h == 0 ? 1 : h;
            }
        }

        private static int MixToInt(long seed)
        {
            unchecked
            {
                var x = (ulong)seed;
                x ^= x >> 33;
                x *= 0xff51afd7ed558ccdUL;
                x ^= x >> 33;
                x *= 0xc4ceb9fe1a85ec53UL;
                x ^= x >> 33;
                var i = (int)x;
                return i == 0 ? 1 : i;
            }
        }
    }
}
