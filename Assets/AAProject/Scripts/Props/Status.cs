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
}
