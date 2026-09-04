using System;

namespace Assets.Resources.Scripts.Progression.Domain
{
    /// <summary>Aptitudes, XP baselines, and energy-rank ascend costs (scrap + credits).</summary>
    public static class ProgressionCatalog
    {
        public const float ProfessionXpPerSecond = 1f;
        public const float MainBattleCombatXp = 80f;
        public const float FarmCombatXpPerCycle = 18f;
        public const float DangerousGatherCombatXp = 6f;
        public const float CommanderBattleXp = 40f;
        public const float CommanderFarmXp = 12f;
        public const float CommanderGatherXp = 10f;
        public const float CommanderCraftXp = 10f;
        public const float CommanderScanXp = 8f;
        public const float CommanderRepairXp = 8f;
        public const string ScrapDefId = "mat_scrap";

        public static float Aptitude(string characterName, ProfessionSkill skill)
        {
            if (string.IsNullOrEmpty(characterName) || skill == ProfessionSkill.None)
                return 1f;

            switch (characterName)
            {
                case "Asra":
                    return skill == ProfessionSkill.Craft || skill == ProfessionSkill.Logistics ? 1.25f : 1f;
                case "Magki":
                    return skill == ProfessionSkill.Gather || skill == ProfessionSkill.Navigate ? 1.20f : 1f;
                case "Sernia":
                    return skill == ProfessionSkill.Scan ? 1.35f : 1f;
                case "Cole":
                    return skill == ProfessionSkill.Navigate ? 1.30f : 1f;
                case "Mia":
                case "Mira":
                    return skill == ProfessionSkill.Logistics ? 1.30f : 1f;
                default:
                    return 1f;
            }
        }

        public static bool TryGetAscensionCost(EnergyRank from, out RankAscensionCost cost)
        {
            cost = default;
            from = ProgressionRules.ClampRank(from);
            if (from >= EnergyRank.S)
                return false;

            var to = ProgressionRules.NextRank(from);
            cost.From = from;
            cost.To = to;
            switch (from)
            {
                case EnergyRank.F:
                    cost.ScrapQty = 20;
                    cost.CreditQty = 20;
                    return true;
                case EnergyRank.E:
                    cost.ScrapQty = 40;
                    cost.CreditQty = 50;
                    return true;
                case EnergyRank.D:
                    cost.ScrapQty = 80;
                    cost.CreditQty = 100;
                    return true;
                case EnergyRank.C:
                    cost.ScrapQty = 120;
                    cost.CreditQty = 200;
                    return true;
                case EnergyRank.B:
                    cost.ScrapQty = 160;
                    cost.CreditQty = 350;
                    return true;
                case EnergyRank.A:
                    cost.ScrapQty = 240;
                    cost.CreditQty = 500;
                    return true;
                default:
                    return false;
            }
        }

        public static ProfessionSpec RecommendedSpec(ProfessionSkill skill, int milestoneLevel)
        {
            if (milestoneLevel == 10) return ProfessionSpec.Precision;
            if (milestoneLevel == 20) return ProfessionSpec.Throughput;
            if (milestoneLevel == 30) return ProfessionSpec.Economy;
            return ProfessionSpec.None;
        }

        public static ProfessionSpec CycleSpec(ProfessionSpec current)
        {
            return current switch
            {
                ProfessionSpec.Precision => ProfessionSpec.Throughput,
                ProfessionSpec.Throughput => ProfessionSpec.Economy,
                ProfessionSpec.Economy => ProfessionSpec.Precision,
                _ => ProfessionSpec.Precision
            };
        }
    }
}
