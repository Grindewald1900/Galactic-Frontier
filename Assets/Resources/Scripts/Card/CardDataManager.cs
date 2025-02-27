using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

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

    void Start()
    {
        LoadCards();
    }

    // Add a new card to our container in memory
    public void AddCard()
    {
        CardEntity newCard = new CardEntity();
        dataContainer.cards.Add(newCard);
    }

    // Save current card data to disk in JSON format
    public void SaveCards()
    {
        // Convert data (List of cards) to JSON
        string json = JsonUtility.ToJson(dataContainer, true);
        Debug.Log($"Cards string {json}");
        File.WriteAllText(filePath, json);
        Debug.Log($"Cards saved to {filePath}");
    }

    // Load card data from disk
    public void LoadCards()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            dataContainer = JsonUtility.FromJson<CardDataContainer>(json);

            if (dataContainer == null)
                dataContainer = new CardDataContainer();

            Debug.Log($"Cards loaded from {filePath}. Total cards: {dataContainer.cards.Count}");
        }
        else
        {
            Debug.LogWarning("No save file found. Creating a new one.");
            dataContainer = new CardDataContainer();
        }
    }

    public CardEntity GetCardEntity(Character character)
    {
        CardEntity cardEntity = new CardEntity();
        CharacterTier tier = GetCardTier(character);
        cardEntity.SetCharacterName(character.characterName).SetCharacterTier(tier).SetArchetype(character.archetype)
        .SetSpeed(Random.Range(15, 25)).SetAttack(Random.Range(10, 20)).SetDefense(Random.Range(10, 20))
        .SetLevel(Random.Range(1, 10)).SetExp(Random.Range(0, 10000));
        return cardEntity;
    }

    public CharacterTier GetCardTier(Character character)
    {
        Dictionary<CharacterTier, int> tierRange = new Dictionary<CharacterTier, int>();
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

    private void InitCharacterList()
    {
        characterList = new List<Character>();
        characterList.Add(new Asra());
        characterList.Add(new Magki());
        characterList.Add(new Sernia());
    }
}

public enum CharacterTier
{
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