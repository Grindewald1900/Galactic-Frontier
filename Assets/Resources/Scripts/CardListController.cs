using System.Collections.Generic;
using UnityEngine;

public class CardListController : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform gridParent;
    // private int selectedIndex = 0;
    public List<Card> cards;
    public List<CardEntity> cardEntities = new List<CardEntity>();
    public static CardListController instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        FakeCardList();
        InitCardList();
    }

    private void OnCardClicked(Card card)
    {
        Debug.Log("Card clicked: " + card.cardEntity.cardName);
        CardPreviewController.instance.ShowCardPreview(card.cardEntity);
        UnhighlightAllCards();
        card.Highlight();
    }

    public void InitCardList()
    {
        cardEntities = DataUtil.LoadCardData();
        for (int i = 0; i < cardEntities.Count; i++)
        {
            GameObject cardGO = Instantiate(cardPrefab, gridParent);
            cardGO.SetActive(true);
            Card card = cardGO.GetComponent<Card>();
            cards.Add(card);
            card.isBattleActive = false;
            card.OnCardClicked += OnCardClicked;
        }
        UpdateCardList();
    }

    public void UpdateCardList()
    {
        for (int i = 0; i < cardEntities.Count; i++)
        {
            cards[i].InitCard(cardEntities[i]);
        }
        CardPreviewController.instance.ShowCardPreview(cards[0].cardEntity);
    }

    public void UnhighlightAllCards()
    {
        foreach (Card card in cards)
        {
            card.Unhighlight();
        }
    }

    public void SortCardsByName(bool isAscending)
    {
        Debug.Log("Sort cards by name isAscending: " + isAscending);
        if (isAscending)
        {
            cardEntities.Sort((a, b) => a.cardName.CompareTo(b.cardName));
        }
        else
        {
            cardEntities.Sort((a, b) => b.cardName.CompareTo(a.cardName));
        }
        UpdateCardList();
    }

    public void FakeCardList()
    {
        List<CardEntity> entities = new List<CardEntity>();
        List<CardEntity> fakeEntities = new List<CardEntity>();
        entities.Add(new CardEntity().SetCardName("Asra").SetCharacter(Character.Asra).SetArchetype(Archetype.Mechanician).SetCharacterTier(CharacterTier.TierF));
        entities.Add(new CardEntity().SetCardName("Sernia").SetCharacter(Character.Sernia).SetArchetype(Archetype.Magician).SetCharacterTier(CharacterTier.TierE));
        entities.Add(new CardEntity().SetCardName("Magki").SetCharacter(Character.Magki).SetArchetype(Archetype.Monster).SetCharacterTier(CharacterTier.TierD));

        for (int i = 0; i < 10; i++)
        {
            CardEntity cardEntity = entities[Random.Range(0, entities.Count)].SetSpeed(Random.Range(25f, 35f));
            fakeEntities.Add(cardEntity);
        }
        DataUtil.SaveCardData(fakeEntities);
    }
}