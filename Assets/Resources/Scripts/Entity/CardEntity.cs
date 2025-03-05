using System;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Props;
using Assets.Scripts.Utils;

namespace Assets.Resources.Scripts.Entity
{
    [Serializable]
    public class CardEntity
    {
        public string cardName = "";
        public CharacterName characterName = CharacterName.Default;
        public LineupPosition position = LineupPosition.None;
        public Archetype archetype;
        public string id = "";
        public int level = 1; // shown on main panel
        public int levelStepForEvolution = 20;
        public bool evolutionPending = false;
        public float currentExp = 0f; // shown on main panel
        public float expToLevelUp = 0f;
        public float power = 0f; // shown on main panel
        public CharacterTier characterTier = CharacterTier.None; // shown on main panel
        public float health = 100f;
        public float healthCoefficient = 1f;
        public float attack = 10f;
        public float attackCoefficient = 1f;
        public float defense = 10f;
        public float defenseCoefficient = 1f;
        public float score = 5f;
        public CardType cardType; // shown on main panel
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
        /// <summary>
        /// All the card attributes and expertises are stored here.
        /// </summary>
        ///<remarks>
        /// All expertises for a card, should be applied to <see cref="CardPreviewController"/>
        /// battleAttributes should be applied to <see cref="BattleController"/>
        /// </remarks>
        public List<ExpertiseEntity> expertises = new();
        // Attributes for the card panel e.g. <Attack, 1.5f>
        Dictionary<Status.AttributeType, float> panelAttributes = new();
        Dictionary<Status.AttributeType, float> battleAttributes = new();

        public CardEntity()
        {
            id = Guid.NewGuid().ToString();
            InitAttributes();
            UpdateExpertises();
        }

        private void InitAttributes()
        {
            foreach (Status.AttributeType attribute in Enum.GetValues(typeof(Status.AttributeType)))
            {
                panelAttributes[attribute] = 1f;
                battleAttributes[attribute] = 1f;
            }
        }

        private void UpdateExpertises()
        {
            foreach (ExpertiseEntity expertise in expertises)
            {
                panelAttributes[expertise.attributeType] += expertise.value;
            }
        }

        public void ChangeBattleAttribute(Status.AttributeType attributeType, float value)
        {
            battleAttributes[attributeType] += value;
        }

        public void AddExpertise(ExpertiseEntity expertise)
        {
            if (expertises.Contains(expertise))
            {
                return;
            }
            expertises.Add(expertise);
            UpdateExpertises();
        }

        public void RemoveExpertise(ExpertiseEntity expertise)
        {
            if (!expertises.Contains(expertise))
            {
                return;
            }
            expertises.Remove(expertise);
            UpdateExpertises();
        }

        // 模拟每场战斗后获得经验（在 BattleController 中调用）
        public void AddExperience(float exp)
        {
            if (evolutionPending)
            {
                UnityEngine.Debug.Log($"{cardName} 已达到进阶等级，进阶前无法获得经验。");
                return;
            }

            currentExp += exp;
            UnityEngine.Debug.Log($"{cardName} 获得 {exp} 经验，总经验 {currentExp}/{expToLevelUp}");
            // 多段升级判断：当经验超过升级需求时，进行多次升级处理
            while (currentExp >= expToLevelUp)
            {
                currentExp -= expToLevelUp;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            level++;
            UnityEngine.Debug.Log($"{cardName} 升级到 {level} 级！");
            // 升级后重置经验和升级需求
            expToLevelUp = LevelUtil.GetNextLevelExp(level);

            // 每达到levelStepForEvolution的倍数，要求进阶（例如20级、40级……）
            if (level % levelStepForEvolution == 0)
            {
                evolutionPending = true;
                UnityEngine.Debug.Log($"{cardName} 达到 {level} 级，需要进阶才能继续获得经验！");
            }
        }

        // Other Setters and Getters
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

        public CardEntity SetExp(float exp)
        {
            this.currentExp = exp;
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