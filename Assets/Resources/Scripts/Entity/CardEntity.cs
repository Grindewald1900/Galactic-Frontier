using System;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Characters;

namespace Assets.Resources.Scripts.Entity
{
    [Serializable]
    public class CardEntity
    {
        public string cardName = "Default";
        public CharacterName characterName = CharacterName.Default;
        public LineupPosition position = LineupPosition.None;
        public Archetype archetype;
        public string id = "";
        public int level = 1;
        public int exp = 0;
        public CharacterTier characterTier = CharacterTier.None;
        public float health = 100f;
        public float healthCoefficient = 1f;
        public float attack = 10f;
        public float attackCoefficient = 1f;
        public float defense = 10f;
        public float defenseCoefficient = 1f;
        public float score = 5f;
        public CardType cardType;
        public float accuracy = 0.8f;
        public float accuracyCoefficient = 1f;
        public float dodge = 0.1f;
        public float dodgeCoefficient = 1f;
        public float critical = 0.1f;
        public float criticalCoefficient = 1f;
        public float criticalDamage = 1.5f;
        public float criticalDamageCoefficient = 1f;
        public float dagameReduction = 0f;
        public float dagameReductionCoefficient = 1f;
        public float energyGenerateRate = 10f;
        public float energyGenerateRateCoefficient = 1f;
        public float speed = 30f;
        public float speedCoefficient = 1f;
        public float maxEnergy = 100f;
        public float maxAttack = 100f;

        public CardEntity()
        {
            id = Guid.NewGuid().ToString();
        }

        public CardEntity(string cardName, CharacterName character, Archetype archetype, string id, int level, int exp,
        CharacterTier characterTier, float health, float healthCoefficient, float attack, float attackCoefficient, float defense,
        float defenseCoefficient, float score, CardType cardType, float accuracy, float accuracyCoefficient, float dodge,
        float dodgeCoefficient, float critical, float criticalCoefficient, float criticalDamage, float criticalDamageCoefficient,

        float dagameReduction, float dagameReductionCoefficient, int energyGenerateRate, int energyGenerateRateCoefficient, float speed,
        float speedCoefficient, float maxEnergy, float maxAttack)
        {
            this.cardName = cardName;
            characterName = character;
            this.archetype = archetype;
            this.id = Guid.NewGuid().ToString();
            this.level = level;
            this.exp = exp;
            this.health = health;
            this.healthCoefficient = healthCoefficient;

            this.attack = attack;
            this.attackCoefficient = attackCoefficient;
            this.defense = defense;
            this.defenseCoefficient = defenseCoefficient;
            this.score = score;
            this.cardType = cardType;
            this.accuracy = accuracy;
            this.accuracyCoefficient = accuracyCoefficient;
            this.dodge = dodge;
            this.dodgeCoefficient = dodgeCoefficient;
            this.critical = critical;
            this.criticalCoefficient = criticalCoefficient;
            this.criticalDamage = criticalDamage;
            this.criticalDamageCoefficient = criticalDamageCoefficient;
            this.dagameReduction = dagameReduction;
            this.dagameReductionCoefficient = dagameReductionCoefficient;
            this.energyGenerateRate = energyGenerateRate;
            this.energyGenerateRateCoefficient = energyGenerateRateCoefficient;
            this.speed = speed;
            this.speedCoefficient = speedCoefficient;
            this.maxEnergy = maxEnergy;
            this.maxAttack = maxAttack;
        }

        public CardEntity SetCardName(string cardName)
        {
            this.cardName = cardName;
            return this;
        }

        public CardEntity SetCharacterName(CharacterName character)
        {
            characterName = character;
            return this;
        }

        public CardEntity SetArchetype(Archetype archetype)
        {
            this.archetype = archetype;
            return this;
        }

        public CardEntity SetId(string id)
        {
            this.id = id;
            return this;
        }

        public CardEntity SetLevel(int level)
        {
            this.level = level;
            return this;
        }

        public CardEntity SetExp(int exp)
        {
            this.exp = exp;
            return this;
        }

        public CardEntity SetCharacterTier(CharacterTier characterTier)
        {
            this.characterTier = characterTier;
            return this;
        }

        public CardEntity SetHealth(float health)
        {
            this.health = health;
            return this;
        }

        public CardEntity SetHealthCoefficient(float healthCoefficient)
        {
            this.healthCoefficient = healthCoefficient;
            return this;
        }

        public CardEntity SetAttack(float attack)
        {
            this.attack = attack;
            return this;
        }

        public CardEntity SetDefense(float defense)
        {
            this.defense = defense;
            return this;
        }

        public CardEntity SetScore(float score)
        {
            this.score = score;
            return this;
        }

        public CardEntity SetCardType(CardType cardType)
        {
            this.cardType = cardType;
            return this;
        }

        public CardEntity SetAccuracy(float accuracy)
        {
            this.accuracy = accuracy;
            return this;
        }

        public CardEntity SetAccuracyCoefficient(float accuracyCoefficient)
        {
            this.accuracyCoefficient = accuracyCoefficient;
            return this;
        }

        public CardEntity SetDodge(float dodge)
        {
            this.dodge = dodge;
            return this;
        }

        public CardEntity SetDodgeCoefficient(float dodgeCoefficient)
        {
            this.dodgeCoefficient = dodgeCoefficient;
            return this;
        }

        public CardEntity SetCritical(float critical)
        {
            this.critical = critical;
            return this;
        }

        public CardEntity SetCriticalCoefficient(float criticalCoefficient)
        {
            this.criticalCoefficient = criticalCoefficient;
            return this;
        }

        public CardEntity SetCriticalDamage(float criticalDamage)
        {
            this.criticalDamage = criticalDamage;
            return this;
        }

        public CardEntity SetCriticalDamageCoefficient(float criticalDamageCoefficient)
        {
            this.criticalDamageCoefficient = criticalDamageCoefficient;
            return this;
        }

        public CardEntity SetDagameReduction(float dagameReduction)
        {
            this.dagameReduction = dagameReduction;
            return this;
        }

        public CardEntity SetDagameReductionCoefficient(float dagameReductionCoefficient)
        {
            this.dagameReductionCoefficient = dagameReductionCoefficient;
            return this;
        }

        public CardEntity SetEnergyGenerateRate(int energyGenerateRate)
        {
            this.energyGenerateRate = energyGenerateRate;
            return this;
        }

        public CardEntity SetEnergyGenerateRateCoefficient(int energyGenerateRateCoefficient)
        {
            this.energyGenerateRateCoefficient = energyGenerateRateCoefficient;
            return this;
        }

        public CardEntity SetSpeed(float speed)
        {
            this.speed = speed;
            return this;
        }

        public CardEntity SetSpeedCoefficient(float speedCoefficient)
        {
            this.speedCoefficient = speedCoefficient;
            return this;
        }

        public CardEntity SetMaxEnergy(float maxEnergy)
        {
            this.maxEnergy = maxEnergy;
            return this;
        }

        public CardEntity SetMaxAttack(float maxAttack)
        {
            this.maxAttack = maxAttack;
            return this;
        }

        public CardEntity SetLineupPosition(LineupPosition position)
        {
            this.position = position;
            return this;
        }

        public LineupPosition GetLineupPosition()
        {
            return position;
        }

        public enum CardType
        {
            Mechanician,
            Magician,
            Monster,
            Potioneer,
            Warrior,
            Assassin
        }
    }

    public enum LineupPosition
    {
        One,
        Two,
        Three,
        Four,
        Five,
        None
    }
}