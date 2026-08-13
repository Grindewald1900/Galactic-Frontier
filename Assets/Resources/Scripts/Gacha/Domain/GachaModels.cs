using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;

namespace Assets.Resources.Scripts.Gacha.Domain
{
    [Serializable]
    public class PlayerGachaState
    {
        public string lastPoolId = "pool_standard";
        public int pityCounter;
        public int totalPulls;
        public int count;
    }

    public sealed class GachaPullResult
    {
        public bool Success;
        public string Message = "";
        public List<CardEntity> Cards = new List<CardEntity>();
        public int TicketsSpent;
        public int PityAfter;
        public int ForcedPityCount;
        public bool Granted;

        public static GachaPullResult Fail(string message) =>
            new GachaPullResult { Success = false, Message = message ?? "" };

        public static GachaPullResult Ok(
            List<CardEntity> cards, int spent, int pity, int forced, bool granted) =>
            new GachaPullResult
            {
                Success = true,
                Cards = cards ?? new List<CardEntity>(),
                TicketsSpent = spent,
                PityAfter = pity,
                ForcedPityCount = forced,
                Granted = granted
            };
    }
}
