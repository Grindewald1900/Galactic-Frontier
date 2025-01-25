using UnityEngine;

[System.Serializable]
public class CardEntity
{
    // public Sprite cardImageSprite;
    public string cardName;
    public float damage = 10;
    public float speed = 10;
    public float energyIncreaseRate = 10f;
    public float maxHealth = 100f;
    public float maxEnergy = 100f;
    public float maxAttack = 30f;

    public CardEntity(string cardName, float damage, float speed, float energyIncreaseRate, float maxHealth, float maxEnergy, float maxAttack)
    {
        this.cardName = cardName;
        this.damage = damage;
        this.speed = speed;
        this.energyIncreaseRate = energyIncreaseRate;
        this.maxHealth = maxHealth;
        this.maxEnergy = maxEnergy;
        this.maxAttack = maxAttack;
    }

    public CardEntity() { }

    public CardEntity SetCardName(string cardName)
    {
        this.cardName = cardName;
        return this;
    }

    public CardEntity SetCardImage(Sprite cardImageSprite)
    {
        // this.cardImageSprite = cardImageSprite;
        return this;
    }

    public CardEntity SetDamage(float damage)
    {
        this.damage = damage;
        return this;
    }

    public CardEntity SetSpeed(float speed)
    {
        this.speed = speed;
        return this;
    }

    public CardEntity SetEnergyIncreaseRate(float energyIncreaseRate)
    {
        this.energyIncreaseRate = energyIncreaseRate;
        return this;
    }

}

