using System.Collections.Generic;
using UnityEngine;

public class CardListController : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform gridParent;
    public CardListController instance;

    // 存储所有技能槽
    public List<CardEntity> cardEntities = new List<CardEntity>();

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
        for (int i = 0; i < cardEntities.Count; i++)
        {
            GameObject cardGO = Instantiate(cardPrefab, gridParent);
            cardGO.SetActive(true);
            Card card = cardGO.GetComponent<Card>();
            card.InitCard(cardEntities[i]);
            card.isBattleActive = false;
            card.OnCardClicked += OnCardClicked;
        }
    }

    private void OnCardClicked(CardEntity cardEntity)
    {
        Debug.Log("Card clicked: " + cardEntity.cardName);
        CardPreviewController.instance.ShowCardPreview(cardEntity);
    }

    public void FakeCardList()
    {
        for (int i = 0; i < 10; i++)
        {
            CardEntity cardEntity = new CardEntity().SetSpeed(UnityEngine.Random.Range(25f, 35f)).SetCardName("Asra").SetCharacter(Character.Asra).SetArchetype(Archetype.Mechanician).SetCharacterTier(CharacterTier.TierF);
            cardEntities.Add(cardEntity);
        }
    }
}