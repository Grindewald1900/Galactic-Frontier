
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
        Paralyzed,
        Slowed,
        Stunned,
        Silenced,
        Restrained,
        // Other
        Blinded,
        DefenseDown,
        AttackDown,
        Vulnerable
    }
}
