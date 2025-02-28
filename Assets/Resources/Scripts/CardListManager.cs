using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// List of card in Card Panel
public class CardListManager : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform gridParent;
    // private int selectedIndex = 0;
    public List<Card> cards;
    public List<CardEntity> cardEntities = new List<CardEntity>();
    public static CardListManager Instance;

    private bool isNameAscending = false;
    private bool isTierAscending = false;
    private bool isPowerAscending = false;
    private bool isLevelAscending = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        // FakeCardList();
    }

    void OnEnable()
    {
        Debug.Log("Init Card List");
        InitCardList();
    }

    private void OnCardClicked(Card card)
    {
        Debug.Log("Card clicked: " + card.cardEntity.cardName);
        CardPreviewController.instance.ShowCardPreview(card.cardEntity);
        UnhighlightAllCards();
        card.Highlight(DefaultProperty.highlightCardScale);
    }

    public void InitCardList()
    {
        cardEntities = DataUtil.LoadCardData();
        if (cards.Count < cardEntities.Count)
        {
            for (int i = cards.Count; i < cardEntities.Count; i++)
            {
                GameObject cardGO = Instantiate(cardPrefab, gridParent);
                cardGO.SetActive(true);
                Card card = cardGO.GetComponent<Card>();
                card.SetCardScale(DefaultProperty.defaultCardScale);
                cards.Add(card);
                card.OnCardClicked += OnCardClicked;
            }
        }
        else
        {
            for (int i = cards.Count - 1; i >= cardEntities.Count; i--)
            {
                Destroy(cards[i].gameObject);
                cards.RemoveAt(i);
            }
        }
        Debug.Log("cardEntities size: " + cardEntities.Count);
        Debug.Log("cards size: " + cards.Count);
        SortCardsByName();
    }

    public void UpdateCardList()
    {
        if (cardEntities.Count == 0) return;
        for (int i = 0; i < cardEntities.Count; i++)
        {
            cards[i].InitCard(cardEntities[i]);
        }
        Debug.Log("instance is null: " + (CardPreviewController.instance == null));
        Debug.Log("Cards size: " + cards.Count);
        cards[0].Highlight(DefaultProperty.highlightCardScale);
        CardPreviewController.instance.ShowCardPreview(cards[0].cardEntity);
    }

    public void UnhighlightAllCards()
    {
        foreach (Card card in cards)
        {
            card.Unhighlight(DefaultProperty.defaultCardScale);
        }
    }

    public void SortCardsByName()
    {
        isNameAscending = !isNameAscending;
        List<CardEntity> sortedEntities = new List<CardEntity>();
        Debug.Log("Sort cards by name isAscending: " + isNameAscending);
        if (isNameAscending)
        {
            sortedEntities = cardEntities.OrderBy(card => card.characterName).ThenByDescending(card => card.characterTier).ToList();
        }
        else
        {
            sortedEntities = cardEntities.OrderByDescending(card => card.characterName).ThenByDescending(card => card.characterTier).ToList();

        }
        cardEntities = sortedEntities;
        UpdateCardList();
    }

    public void SortCardsByTier()
    {
        isTierAscending = !isTierAscending;
        Debug.Log("Sort cards by tier isAscending: " + isTierAscending);
        if (isTierAscending)
        {
            cardEntities.Sort((a, b) => a.characterTier.CompareTo(b.characterTier));
        }
        else
        {
            cardEntities.Sort((a, b) => b.characterTier.CompareTo(a.characterTier));
        }
        UpdateCardList();
    }

    public void AddCardEntity(CardEntity cardEntity)
    {
        Debug.Log("Add card entity: " + cardEntities.Count);
        cardEntities.Add(cardEntity);
        DataUtil.SaveCardData(cardEntities);
    }

    public void AddCardEntity(List<CardEntity> cardEntities)
    {
        Debug.Log("Add card entities: " + cardEntities.Count);
        this.cardEntities.AddRange(cardEntities);
        DataUtil.SaveCardData(this.cardEntities);
    }

    public void RemoveCardEntity(CardEntity cardEntity)
    {
        cardEntities.Remove(cardEntity);
        DataUtil.SaveCardData(cardEntities);
    }

    public void ClearCardEntities()
    {
        Debug.Log("Clear card entities");
        cardEntities.Clear();
        DataUtil.SaveCardData(cardEntities);
    }

    public List<CardEntity> GetCardEntities()
    {
        return cardEntities;
    }

    public void FakeCardList()
    {
        List<CardEntity> fakeEntities = new List<CardEntity>();
        for (int i = 0; i < 30; i++)
        {
            Character character = CardDataManager.Instance.GetCharacter();
            fakeEntities.Add(CardDataManager.Instance.GetCardEntity(character));
        }
    }
}