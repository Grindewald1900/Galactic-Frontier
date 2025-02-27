using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System;

public class CardResultManager : MonoBehaviour
{
    public static CardResultManager Instance;
    public Transform cardContent;
    public Transform tierContent;
    public Transform characterContent;
    public GameObject cardPrefab;
    // Both tier and character share the same prefab
    public GameObject reportPrefab;
    public Button confirmButton;
    public List<Card> cards = new List<Card>();
    private List<CardEntity> cardEntities = new List<CardEntity>();
    private bool isCardDrawing = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        Init();
    }

    void OnDisable()
    {
        ClearCardResult();
    }

    private void Init()
    {
        confirmButton.onClick.AddListener(() => { ConfirmCards(); });
    }

    public void InitCards(int cardCount)
    {
        cards.Clear();
        // TODO: Replace with actual data fetching logic
        FakeData(cardCount);
        foreach (var cardEntity in cardEntities)
        {
            AddCard(cardEntity);
        }
        StartCoroutine(FlipAllCards());
    }

    private void AddCard(CardEntity cardEntity)
    {
        GameObject cardGO = Instantiate(cardPrefab, cardContent);
        Card card = cardGO.GetComponent<Card>();
        card.InitCard(cardEntity);
        card.FlipCard(true, false);
        cards.Add(card);
    }

    private void ConfirmCards()
    {
        //TODO: Save card data
        MainScrollController.Instance.ShowPanel((int)MainMenuPanel.SHOP);
    }

    public void ClearCardResult()
    {
        confirmButton.interactable = false;
        cards.Clear();
        cardEntities.Clear();
        ClearLayout(cardContent);
        ClearLayout(tierContent);
        ClearLayout(characterContent);
    }

    private void ClearLayout(Transform transform)
    {
        Debug.Log("Clearing layout of " + transform.name + "Count: " + transform.childCount);
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        Debug.Log("Cleared " + transform.name + "Count: " + transform.childCount);
    }

    private IEnumerator FlipAllCards()
    {
        isCardDrawing = true;
        yield return new WaitForSeconds(1f);
        foreach (var card in cards)
        {
            card.FlipCard(false, true);
            Debug.Log("Flipped card: " + card.cardEntity.characterName.ToString());
            yield return new WaitForSeconds(0.5f);
        }
        ShowReport(cardEntities);
    }

    public void ShowReport(List<CardEntity> drawResults)
    {
        isCardDrawing = false;
        Dictionary<CharacterTier, int> tierDrawCount = GetTierDrawCount(drawResults);
        foreach (var tier in tierDrawCount)
        {
            ShowTier(tier.Key, tier.Value);
        }
        Dictionary<CharacterName, int> characterDrawCount = GetCharacterDrawCount(drawResults);
        foreach (var character in characterDrawCount)
        {
            ShowCharacter(character.Key, character.Value);
        }
        confirmButton.interactable = true;
    }

    public bool IsCardDrawing()
    {
        return isCardDrawing;
    }

    private Dictionary<CharacterName, int> GetCharacterDrawCount(List<CardEntity> drawResults)
    {
        return drawResults.GroupBy(card => card.characterName)  // 按 `Character` 分组
                          .ToDictionary(group => group.Key, group => group.Count());
    }

    private Dictionary<CharacterTier, int> GetTierDrawCount(List<CardEntity> drawResults)
    {
        return drawResults.GroupBy(card => card.characterTier)  // 按 `Tier` 分组
                          .ToDictionary(group => group.Key, group => group.Count());
    }

    private void ShowCharacter(CharacterName character, int count)
    {
        GameObject characterGO = Instantiate(reportPrefab, characterContent);
        characterGO.GetComponent<ReportSlot>().SetReport(character.ToString(), count);
    }

    private void ShowTier(CharacterTier tier, int count)
    {
        GameObject tierGO = Instantiate(reportPrefab, tierContent);
        tierGO.GetComponent<ReportSlot>().SetReport(tier.ToString(), count);
    }

    private List<CardEntity> FakeData(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Character character = CardDataManager.Instance.GetCharacter();
            CardEntity cardEntity = CardDataManager.Instance.GetCardEntity(character);
            cardEntities.Add(cardEntity);
        }
        return cardEntities;
    }
}