using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class BuffEntity
{
    public Status.BuffType type;
    public Status.HealType healType;
    public Status.AttributeType attributeType;
    public int roundsRemaining;
    public float heal;
    public float attribute;
    public Sprite icon;
    public string name;

    public BuffEntity() { }
    public BuffEntity(
        Sprite icon, string name,
        Status.BuffType type,
        Status.HealType healType = Status.HealType.None,
        Status.AttributeType attributeType = Status.AttributeType.None,
        int roundsRemaining = DefaultProperty.defaultBuffRound, float heal = 0f, float attribute = 0f)

    {
        this.type = type;
        this.healType = healType;
        this.attributeType = attributeType;
        this.roundsRemaining = roundsRemaining;
        this.heal = heal;
        this.attribute = attribute;
        this.icon = icon;
        this.name = name;
    }

    public void Purify()
    {
        roundsRemaining = 0;
    }
    public BuffEntity ChangeRounds(int rounds)
    {
        roundsRemaining += rounds;
        return this;
    }

    public BuffEntity SetIcon(Sprite icon)
    {
        this.icon = icon;
        return this;
    }

    public BuffEntity SetName(string name)
    {
        this.name = name;
        return this;
    }

    public BuffEntity SetBuffType(Status.BuffType type)
    {
        this.type = type;
        return this;
    }

    public BuffEntity SetHealType(Status.HealType healType)
    {
        this.healType = healType;
        return this;
    }

    public BuffEntity SetAttributeType(Status.AttributeType attributeType)
    {
        this.attributeType = attributeType;
        return this;
    }

    public BuffEntity SetHeal(float heal)
    {
        this.heal = heal;
        return this;
    }

    public BuffEntity SetAttribute(float attribute)
    {
        this.attribute = attribute;
        return this;
    }
}