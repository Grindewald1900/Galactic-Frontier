using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.Utils;

namespace Assets.Resources.Scripts.Entity
{
    /// <summary>
    /// Serializable source of truth for a collectible character card.
    /// It contains persistent identity and progression data plus transient battle multipliers.
    /// Card MonoBehaviours render and animate this model but do not own its gameplay state.
    /// </summary>
    /// <remarks>
    /// JsonUtility serializes fields rather than properties. Keep the public backing fields compatible
    /// with existing saves, and avoid reordering persisted enums without a migration.
    /// </remarks>
    [Serializable]
    public class CardEntity
    {
        public string cardName = "";
        public CharacterName characterName = CharacterName.Default;
        public LineupPosition position = LineupPosition.None;
        public Archetype archetype;
        public string id = "";
        public float power = 0f; // shown on main panel
        /// <summary>P2 AFK farm wear; pauses AutoCombat at WorldConstants.FarmWearPauseThreshold.</summary>
        public int farmWear;
        public CardSource cardSource = CardSource.None;
        public CardBoundReason boundReason = CardBoundReason.None;

        public int Level
        {
            get => level;
            set
            {
                level = value;
                CalculatePower();
            }
        } // shown on main panel
        public bool EvolutionPending
        {
            get => evolutionPending;
            set
            {
                evolutionPending = value;
                CalculatePower();
            }
        }
        public float CurrentExp
        {
            get => currentExp;
            set
            {
                currentExp = value;
                CalculatePower();
            }
        }
        public float ExpToNextLevel
        {
            get => expToLevelUp;
            set
            {
                expToLevelUp = value;
                CalculatePower();
            }
        }
        public CharacterTier CharacterTier
        {
            get => characterTier;
            set
            {
                characterTier = value;
                CalculatePower();
            }
        }
        public float Health
        {
            get => health;
            set
            {
                health = value;
                CalculatePower();
            }
        }
        public float Attack
        {
            get => attack;
            set
            {
                attack = value;
                CalculatePower();
            }
        }
        public float Defense
        {
            get => defense;
            set
            {
                defense = value;
                CalculatePower();
            }
        }
        public float Accuracy
        {
            get => accuracy;
            set
            {
                accuracy = value;
                CalculatePower();
            }
        }
        public float Dodge
        {
            get => dodge;
            set
            {
                dodge = value;
                CalculatePower();
            }
        }
        public float Critical
        {
            get => critical;
            set
            {
                critical = value;
                CalculatePower();
            }
        }
        public float CriticalDamage
        {
            get => criticalDamage;
            set
            {
                criticalDamage = value;
                CalculatePower();
            }
        }
        public float DamageReduction
        {
            get => damageReduction;
            set
            {
                damageReduction = value;
                CalculatePower();
            }
        }
        public float EnergyGenerateRate
        {
            get => energyGenerateRate;
            set
            {
                energyGenerateRate = value;
                CalculatePower();
            }
        }
        public float Speed
        {
            get => speed;
            set
            {
                speed = value;
                CalculatePower();
            }
        }
        public float expToLevelUp = 50f;
        public CharacterTier characterTier = CharacterTier.None; // shown on main panel
        public float health = 100f;
        public float attack = 10f;
        public float defense = 10f;
        public float accuracy = 0.8f;
        public float dodge = 0.1f;
        public float critical = 0.1f;
        public float criticalDamage = 1.2f;
        public float damageReduction = 0f;
        public float energyGenerateRate = 10f;
        public float speed = 30f;
        public float currentExp; // Backing field
        public int level; // Backing field
        public bool evolutionPending; // Backing field
        public float maxEnergy = 100f;
        public float maxAttack = 100f;
        public float score = 5f;
        /// <summary>
        /// All the card attributes and expertises are stored here.
        /// </summary>
        ///<remarks>
        /// All expertises for a card, should be applied to <see cref="CardPreviewController"/>
        /// battleAttributes should be applied to <see cref="BattleController"/>
        /// </remarks>
        public List<ExpertiseEntity> expertises = new();
        // Each archetype should have its own specialization
        public List<ExpertiseEntity> characterExpertises = new();
        public List<SkillEntity> skills = new();
        // Displayed attributes = base value * characterAttrs * panelAttrs.
        // Battle attributes add battleAttrs as a final transient multiplier layer.
        Dictionary<Status.AttributeType, float> characterAttrs = new();
        Dictionary<Status.AttributeType, float> panelAttrs = new();
        Dictionary<Status.AttributeType, float> battleAttrs = new();
        private readonly int levelStepForEvolution = 20;
        /// <summary>Raised whenever a derived value changes and views should refresh.</summary>
        public event Action OnDataChanged;

        /// <summary>Raised after an evolution applies a new expertise.</summary>
        public event Action OnCardUpgraded;

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
                characterAttrs[attribute] = 1f;
                panelAttrs[attribute] = 1f;
                battleAttrs[attribute] = 1f;
            }
            if (characterName != CharacterName.Default)
            {
                characterExpertises = CardDataManager.Instance.GetCharacterExpertises(characterName);
            }
        }

        private void UpdateExpertises()
        {
            foreach (ExpertiseEntity expertise in expertises)
            {
                panelAttrs[expertise.attributeType] += expertise.value;
            }
        }

        private void UpdateCharacterExpertises()
        {
            foreach (ExpertiseEntity expertise in characterExpertises)
            {
                characterAttrs[expertise.attributeType] += expertise.value;
            }
        }

        /// <summary>Applies a battle-only multiplier delta without changing persisted base stats.</summary>
        public void ChangeBattleAttribute(Status.AttributeType attributeType, float value)
        {
            battleAttrs[attributeType] += value;
        }

        public void AddExpertise(ExpertiseEntity expertise)
        {
            if (expertises.Contains(expertise))
            {
                return;
            }
            expertises.Add(expertise);
            // Sort the expertises by tier
            expertises.Sort((a, b) => b.expertiseTier.CompareTo(a.expertiseTier));
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

        /// <summary>Adds battle experience, performs chained level-ups, and stops at evolution gates.</summary>
        public void AddExperience(float exp)
        {
            if (EvolutionPending)
            {
                UnityEngine.Debug.Log($"{cardName} 已达到进阶等级，进阶前无法获得经验。");
                return;
            }

            CurrentExp += exp;
            UnityEngine.Debug.Log($"{cardName} 获得 {exp} 经验，总经验 {CurrentExp}/{ExpToNextLevel}");
            // 多段升级判断：当经验超过升级需求时，进行多次升级处理
            while (CurrentExp >= ExpToNextLevel)
            {
                CurrentExp -= ExpToNextLevel;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            Level++;
            UnityEngine.Debug.Log($"{cardName} 升级到 {Level} 级！");
            // 升级后重置经验和升级需求
            ExpToNextLevel = LevelUtil.GetNextLevelExp(Level);
            UpdateAttributes();

            // 每达到levelStepForEvolution的倍数，要求进阶（例如20级、40级……）
            if (Level % levelStepForEvolution == 0)
            {
                EvolutionPending = true;
                UnityEngine.Debug.Log($"{cardName} 达到 {Level} 级，需要进阶才能继续获得经验！");
            }
        }

        // Update base attributes according to level
        private void UpdateAttributes()
        {
            BaseAttrEntity attrEntity = CardDataManager.Instance.GetBaseAttrEntitiy(Level);
            health = attrEntity.health;
            attack = attrEntity.attack;
            defense = attrEntity.defense;
            accuracy = attrEntity.accuracy;
            dodge = attrEntity.dodge;
            critical = attrEntity.critical;
            criticalDamage = attrEntity.criticalDamage;
            damageReduction = attrEntity.damageReduction;
            energyGenerateRate = attrEntity.energyGenerateRate;
            speed = attrEntity.speed;
            CalculatePower();
        }

        /// <summary>
        /// Recomputes the score shown by collection and formation UI, then notifies listeners.
        /// Current implementation also persists the complete collection through CardListManager.
        /// </summary>
        private void CalculatePower()
        {
            power = (GetPanelHealth() * 1f) + (GetPanelAttack() * 5f) + (GetPanelDefense() * 5f)
            + (GetPanelAccuracy() * 1f) + (GetPanelDodge() * 1f) + (GetPanelCritical() * 1f)
            + (GetPanelCritialDamage() * 1f) + (GetPanelDMGReduction() * 1f) + (GetPanelEnergyRate() * 1f)
            + (GetPanelSpeed() * 1f);
            OnDataChanged?.Invoke();
            DataUtil.Instance.SaveCardData(CardListManager.Instance.cardEntities);
        }

        public void UpgradeCard()
        {
            ExpertiseEntity expertise = CardDataManager.Instance.GetExpertise(this);
            AddExpertise(expertise);
            EvolutionPending = false;
            OnCardUpgraded?.Invoke();
            UnityEngine.Debug.Log($"{cardName} 已进阶！expertise：{expertise.attributeType} {expertise.expertiseTier} {expertise.value}");
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
            this.Level = level;
            return this;
        }

        public CardEntity SetExp(float exp)
        {
            this.CurrentExp = exp;
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

        public CardEntity SetCardType(Archetype archetype)
        {
            this.archetype = archetype;
            return this;
        }

        public CardEntity SetAccuracy(float accuracy)
        {
            this.accuracy = accuracy;
            return this;
        }

        public CardEntity SetDodge(float dodge)
        {
            this.dodge = dodge;
            return this;
        }

        public CardEntity SetCritical(float critical)
        {
            this.critical = critical;
            return this;
        }

        public CardEntity SetCriticalDamage(float criticalDamage)
        {
            this.criticalDamage = criticalDamage;
            return this;
        }

        public CardEntity SetDagameReduction(float dagameReduction)
        {
            this.damageReduction = dagameReduction;
            return this;
        }

        public CardEntity SetEnergyGenerateRate(int energyGenerateRate)
        {
            this.energyGenerateRate = energyGenerateRate;
            return this;
        }

        public CardEntity SetSpeed(float speed)
        {
            this.speed = speed;
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

        public CardEntity SetLineupPosition(int position)
        {
            this.position = (LineupPosition)position;
            return this;
        }

        public LineupPosition GetLineupPosition()
        {
            return position;
        }

        public float GetPanelHealth()
        {
            return health * characterAttrs[Status.AttributeType.Health] * panelAttrs[Status.AttributeType.Health];
        }

        public float GetPanelAttack()
        {
            return attack * characterAttrs[Status.AttributeType.Attack] * panelAttrs[Status.AttributeType.Attack];
        }

        public float GetPanelDefense()
        {
            return defense * characterAttrs[Status.AttributeType.Defense] * panelAttrs[Status.AttributeType.Defense];
        }

        public float GetPanelAccuracy()
        {
            return accuracy * characterAttrs[Status.AttributeType.Accuracy] * panelAttrs[Status.AttributeType.Accuracy];
        }

        public float GetPanelDodge()
        {
            return dodge * characterAttrs[Status.AttributeType.Dodge] * panelAttrs[Status.AttributeType.Dodge];
        }

        public float GetPanelCritical()
        {
            return critical * characterAttrs[Status.AttributeType.Critical] * panelAttrs[Status.AttributeType.Critical];
        }

        public float GetPanelCritialDamage()
        {
            return criticalDamage * characterAttrs[Status.AttributeType.CriticalDamage] * panelAttrs[Status.AttributeType.CriticalDamage];
        }

        public float GetPanelDMGReduction()
        {
            return damageReduction * characterAttrs[Status.AttributeType.DamageReduction] * panelAttrs[Status.AttributeType.DamageReduction];
        }

        public float GetPanelEnergyRate()
        {
            return energyGenerateRate * characterAttrs[Status.AttributeType.EnergyGenerateRate] * panelAttrs[Status.AttributeType.EnergyGenerateRate];
        }

        public float GetPanelSpeed()
        {
            return speed * characterAttrs[Status.AttributeType.Speed] * panelAttrs[Status.AttributeType.Speed];
        }

        public float GetBattleHealth()
        {
            return GetPanelHealth() * battleAttrs[Status.AttributeType.Health];
        }

        public float GetBattleAttack()
        {
            return GetPanelAttack() * battleAttrs[Status.AttributeType.Attack];
        }

        public float GetBattleDefense()
        {
            return GetPanelDefense() * battleAttrs[Status.AttributeType.Defense];
        }

        public float GetBattleAccuracy()
        {
            return GetPanelAccuracy() * battleAttrs[Status.AttributeType.Accuracy];
        }

        public float GetBattleDodge()
        {
            return GetPanelDodge() * battleAttrs[Status.AttributeType.Dodge];
        }

        public float GetBattleCritical()
        {
            return GetPanelCritical() * battleAttrs[Status.AttributeType.Critical];
        }

        public float GetBattleCriticalDamage()
        {
            return GetPanelCritialDamage() * battleAttrs[Status.AttributeType.CriticalDamage];
        }

        public float GetBattleDMGReduction()
        {
            return GetPanelDMGReduction() * battleAttrs[Status.AttributeType.DamageReduction];
        }

        public float GetBattleEnergyRate()
        {
            return GetPanelEnergyRate() * battleAttrs[Status.AttributeType.EnergyGenerateRate];
        }

        public float GetBattleSpeed()
        {
            return GetPanelSpeed() * battleAttrs[Status.AttributeType.Speed];
        }
    }

    public enum LineupPosition
    {
        ZERO,
        ONE,
        Two,
        Three,
        Four,
        None
    }
}
