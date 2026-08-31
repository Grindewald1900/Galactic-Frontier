using System;
using System.Text;

namespace Assets.Resources.Scripts.World.Domain
{
    public static class UniverseSeedUtil
    {
        public const int DefaultServerSeed = 0x47A1_2026;
        public const int DefaultSeasonId = 1;

        /// <summary>Hash(server + plane + season) per doc 23 §2.1.</summary>
        public static int ComposeSeed(int serverSeed, string planeId, int seasonId)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + serverSeed;
                h = h * 31 + seasonId;
                if (!string.IsNullOrEmpty(planeId))
                {
                    foreach (char c in planeId)
                        h = h * 31 + c;
                }

                return h == 0 ? 1 : h;
            }
        }

        public static int FromPlayerId(string playerId, int serverSeed = DefaultServerSeed, int seasonId = DefaultSeasonId)
        {
            string plane = string.IsNullOrEmpty(playerId) ? "solo" : playerId;
            return ComposeSeed(serverSeed, plane, seasonId);
        }

        public static int MixToInt(int seed)
        {
            unchecked
            {
                ulong x = (ulong)(uint)seed;
                x ^= x >> 33;
                x *= 0xff51afd7ed558ccdUL;
                x ^= x >> 33;
                x *= 0xc4ceb9fe1a85ec53UL;
                x ^= x >> 33;
                int i = (int)x;
                return i == 0 ? 1 : i;
            }
        }
    }
}
