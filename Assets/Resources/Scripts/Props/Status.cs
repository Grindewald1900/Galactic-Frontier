public static class Status
{
    public enum BuffType
    {
        AttackUp,
        DefenseUp,
        SpeedUp,
        Haste,
        CritUp,
        DamageUp,
        None
    }


    public enum DebuffType
    {
        // Damage Type
        Bleeding,
        Burning,
        Electrified,
        Radiated,
        Poisoned,
        // Controll Type
        Frozen,
        Slowed,
        Stunned,
        Silenced,
        // Other
        Blinded,
        DefenseDown,
        AttackDown,
        Vulnerable,
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