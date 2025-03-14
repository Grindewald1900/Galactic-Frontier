using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Characters;
using Assets.Resources.Scripts.Characters.Mechanician;
using Assets.Resources.Scripts.Characters.Monster;
using Assets.Resources.Scripts.Characters.Magician;
using static Assets.Resources.Scripts.Props.Status;
using Assets.Resources.Scripts.Props;
using System.Text;
using Assets.Resources.Scripts.CharacterPanel;

namespace Assets.Resources.Scripts.Cards
{
    public class CardDataManager : MonoBehaviour
    {
        public static CardDataManager Instance { get; private set; }
        private string filePath;          // Where we store the JSON file
        private CardDataContainer dataContainer;   // Holds our list of cards
        private List<Character> characterList;    // A list that holds all the designed characters for the game
        public List<SkillEntity> skillEntities; // A list that holds all the skills for the game
        public List<BaseAttrEntity> baseAttrEntities; // A list that holds all the base attributes for each level
        public static Dictionary<Archetype, Dictionary<AttributeType, float>> baseAttributeProbabilities;
        public static Dictionary<CharacterTier, float> baseTierProbabilities;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            // On most platforms, Application.persistentDataPath is a good location for data
            filePath = Path.Combine(Application.persistentDataPath, "Cards.json");
            dataContainer = new CardDataContainer();
            InitCharacterList();
            InitSkillList();
            InitBaseAttrList();
            InitBaseAttributeProbabilities();
            InitExpertiseTierProbabilities();
        }

        public CardEntity GetCardEntity(Character character)
        {
            CharacterTier tier = GetCardTier(character);
            CardEntity cardEntity = new CardEntity().SetCharacterName(character.characterName).SetCharacterTier(tier).SetArchetype(character.archetype)
            .SetSpeed(Random.Range(15, 25)).SetAttack(Random.Range(10, 20)).SetDefense(Random.Range(10, 20))
            .SetLevel(Random.Range(1, 10)).SetExp(0f);
            return cardEntity;
        }

        public CharacterTier GetCardTier(Character character)
        {
            int roll = Random.Range(1, 10001);
            foreach (KeyValuePair<CharacterTier, int> entry in character.possibleTiers)
            {
                if (roll <= entry.Value)
                {
                    return entry.Key;
                }
            }
            return CharacterTier.TierE; // Default to Grey if no tier is found
        }

        public Character GetCharacter()
        {
            int rollRange = characterList.Sum(character => character.weight);
            int roll = Random.Range(0, rollRange);
            int cumulativeWeight = 0;
            foreach (Character character in characterList)
            {
                cumulativeWeight += character.weight;
                if (roll < cumulativeWeight)
                {
                    return character; // 选中该角色
                }
            }
            return characterList[0]; // 兜底返回（理论上不会执行）
        }

        public ExpertiseEntity GetExpertise(CardEntity cardEntity)
        {
            ExpertiseEntity entity = new();
            // Get expertise according to the character's archetype and level
            var attributeProbabilities = baseAttributeProbabilities[cardEntity.archetype];
            AttributeType selectedAttribute = WeightedRandom(attributeProbabilities);
            var adjustedTierProbabilities = GetAdjustedTierProbabilities(cardEntity.Level);
            CharacterTier selectedTier = WeightedRandom(adjustedTierProbabilities);
            entity.attributeType = selectedAttribute;
            entity.expertiseTier = selectedTier;
            entity.value = GetExpertiseValue(selectedTier);
            return entity;
        }

        public Dictionary<CharacterTier, float> GetAdjustedTierProbabilities(int characterLevel)
        {
            float levelFactor = characterLevel / DefaultProperty.MAX_LEVEL;
            return new Dictionary<CharacterTier, float>
            {
                { CharacterTier.TierSS, baseTierProbabilities[CharacterTier.TierSS] + (levelFactor * 0.01f) },
                { CharacterTier.TierS, baseTierProbabilities[CharacterTier.TierS] + (levelFactor * 0.04f) },
                { CharacterTier.TierA, baseTierProbabilities[CharacterTier.TierA] + levelFactor * 0.15f },
                { CharacterTier.TierB, baseTierProbabilities[CharacterTier.TierB]},
                { CharacterTier.TierC, baseTierProbabilities[CharacterTier.TierC] - levelFactor * 0.01f },
                { CharacterTier.TierD, baseTierProbabilities[CharacterTier.TierD] - levelFactor * 0.04f },
                { CharacterTier.TierE, baseTierProbabilities[CharacterTier.TierE] - levelFactor * 0.15f }
            };
        }

        public float GetExpertiseValue(CharacterTier tier)
        {
            switch (tier)
            {
                case CharacterTier.TierSS:
                    return Random.Range(0.8f, 1.1f);
                case CharacterTier.TierS:
                    return Random.Range(0.5f, 0.7f);
                case CharacterTier.TierA:
                    return Random.Range(0.3f, 0.5f);
                case CharacterTier.TierB:
                    return Random.Range(0.16f, 0.25f);
                case CharacterTier.TierC:
                    return Random.Range(0.08f, 0.16f);
                case CharacterTier.TierD:
                    return Random.Range(0.06f, 0.08f);
                case CharacterTier.TierE:
                    return Random.Range(0.4f, 0.06f);
                case CharacterTier.None:
                    break;
                default:
                    return 0f;
            }
            return 0f;
        }

        public List<SkillEntity> GetSkillsByCharacter(CharacterName name)
        {
            return skillEntities.Where(skill => skill.characterName == name).ToList();
        }

        public BaseAttrEntity GetBaseAttrEntitiy(int level)
        {
            return baseAttrEntities.FirstOrDefault(entity => entity.level == level);
        }

        public List<ExpertiseEntity> GetCharacterExpertises(CharacterName name)
        {
            List<ExpertiseEntity> ret = new();
            switch (name)
            {
                case CharacterName.Asra:
                    ret.Add(new ExpertiseEntity(AttributeType.EnergyGenerateRate, GetExpertiseValue(CharacterTier.TierA), CharacterTier.TierA));
                    ret.Add(new ExpertiseEntity(AttributeType.Accuracy, GetExpertiseValue(CharacterTier.TierC), CharacterTier.TierC));
                    ret.Add(new ExpertiseEntity(AttributeType.Health, GetExpertiseValue(CharacterTier.TierE), CharacterTier.TierE));
                    break;
                case CharacterName.Magki:
                    ret.Add(new ExpertiseEntity(AttributeType.Health, GetExpertiseValue(CharacterTier.TierB), CharacterTier.TierB));
                    ret.Add(new ExpertiseEntity(AttributeType.Defense, GetExpertiseValue(CharacterTier.TierC), CharacterTier.TierC));
                    ret.Add(new ExpertiseEntity(AttributeType.DamageReduction, GetExpertiseValue(CharacterTier.TierE), CharacterTier.TierE));
                    break;
                case CharacterName.Sernia:
                    ret.Add(new ExpertiseEntity(AttributeType.Attack, GetExpertiseValue(CharacterTier.TierB), CharacterTier.TierB));
                    ret.Add(new ExpertiseEntity(AttributeType.Critical, GetExpertiseValue(CharacterTier.TierC), CharacterTier.TierC));
                    ret.Add(new ExpertiseEntity(AttributeType.CriticalDamage, GetExpertiseValue(CharacterTier.TierE), CharacterTier.TierE));
                    break;
                default:
                    break;
            }
            return ret;
        }

        // Accessor method to get card list
        public List<CardEntity> GetAllCards()
        {
            return dataContainer.cards;
        }

        public void SetAllCards(List<CardEntity> cards)
        {
            dataContainer.cards = cards;
        }

        private void InitCharacterList()
        {
            characterList = new List<Character>
            {
                new Asra(),
                new Magki(),
                new Sernia()
            };
        }

        private void InitSkillList()
        {
            Debug.Log("Decrypting skill data...");
            byte[] decryptedData = EncryptionUtil.LoadAndDecryptFile(DefaultProperty.SKILL_DATA_PATH);
            if (decryptedData != null)
            {
                string jsonContent = Encoding.UTF8.GetString(decryptedData);
                Debug.Log("Decrypted skill data: " + jsonContent);
                // 处理解密后的JSON内容，例如反序列化为对象
                SkillListWrapper wrapper = JsonUtility.FromJson<SkillListWrapper>(jsonContent);
                Debug.Log("Skill count: " + wrapper.skillEntities.Count + "个技能");
                skillEntities = wrapper.skillEntities;
            }
        }

        private void InitBaseAttrList()
        {
            Debug.Log("InitBaseAttrList...");
            TextAsset jsonContent = UnityEngine.Resources.Load<TextAsset>(DefaultProperty.BASE_ATTR_PATH);
            if (jsonContent != null)
            {
                BaseAttrEntityWrapper wrapper = JsonUtility.FromJson<BaseAttrEntityWrapper>(jsonContent.text);
                baseAttrEntities = wrapper.baseAttrEntities;
                Debug.Log("InitBaseAttrList count: " + baseAttrEntities.Count);
            }
        }

        private void InitBaseAttributeProbabilities()
        {
            baseAttributeProbabilities = new Dictionary<Archetype, Dictionary<AttributeType, float>>
            {
                {
                    Archetype.Assassin, new Dictionary<AttributeType, float>
                    {
                            { AttributeType.Speed, 0.25f },
                            { AttributeType.Critical, 0.15f },
                            { AttributeType.CriticalDamage, 0.1f },
                            { AttributeType.Attack, 0.1f },
                            { AttributeType.Accuracy, 0.1f },
                            { AttributeType.Dodge, 0.1f },
                            { AttributeType.EnergyGenerateRate, 0.05f },
                            { AttributeType.Health, 0.05f },
                            { AttributeType.Defense, 0.05f },
                            { AttributeType.DamageReduction, 0.05f },
                    }
                },
                {
                    Archetype.Magician, new Dictionary<AttributeType, float>
                    {
                            { AttributeType.Attack, 0.3f },
                            { AttributeType.Critical, 0.15f },
                            { AttributeType.CriticalDamage, 0.15f },
                            { AttributeType.Health, 0.1f },
                            { AttributeType.Accuracy, 0.05f },
                            { AttributeType.Dodge, 0.05f },
                            { AttributeType.Speed, 0.05f },
                            { AttributeType.EnergyGenerateRate, 0.05f },
                            { AttributeType.Defense, 0.05f },
                            { AttributeType.DamageReduction, 0.05f },
                    }
                },
                {
                    Archetype.Mechanician, new Dictionary<AttributeType, float>
                    {
                            { AttributeType.EnergyGenerateRate, 0.25f },
                            { AttributeType.Accuracy, 0.15f },
                            { AttributeType.Health, 0.1f },
                            { AttributeType.Defense, 0.1f },
                            { AttributeType.DamageReduction, 0.1f },
                            { AttributeType.Attack, 0.1f },
                            { AttributeType.Critical, 0.05f },
                            { AttributeType.CriticalDamage, 0.05f},
                            { AttributeType.Dodge, 0.05f },
                            { AttributeType.Speed, 0.05f },
                    }
                },
                {
                    Archetype.Monster, new Dictionary<AttributeType, float>
                    {
                            { AttributeType.Health, 0.35f },
                            { AttributeType.Defense, 0.15f },
                            { AttributeType.DamageReduction, 0.1f },
                            { AttributeType.Attack, 0.1f },
                            { AttributeType.Critical, 0.05f },
                            { AttributeType.CriticalDamage, 0.05f },
                            { AttributeType.Accuracy, 0.05f },
                            { AttributeType.Dodge, 0.05f },
                            { AttributeType.Speed, 0.05f },
                            { AttributeType.EnergyGenerateRate, 0.05f },
                    }
                },
                {
                    Archetype.Potioneer, new Dictionary<AttributeType, float>
                    {
                            { AttributeType.EnergyGenerateRate, 0.25f },
                            { AttributeType.Dodge, 0.15f },
                            { AttributeType.Health, 0.1f },
                            { AttributeType.Accuracy, 0.1f },
                            { AttributeType.Speed, 0.1f },
                            { AttributeType.DamageReduction, 0.1f },
                            { AttributeType.Critical, 0.05f },
                            { AttributeType.CriticalDamage, 0.05f },
                            { AttributeType.Attack, 0.05f },
                            { AttributeType.Defense, 0.05f },
                    }
                },
                {
                    Archetype.Warrior, new Dictionary<AttributeType, float>
                    {
                            { AttributeType.DamageReduction, 0.3f },
                            { AttributeType.Defense, 0.2f },
                            { AttributeType.Health, 0.2f },
                            { AttributeType.Dodge, 0.1f },
                            { AttributeType.Attack, 0.04f },
                            { AttributeType.EnergyGenerateRate, 0.04f },
                            { AttributeType.Critical, 0.03f },
                            { AttributeType.CriticalDamage, 0.03f },
                            { AttributeType.Accuracy, 0.03f },
                            { AttributeType.Speed, 0.03f },
                    }
                }
            };
        }

        private void InitExpertiseTierProbabilities()
        {
            baseTierProbabilities = new Dictionary<CharacterTier, float>
            {
                { CharacterTier.TierE, 0.5f },
                { CharacterTier.TierD, 0.2f },
                { CharacterTier.TierC, 0.14f },
                { CharacterTier.TierB, 0.1f },
                { CharacterTier.TierA, 0.05f },
                { CharacterTier.TierS, 0.008f },
                { CharacterTier.TierSS, 0.002f },
            };
        }

        private T WeightedRandom<T>(Dictionary<T, float> probabilities)
        {
            float total = probabilities.Values.Sum();
            float randomPoint = Random.value * total;

            foreach (var kvp in probabilities)
            {
                if (randomPoint < kvp.Value)
                    return kvp.Key;
                else
                    randomPoint -= kvp.Value;
            }
            return default;
        }
    }

    public enum CharacterTier
    {
        None,
        TierE, // Grey
        TierD, // Green
        TierC, // Blue
        TierB, // Purple
        TierA, // Yellow
        TierS, // Rainbow
        TierSS, // Rainbow
    }

    public enum Archetype
    {
        Assassin,
        Magician,
        Mechanician,
        Monster,
        Potioneer,
        Warrior,
    }
}