using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    [Serializable]
    public class LootEntry
    {
        public string itemDefId = "";
        public int qtyMin = 1;
        public int qtyMax = 1;
        public int quality = 2;
        public float chance = 1f;
    }

    [Serializable]
    public class RewardTable
    {
        public string tableId = "";
        public List<LootEntry> guaranteed = new List<LootEntry>();
        public List<LootEntry> weighted = new List<LootEntry>();
    }

    public sealed class LootGrant
    {
        public string itemDefId = "";
        public int quantity = 1;
        public int quality = 2;
        public bool IsCredit => itemDefId == RewardConstants.CreditItemId;
    }

    public static class RewardConstants
    {
        public const string CreditItemId = "credit";
    }
}
