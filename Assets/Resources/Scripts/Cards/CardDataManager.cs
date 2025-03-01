using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Characters;
using Assets.Resources.Scripts.Characters.Mechanician;
using Assets.Resources.Scripts.Characters.Monster;
using Assets.Resources.Scripts.Characters.Magician;

namespace Assets.Resources.Scripts.Cards
{
    public class CardDataManager : MonoBehaviour
    {
        public static CardDataManager Instance { get; private set; }
        private string filePath;          // Where we store the JSON file
        private CardDataContainer dataContainer;   // Holds our list of cards
        private List<Character> characterList;    // A list that holds all the designed characters for the game

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
        }

        public CardEntity GetCardEntity(Character character)
        {
            CharacterTier tier = GetCardTier(character);
            CardEntity cardEntity = new CardEntity().SetCharacterName(character.characterName).SetCharacterTier(tier).SetArchetype(character.archetype)
            .SetSpeed(Random.Range(15, 25)).SetAttack(Random.Range(10, 20)).SetDefense(Random.Range(10, 20))
            .SetLevel(Random.Range(1, 10)).SetExp(Random.Range(0, 10000));
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