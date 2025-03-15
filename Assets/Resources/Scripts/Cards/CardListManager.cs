using System.Collections.Generic;
using System.Linq;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Characters;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.Utils;
using UnityEngine;

namespace Assets.Resources.Scripts.Cards
{
    // List of card in Card Panel, should be attached to CardPanel(parent)
    public class CardListManager : MonoBehaviour
    {
        public GameObject cardPrefab;
        public Transform gridParent;
        // private int selectedIndex = 0;
        public List<Card> cards;
        public List<CardEntity> cardEntities = new();
        public static CardListManager Instance;
        public CardSortOrder order = CardSortOrder.Default;

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
            CardPreviewController.Instance.ShowCardPreview(card.cardEntity);
            UnhighlightAllCards();
            card.Highlight(DefaultProperty.highlightCardScale);
        }

        public void InitCardList()
        {
            cardEntities = DataUtil.Instance.LoadCardData();
            CheckCardCount(cardEntities);

            for (int i = 0; i < cardEntities.Count; i++)
            {
                if (cardEntities[i].GetLineupPosition() != LineupPosition.None)
                {
                    LineupManager.Instance.AddLineupCard(cardEntities[i]);
                }
            }

            Debug.Log("cardEntities size: " + cardEntities.Count);
            Debug.Log("cards size: " + cards.Count);
            SortCards();
        }

        private void InitCard()
        {
            GameObject cardGO = Instantiate(cardPrefab, gridParent);
            cardGO.SetActive(true);
            Card card = cardGO.GetComponent<Card>();
            card.SetCardScale(DefaultProperty.defaultCardScale);
            cards.Add(card);
            card.OnCardClicked += OnCardClicked;
        }

        public void UpdateCardList(List<CardEntity> entities)
        {
            if (!gameObject.activeSelf) return;
            if (entities.Count == 0)
            {
                CardPreviewController.Instance.HideCardPreview();
            }
            int tempIndex = 0;
            CheckCardCount(entities);

            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].GetLineupPosition() != LineupPosition.None) continue;
                // Debug.Log("Update card list, i: " + i + ", tempIndex: " + tempIndex);
                cards[tempIndex].InitCard(entities[i]);
                tempIndex++;
            }
            for (int i = entities.Count - 1; i >= tempIndex; i--)
            {
                Debug.Log("Hide card at index: " + i);
                cards[i].HideCard();
            }

            Debug.Log("Cards size: " + cards.Count);
            cards[0].Highlight(DefaultProperty.highlightCardScale);
            CardPreviewController.Instance?.ShowCardPreview(cards[0].cardEntity);
        }

        public void UnhighlightAllCards()
        {
            foreach (Card card in cards)
            {
                card.Unhighlight(DefaultProperty.defaultCardScale);
            }
        }

        public void SortCards(CardSortOrder sortOrder = CardSortOrder.Default)
        {
            List<CardEntity> sortedEntities = new();
            order = sortOrder;
            Debug.Log("Sort cards by: " + order);
            if (order == CardSortOrder.NameAscending || order == CardSortOrder.Default)
            {
                cardEntities = cardEntities.OrderBy(card => card.characterName).ThenByDescending(card => card.CharacterTier).ToList();
            }
            if (order == CardSortOrder.NameDescending)
            {
                cardEntities = cardEntities.OrderByDescending(card => card.characterName).ThenByDescending(card => card.CharacterTier).ToList();
            }
            if (order == CardSortOrder.TierAscending)
            {
                cardEntities.Sort((a, b) => a.CharacterTier.CompareTo(b.CharacterTier));
            }
            if (order == CardSortOrder.TierDescending)
            {
                cardEntities.Sort((a, b) => b.CharacterTier.CompareTo(a.CharacterTier));
            }
            if (order == CardSortOrder.LevelAscending)
            {
                cardEntities.Sort((a, b) => a.level.CompareTo(b.Level));
            }
            if (order == CardSortOrder.LevelDesending)
            {
                cardEntities.Sort((a, b) => b.level.CompareTo(a.Level));
            }
            if (order == CardSortOrder.PowerAscending)
            {
                cardEntities.Sort((a, b) => a.power.CompareTo(b.power));
            }
            if (order == CardSortOrder.PowerDesending)
            {
                cardEntities.Sort((a, b) => b.power.CompareTo(a.power));
            }
            CardPanelController.Instance.typeDropdown.value = 0;
            UpdateCardList(cardEntities);
        }

        public void FilterCardsByType(Archetype cardType)
        {
            Debug.Log("Filter cards by: " + cardType);
            if (cardType == Archetype.Default)
            {
                SortCards();
            }
            else
            {
                UpdateCardList(cardEntities.FindAll(c => c.archetype == cardType));
            }
        }

        public void AddCardEntity(CardEntity cardEntity)
        {
            Debug.Log("Add card entity: " + cardEntities.Count);
            if (cardEntities.Exists(c => c.id == cardEntity.id))
            {
                return;
            }
            cardEntities.Add(cardEntity);
            UpdateCardList(cardEntities);
            DataUtil.Instance.SaveCardData(cardEntities);
        }

        public void AddCardEntity(List<CardEntity> mCardEntities)
        {
            Debug.Log("Add card entities: " + cardEntities.Count);
            foreach (CardEntity cardEntity in mCardEntities)
            {
                if (cardEntities.Exists(c => c.id == cardEntity.id))
                {
                    continue;
                }
                cardEntities.Add(cardEntity);
            }
            Debug.Log("Add card entities: " + cardEntities.Count);

            UpdateCardList(cardEntities);
            DataUtil.Instance.SaveCardData(cardEntities);
        }

        public void RemoveCardEntity(CardEntity cardEntity)
        {
            cardEntities.RemoveAll(card => card.id == cardEntity.id);
            DataUtil.Instance.SaveCardData(cardEntities);
        }

        public void ClearCardEntities()
        {
            Debug.Log("Clear card entities");
            cardEntities.Clear();
            UpdateCardList(cardEntities);
            DataUtil.Instance.SaveCardData(cardEntities);
        }

        public List<CardEntity> GetCardEntities()
        {
            return cardEntities;
        }

        public CardEntity GetCardEntityById(string id)
        {
            return cardEntities.Find(cardEntities => cardEntities.id == id);
        }

        // Make sure there are enough cards in the card panel to display all the cards
        private void CheckCardCount(List<CardEntity> entities)
        {
            if (cards.Count < entities.Count)
            {
                for (int i = cards.Count; i < entities.Count; i++)
                {
                    InitCard();
                }
            }
            else
            {
                for (int i = cards.Count - 1; i >= entities.Count; i--)
                {
                    Destroy(cards[i].gameObject);
                    cards.RemoveAt(i);
                }
            }
        }

        public void FakeCardList()
        {
            List<CardEntity> fakeEntities = new();
            for (int i = 0; i < 30; i++)
            {
                Character character = CardDataManager.Instance.GetCharacter();
                fakeEntities.Add(CardDataManager.Instance.GetCardEntity(character));
            }
        }
    }

    public enum CardSortOrder
    {
        Default,
        NameAscending,
        NameDescending,
        TierAscending,
        TierDescending,
        LevelAscending,
        LevelDesending,
        PowerAscending,
        PowerDesending
    }
}