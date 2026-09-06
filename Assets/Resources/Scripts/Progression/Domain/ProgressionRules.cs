using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.Progression.Domain
{
    /// <summary>Pure auto-growth math: caps, XP curves, shares, match, team power, catch-up.</summary>
    public static class ProgressionRules
    {
        public const int MaxCommanderLevel = 99;
        public const int MaxCombatLevel = 100;
        public const int MaxProfessionLevel = 60;
        public const float IdleCombatShare = 0.10f;
        public const float KoCombatShare = 0.80f;
        public const float LeaderShare = 1.00f;
        public const float MainAssistantShare = 0.60f;
        public const float OtherShare = 0.35f;
        public const float AssistantTeamWeight = 0.20f;

        public static EnergyRank ClampRank(EnergyRank rank)
        {
            if ((int)rank < (int)EnergyRank.F) return EnergyRank.F;
            if ((int)rank > (int)EnergyRank.S) return EnergyRank.S;
            return rank;
        }

        public static EnergyRank NextRank(EnergyRank rank)
        {
            rank = ClampRank(rank);
            return rank >= EnergyRank.S ? EnergyRank.S : (EnergyRank)((int)rank + 1);
        }

        public static int CombatCap(EnergyRank rank) => ClampRank(rank) switch
        {
            EnergyRank.F => 10,
            EnergyRank.E => 20,
            EnergyRank.D => 35,
            EnergyRank.C => 50,
            EnergyRank.B => 70,
            EnergyRank.A => 90,
            EnergyRank.S => 100,
            _ => 10
        };

        public static int SkillCap(EnergyRank rank) => ClampRank(rank) switch
        {
            EnergyRank.F => 5,
            EnergyRank.E => 10,
            EnergyRank.D => 20,
            EnergyRank.C => 30,
            EnergyRank.B => 40,
            EnergyRank.A => 50,
            EnergyRank.S => 60,
            _ => 5
        };

        /// <summary>XP required to leave <paramref name="currentLevel"/> for the next combat level.</summary>
        public static float CombatExpToNext(int currentLevel)
        {
            if (currentLevel < 0) return 0f;
            if (currentLevel == 0) return 50f;
            if (currentLevel <= 20) return currentLevel * 100f;
            if (currentLevel <= 40) return CombatExpToNext(20) + 200f * (currentLevel - 20);
            return currentLevel * 1000f;
        }

        public static float ProfessionExpToNext(int currentLevel)
        {
            if (currentLevel < 1) return 40f;
            if (currentLevel <= 10) return 40f + currentLevel * 30f;
            if (currentLevel <= 30) return 200f + currentLevel * 40f;
            return currentLevel * 80f;
        }

        public static float CommanderExpToNext(int currentLevel)
        {
            var level = Math.Max(1, currentLevel);
            if (level <= 10) return 60f + level * 40f;
            if (level <= 20) return 200f + level * 50f;
            if (level <= 40) return 400f + level * 80f;
            return level * 120f;
        }

        public static float MatchMultiplier(int skillLevel, int requiredLevel)
        {
            if (requiredLevel <= 0) return 1f;
            var delta = requiredLevel - Math.Max(1, skillLevel);
            if (delta >= 1) return 1.2f;
            if (delta == 0) return 1.0f;
            if (delta >= -5) return 0.7f;
            if (delta >= -10) return 0.3f;
            return 0.05f;
        }

        public static float JobShare(int memberIndex, bool combat, bool knockedOut)
        {
            if (combat)
                return knockedOut ? KoCombatShare : 1f;
            if (memberIndex <= 0) return LeaderShare;
            if (memberIndex == 1) return MainAssistantShare;
            return OtherShare;
        }

        public static float ScaledXp(float baseAmount, float timeSeconds, float match, float share, float aptitude)
        {
            if (baseAmount <= 0f || timeSeconds <= 0f) return 0f;
            return baseAmount * timeSeconds * Math.Max(0f, match) * Math.Max(0f, share) * Math.Max(0f, aptitude);
        }

        /// <summary>Leader skill + top two assistants × 20% + facility level.</summary>
        public static float TeamPower(IList<int> orderedSkillLevels, int facilityLevel)
        {
            var facility = Math.Max(0, facilityLevel);
            if (orderedSkillLevels == null || orderedSkillLevels.Count == 0)
                return facility;

            var leader = Math.Max(0, orderedSkillLevels[0]);
            var first = 0;
            var second = 0;
            for (var i = 1; i < orderedSkillLevels.Count; i++)
            {
                var skill = Math.Max(0, orderedSkillLevels[i]);
                if (skill > first)
                {
                    second = first;
                    first = skill;
                }
                else if (skill > second)
                {
                    second = skill;
                }
            }

            return leader + first * AssistantTeamWeight + second * AssistantTeamWeight + facility;
        }

        public static float AdjustedCycleSeconds(int baseSeconds, float teamPower)
        {
            var baseCycle = Math.Max(1, baseSeconds);
            var scaled = baseCycle / (1f + 0.01f * Math.Max(0f, teamPower));
            var floor = baseCycle * 0.25f;
            if (scaled < floor) scaled = floor;
            return scaled;
        }

        public static int WorkshopThresholdCorrection(int workshopLevel)
        {
            if (workshopLevel >= 4) return 2;
            if (workshopLevel >= 2) return 1;
            return 0;
        }

        public static bool LeaderMeetsGate(int bestSkill, int requiredLevel, int workshopCorrection)
        {
            if (requiredLevel <= 0) return true;
            return Math.Max(0, bestSkill) + Math.Max(0, workshopCorrection) >= requiredLevel;
        }

        public static int CommanderBandCombatLevel(int commanderLevel)
        {
            var level = Math.Max(1, commanderLevel);
            if (level <= 10) return 1;
            if (level <= 20) return 10;
            if (level <= 30) return 20;
            if (level <= 40) return 30;
            return (level / 10) * 10;
        }

        public static int CatchUpCombatLevel(int highestCombatLevel, int commanderLevel)
        {
            var fromHighest = Math.Max(1, (int)Math.Floor(Math.Max(0, highestCombatLevel) * 0.7));
            var band = CommanderBandCombatLevel(commanderLevel);
            return Math.Max(1, Math.Max(fromHighest, band));
        }

        public static int CommanderOfflineBonusSeconds(int commanderLevel)
        {
            var level = Math.Max(1, commanderLevel);
            var bonus = 0;
            if (level >= 5) bonus += 1800;
            if (level >= 10) bonus += 1800;
            if (level >= 15) bonus += 3600;
            if (level >= 20) bonus += 3600;
            return bonus;
        }

        public static bool IsProfessionMilestone(int newLevel) =>
            newLevel == 10 || newLevel == 20 || newLevel == 30;

        public static CombatXpState ApplyCombatXp(CombatXpState state, EnergyRank rank, float added)
        {
            var cap = MaxCombatLevel;
            state.Level = Math.Max(1, state.Level);
            state.CurrentXp += Math.Max(0f, state.StoredXp);
            state.StoredXp = 0f;
            if (added > 0f)
                state.CurrentXp += added;
            state.LevelsGained = 0;
            state = ResolveCombatOverflow(state, cap);
            state.AtCap = state.Level >= CombatCap(rank) || state.Level >= cap;
            return state;
        }

        private static CombatXpState ResolveCombatOverflow(CombatXpState state, int cap)
        {
            state.ExpToNext = CombatExpToNext(state.Level);
            while (state.Level < cap && state.CurrentXp >= state.ExpToNext && state.ExpToNext > 0f)
            {
                state.CurrentXp -= state.ExpToNext;
                state.Level++;
                state.LevelsGained++;
                state.ExpToNext = CombatExpToNext(state.Level);
                if (state.Level >= cap)
                {
                    state.StoredXp += state.CurrentXp;
                    state.CurrentXp = 0f;
                    break;
                }
            }

            state.AtCap = state.Level >= cap;
            return state;
        }

        public static CombatXpState DumpStoredCombatXp(CombatXpState state, EnergyRank rank) =>
            ApplyCombatXp(state, rank, 0f);

        public static float StoredXpForSkippedLevels(int fromLevel, int toLevel)
        {
            if (toLevel <= fromLevel) return 0f;
            float sum = 0f;
            for (var level = fromLevel; level < toLevel; level++)
                sum += CombatExpToNext(level);
            return sum;
        }

        public static ProfessionXpState ApplyProfessionXp(ProfessionXpState state, EnergyRank rank, float added)
        {
            var cap = MaxProfessionLevel;
            state.Level = Math.Max(1, state.Level);
            state.CurrentXp += Math.Max(0f, state.StoredXp);
            state.StoredXp = 0f;
            state.LevelsGained = 0;
            state.HitMilestone = false;
            state.MilestoneLevel = 0;
            if (added > 0f)
                state.CurrentXp += added;
            state = ResolveProfessionOverflow(state, cap);
            state.AtCap = state.Level >= SkillCap(rank) || state.Level >= cap;
            return state;
        }

        private static ProfessionXpState ResolveProfessionOverflow(ProfessionXpState state, int cap)
        {
            var need = ProfessionExpToNext(state.Level);
            while (state.Level < cap && state.CurrentXp >= need && need > 0f)
            {
                state.CurrentXp -= need;
                state.Level++;
                state.LevelsGained++;
                if (IsProfessionMilestone(state.Level))
                {
                    state.HitMilestone = true;
                    state.MilestoneLevel = state.Level;
                }

                need = ProfessionExpToNext(state.Level);
                if (state.Level >= cap)
                {
                    state.StoredXp += state.CurrentXp;
                    state.CurrentXp = 0f;
                    break;
                }
            }

            state.AtCap = state.Level >= cap;
            return state;
        }

        public static ProfessionXpState DumpStoredProfessionXp(ProfessionXpState state, EnergyRank rank) =>
            ApplyProfessionXp(state, rank, 0f);

        public static CommanderXpState ApplyCommanderXp(CommanderXpState state, float added)
        {
            if (state.Level < 1) state.Level = 1;
            state.ExpToNext = CommanderExpToNext(state.Level);
            state.LevelsGained = 0;
            if (added > 0f)
                state.CurrentXp += added;
            while (state.Level < MaxCommanderLevel && state.CurrentXp >= state.ExpToNext && state.ExpToNext > 0f)
            {
                state.CurrentXp -= state.ExpToNext;
                state.Level++;
                state.LevelsGained++;
                state.ExpToNext = CommanderExpToNext(state.Level);
            }

            return state;
        }
    }
}
