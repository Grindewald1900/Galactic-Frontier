using System.Collections.Generic;

namespace Assets.Resources.Scripts.Economy.Domain
{
    /// <summary>One stack granted when a card is dismantled.</summary>
    public sealed class DismantleGrant
    {
        public string ItemDefId = "";
        public int Quantity;
        public int Quality = EconomyConstants.DefaultQuality;
    }

    /// <summary>
    /// Pure dismantle table keyed by CharacterTier ordinal
    /// (None=0, TierE=1 … TierSS=7). No Unity dependency.
    /// </summary>
    public static class DismantleRules
    {
        public const int MinTier = 1;
        public const int MaxTier = 7;

        public static int NormalizeTier(int characterTier)
        {
            if (characterTier < MinTier) return MinTier;
            if (characterTier > MaxTier) return MaxTier;
            return characterTier;
        }

        public static int CreditsForTier(int characterTier)
        {
            return NormalizeTier(characterTier) switch
            {
                1 => 8,
                2 => 16,
                3 => 32,
                4 => 64,
                5 => 120,
                6 => 220,
                7 => 400,
                _ => 8
            };
        }

        public static IReadOnlyList<DismantleGrant> ItemGrantsForTier(int characterTier)
        {
            var t = NormalizeTier(characterTier);
            var q = EconomyConstants.DefaultQuality;
            var list = new List<DismantleGrant>
            {
                Grant(EconomyConstants.ScrapDefId, 2 + t * 2, q)
            };

            if (t >= 3)
                list.Add(Grant("mat_iron_ore", t >= 6 ? 2 : 1, q));
            if (t >= 4)
                list.Add(Grant("mat_crystal_sand", t >= 7 ? 2 : 1, q));
            if (t >= 5)
                list.Add(Grant(GachaRules.TicketDefId, t >= 7 ? 2 : 1, 1));

            return list;
        }

        private static DismantleGrant Grant(string defId, int qty, int quality) =>
            new DismantleGrant { ItemDefId = defId, Quantity = qty, Quality = quality };
    }
}
