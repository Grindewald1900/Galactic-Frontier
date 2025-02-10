using System;

// 定义伤害类型的枚举
public enum DamageType
{
    DAMAGE,
    SPECIAL_DAMAGE,
    MISS,
    CURE,
}

public enum TargetSelection
{
    Random,       // 随机单体
    Fastest,      // 攻击速度最快
    LowestHP,     // 攻击血量最低
    LowestDefense, // 攻击防御最低
    RandomGroup,   // 随机多人
    FrontRow, // 攻击前排（position == 1或2）
    BackRow   // 攻击后排（position == 3,4,5）
}

[Serializable]
public class DamageEntity
{
    // 伤害数值
    public float damageAmount;
    // 伤害类型
    public DamageType damageType;
    // 是否暴击
    public float criticalMultiplier;
    public Status.BuffType buffType;
    public Status.DebuffType debuffType;

    // 构造函数，用于快速初始化
    public DamageEntity(float damageAmount, DamageType damageType, float criticalMultiplier)
    {
        this.damageAmount = damageAmount;
        this.damageType = damageType;
        this.criticalMultiplier = criticalMultiplier;
    }


    // 可选：重写 ToString 方法，方便调试输出
    public override string ToString()
    {
        return $"Damage: {damageAmount}, Type: {damageType}, Critical: {criticalMultiplier}";
    }

}
