using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CardDataManager : MonoBehaviour
{
    public static CardDataManager Instance { get; private set; }
    private string filePath;          // Where we store the JSON file
    private CardDataContainer dataContainer;   // Holds our list of cards

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        // On most platforms, Application.persistentDataPath is a good location for data
        filePath = Path.Combine(Application.persistentDataPath, "cardData.json");
        dataContainer = new CardDataContainer();
    }

    // Add a new card to our container in memory
    public void AddCard()
    {
        CardEntity newCard = new CardEntity("Yee", 15f, 20f, 15f, 100f, 100f, 30f);
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

    // Accessor method to get card list
    public List<CardEntity> GetAllCards()
    {
        return dataContainer.cards;
    }
}
