using System;

namespace Assets.Resources.Scripts.Progression.Domain
{
    public enum EnergyRank
    {
        F = 0,
        E = 1,
        D = 2,
        C = 3,
        B = 4,
        A = 5,
        S = 6
    }

    public enum ProfessionSkill
    {
        None = 0,
        Gather = 1,
        Craft = 2,
        Scan = 3,
        Navigate = 4,
        Logistics = 5
    }

    public enum ProfessionSpec
    {
        None = 0,
        Precision = 1,
        Throughput = 2,
        Economy = 3
    }

    public struct CombatXpState
    {
        public int Level;
        public float CurrentXp;
        public float ExpToNext;
        public float StoredXp;
        public int LevelsGained;
        public bool AtCap;
    }

    public struct ProfessionXpState
    {
        public int Level;
        public float CurrentXp;
        public float StoredXp;
        public int LevelsGained;
        public bool AtCap;
        public bool HitMilestone;
        public int MilestoneLevel;
    }

    public struct CommanderXpState
    {
        public int Level;
        public float CurrentXp;
        public float ExpToNext;
        public int LevelsGained;
    }

    public struct RankAscensionCost
    {
        public EnergyRank From;
        public EnergyRank To;
        public int ScrapQty;
        public int CreditQty;
    }
}
