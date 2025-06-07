namespace Assets.Resources.Scripts.Props
{
    public static class Status
    {
        public enum BuffType
        {
            Heal,
            AttributeUp,
            None
        }

        public enum DebuffType
        {
            Damage,
            Controll,
            AttributeDown,
            None
        }

        public enum DamageType
        {
            // Damage Type
            Bleeding,
            Burning,
            Electrified,
            Radiated,
            Poisoned,
            None
        }

        public enum HealType
        {
            Health,
            Shield,
            None
        }

        public enum ControllType
        {
            // Controll Type
            Frozen,
            Slowed,
            Stunned,
            Silenced,
            Blinded,
            None
        }

        // Define Enums attributes that can be affected by Buffs, Debuffs and Expertise or skills
        public enum AttributeType
        {
            Health,
            Attack,
            Defense,
            Accuracy,
            Dodge,
            Critical,
            CriticalDamage,
            DamageReduction,
            EnergyGenerateRate,
            Speed,
            None
        }

        public enum BattleReportType
        {
            Damage,
            Injury,
            Heal,
            Shield,
            None
        }

        public static DebuffType GetRandomDebuff()
        {
            return (DebuffType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(DebuffType)).Length);
        }

        public static BuffType GetRandomBuff()
        {
            return (BuffType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(BuffType)).Length);
        }
    }
}