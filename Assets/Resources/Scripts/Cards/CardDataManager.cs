using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Characters;
using Assets.Resources.Scripts.Characters.Mechanician;
using Assets.Resources.Scripts.Characters.Monster;
using Assets.Resources.Scripts.Characters.Magician;
using static Assets.Resources.Scripts.Props.Status;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.CharacterPanel;

namespace Assets.Resources.Scripts.Cards
{
    using UnityEngine;

    /// <summary>
    /// Builds randomized CardEntity instances from character strategies and static Resources data.
    /// It owns the runtime catalogs for characters, skills, level attributes, and expertise weights.
    /// </summary>
    /// <remarks>
    /// Awake must finish before CardEntity progression or draw-card flows request generated data.
    /// This manager creates domain models; CardListManager owns the player's persisted collection.
    /// </remarks>
    public class CardDataManager : MonoBehaviour
    {
        /// <summary>Singleton instance.</summary>
        public static CardDataManager Instance { get; private set; }
        [SerializeField] private string filePath;
        private CardListWrapper dataContainer = new CardListWrapper();
        private List<Character> characterList;
        [SerializeField] private List<SkillEntity> skillEntities;
        [SerializeField] private List<BaseAttrEntity> baseAttrEntities;
        public static Dictionary<Archetype, Dictionary<AttributeType, float>> baseAttributeProbabilities;
        public static Dictionary<CharacterTier, float> baseTierProbabilities;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            filePath = Path.Combine(Application.persistentDataPath, "Cards.json");
            InitCharacterList();
            InitSkillList();
            InitBaseAttrList();
            InitBaseAttributeProbabilities();
            InitExpertiseTierProbabilities();
        }

        /// <summary>
        /// Creates a randomized CardEntity whose identity comes from the supplied character strategy.
        /// The returned entity is not added to the player's collection automatically.
        /// </summary>
        public CardEntity GetCardEntity(Character character)
        {
            var tier = GetCardTier(character);
            return new CardEntity()
                .SetCardName(character.characterName.ToString())
                .SetCharacterName(character.characterName)
                .SetCharacterTier(tier)
                .SetArchetype(character.archetype)
                .SetSpeed(UnityEngine.Random.Range(15, 25))
                .SetAttack(UnityEngine.Random.Range(10, 20))
                .SetDefense(UnityEngine.Random.Range(10, 20))
                .SetLevel(UnityEngine.Random.Range(1, 10))
                .SetExp(0f);
        }

        /// <summary>Rolls for character tier based on assigned probabilities.</summary>
        public CharacterTier GetCardTier(Character character)
        {
            int roll = UnityEngine.Random.Range(1, 10001);
            int acc = 0;
            foreach (var entry in character.possibleTiers.OrderBy(e => e.Value))
            {
                acc += entry.Value;
                if (roll <= acc)
                    return entry.Key;
            }
            return CharacterTier.TierE;
        }

        /// <summary>Selects a character strategy using each character's spawn weight.</summary>
        public Character GetCharacter()
        {
            int rollRange = characterList.Sum(c => c.weight);
            int roll = UnityEngine.Random.Range(0, rollRange);
            int cumulative = 0;
            foreach (var character in characterList)
            {
                cumulative += character.weight;
                if (roll < cumulative)
                    return character;
            }
            return characterList.First();
        }

        /// <summary>Get a random ExpertiseEntity for given CardEntity.</summary>
        public ExpertiseEntity GetExpertise(CardEntity cardEntity)
        {
            var attrProb = baseAttributeProbabilities[cardEntity.archetype];
            var selAttr = WeightedRandom(attrProb);
            var adjTierProb = GetAdjustedTierProbabilities(cardEntity.Level);
            var selTier = WeightedRandom(adjTierProb);
            return new ExpertiseEntity(selAttr, GetExpertiseValue(selTier), selTier);
        }

        /// <summary>Adjust base tier probabilities by level.</summary>
        public Dictionary<CharacterTier, float> GetAdjustedTierProbabilities(int characterLevel)
        {
            float levelFactor = characterLevel / (float)DefaultProperty.MAX_LEVEL;
            var dict = new Dictionary<CharacterTier, float>();
            foreach (var kv in baseTierProbabilities)
            {
                float add = 0, sub = 0;
                switch (kv.Key)
                {
                    case CharacterTier.TierSS: add = .01f; break;
                    case CharacterTier.TierS: add = .04f; break;
                    case CharacterTier.TierA: add = .15f; break;
                    case CharacterTier.TierC: sub = .01f; break;
                    case CharacterTier.TierD: sub = .04f; break;
                    case CharacterTier.TierE: sub = .15f; break;
                }
                dict[kv.Key] = kv.Value + add * levelFactor - sub * levelFactor;
            }
            return dict;
        }

        /// <summary>Randomizes an expertise value in range based on tier.</summary>
        public float GetExpertiseValue(CharacterTier tier)
        {
            switch (tier)
            {
                case CharacterTier.TierSS: return UnityEngine.Random.Range(.8f, 1.1f);
                case CharacterTier.TierS: return UnityEngine.Random.Range(.5f, .7f);
                case CharacterTier.TierA: return UnityEngine.Random.Range(.3f, .5f);
                case CharacterTier.TierB: return UnityEngine.Random.Range(.16f, .25f);
                case CharacterTier.TierC: return UnityEngine.Random.Range(.08f, .16f);
                case CharacterTier.TierD: return UnityEngine.Random.Range(.06f, .08f);
                case CharacterTier.TierE: return UnityEngine.Random.Range(.04f, .06f);
                default: return 0f;
            }
        }

        /// <summary>Returns all <see cref="SkillEntity"/> for a given character.</summary>
        public List<SkillEntity> GetSkillsByCharacter(CharacterName name) => skillEntities?.Where(s => s.characterName == name).ToList();

        /// <summary>Finds the base attributes for a level, or null.</summary>
        public BaseAttrEntity GetBaseAttrEntitiy(int level) => baseAttrEntities?.FirstOrDefault(e => e.level == level);

        /// <summary>Returns all expertise definitions for a character.</summary>
        public List<ExpertiseEntity> GetCharacterExpertises(CharacterName name)
        {
            var ret = new List<ExpertiseEntity>();
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
            }
            return ret;
        }

        /// <summary>Returns all stored cards.</summary>
        public List<CardEntity> GetAllCards() => dataContainer?.cardEntities;

        /// <summary>Sets all card entities.</summary>
        public void SetAllCards(List<CardEntity> cards) => dataContainer.cardEntities = cards;
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
            var decryptedData = EncryptionUtil.LoadAndDecryptFile(DefaultProperty.SKILL_DATA_PATH);
            if (decryptedData == null) { skillEntities = new List<SkillEntity>(); return; }
            var jsonContent = Encoding.UTF8.GetString(decryptedData);
            var wrapper = JsonUtility.FromJson<SkillListWrapper>(jsonContent);
            skillEntities = wrapper?.skillEntities ?? new List<SkillEntity>();
        }
        private void InitBaseAttrList()
        {
            var jsonContent = Resources.Load<TextAsset>(DefaultProperty.BASE_ATTR_PATH);
            baseAttrEntities = jsonContent == null ? new List<BaseAttrEntity>() :
                (JsonUtility.FromJson<BaseAttrEntityWrapper>(jsonContent.text)?.baseAttrEntities ?? new List<BaseAttrEntity>());
        }
        private void InitBaseAttributeProbabilities()
        {
            baseAttributeProbabilities = new Dictionary<Archetype, Dictionary<AttributeType, float>>
            {
                { Archetype.Assassin, new(){ {AttributeType.Speed,.25f}, {AttributeType.Critical,.15f},{AttributeType.CriticalDamage,.1f},{AttributeType.Attack,.1f},{AttributeType.Accuracy,.1f},{AttributeType.Dodge,.1f},{AttributeType.EnergyGenerateRate,.05f},{AttributeType.Health,.05f},{AttributeType.Defense,.05f},{AttributeType.DamageReduction,.05f}, } },
                { Archetype.Magician, new(){{AttributeType.Attack,.3f},{AttributeType.Critical,.15f},{AttributeType.CriticalDamage,.15f},{AttributeType.Health,.1f},{AttributeType.Accuracy,.05f},{AttributeType.Dodge,.05f},{AttributeType.Speed,.05f},{AttributeType.EnergyGenerateRate,.05f},{AttributeType.Defense,.05f},{AttributeType.DamageReduction,.05f},}},
                { Archetype.Mechanician, new(){{AttributeType.EnergyGenerateRate,.25f},{AttributeType.Accuracy,.15f},{AttributeType.Health,.1f},{AttributeType.Defense,.1f},{AttributeType.DamageReduction,.1f},{AttributeType.Attack,.1f},{AttributeType.Critical,.05f},{AttributeType.CriticalDamage,.05f},{AttributeType.Dodge,.05f},{AttributeType.Speed,.05f},}},
                { Archetype.Monster, new(){{AttributeType.Health,.35f},{AttributeType.Defense,.15f},{AttributeType.DamageReduction,.1f},{AttributeType.Attack,.1f},{AttributeType.Critical,.05f},{AttributeType.CriticalDamage,.05f},{AttributeType.Accuracy,.05f},{AttributeType.Dodge,.05f},{AttributeType.Speed,.05f},{AttributeType.EnergyGenerateRate,.05f},}},
                { Archetype.Potioneer, new(){{AttributeType.EnergyGenerateRate,.25f},{AttributeType.Dodge,.15f},{AttributeType.Health,.1f},{AttributeType.Accuracy,.1f},{AttributeType.Speed,.1f},{AttributeType.DamageReduction,.1f},{AttributeType.Critical,.05f},{AttributeType.CriticalDamage,.05f},{AttributeType.Attack,.05f},{AttributeType.Defense,.05f},}},
                { Archetype.Warrior, new(){{AttributeType.DamageReduction,.3f},{AttributeType.Defense,.2f},{AttributeType.Health,.2f},{AttributeType.Dodge,.1f},{AttributeType.Attack,.04f},{AttributeType.EnergyGenerateRate,.04f},{AttributeType.Critical,.03f},{AttributeType.CriticalDamage,.03f},{AttributeType.Accuracy,.03f},{AttributeType.Speed,.03f},}},
            };
        }
        private void InitExpertiseTierProbabilities()
        {
            baseTierProbabilities = new()
            {
                { CharacterTier.TierE, .5f },
                { CharacterTier.TierD, .2f },
                { CharacterTier.TierC, .14f },
                { CharacterTier.TierB, .1f },
                { CharacterTier.TierA, .05f },
                { CharacterTier.TierS, .008f },
                { CharacterTier.TierSS, .002f },
            };
        }

        /// <summary>
        /// Selects one key from arbitrary non-normalized weights. Negative weights are not supported.
        /// </summary>
        private static T WeightedRandom<T>(Dictionary<T, float> probabilities)
        {
            if (probabilities == null || probabilities.Count == 0) return default;
            float total = probabilities.Values.Sum();
            float r = UnityEngine.Random.value * total;
            foreach (var kv in probabilities)
            {
                if (r < kv.Value) return kv.Key;
                r -= kv.Value;
            }
            return default;
        }
    }

    /// <summary>Character visual tier (rarity).</summary>
    public enum CharacterTier
    {
        None,
        TierE, TierD, TierC, TierB, TierA, TierS, TierSS
    }

    /// <summary>Class/role of a character.</summary>
    public enum Archetype
    {
        Default, Assassin, Magician, Mechanician, Monster, Potioneer, Warrior
    }
}
