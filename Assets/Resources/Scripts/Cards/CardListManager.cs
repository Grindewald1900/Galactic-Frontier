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

        private bool isNameAscending = false;
        private bool isTierAscending = false;

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
            CheckCardCount();

            for (int i = 0; i < cardEntities.Count; i++)
            {
                if (cardEntities[i].GetLineupPosition() != LineupPosition.None)
                {
                    LineupManager.Instance.AddLineupCard(cardEntities[i]);
                }
            }

            Debug.Log("cardEntities size: " + cardEntities.Count);
            Debug.Log("cards size: " + cards.Count);
            SortCardsByName();
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

        [System.Obsolete]
        public void UpdateCardList()
        {
            if (!gameObject.active) return;
            if (cardEntities.Count == 0) return;
            int tempIndex = 0;
            CheckCardCount();

            for (int i = 0; i < cardEntities.Count; i++)
            {
                if (cardEntities[i].GetLineupPosition() != LineupPosition.None) continue;
                // Debug.Log("Update card list, i: " + i + ", tempIndex: " + tempIndex);
                cards[tempIndex].InitCard(cardEntities[i]);
                tempIndex++;
            }
            for (int i = cardEntities.Count - 1; i >= tempIndex; i--)
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

        public void SortCardsByName()
        {
            isNameAscending = !isNameAscending;
            List<CardEntity> sortedEntities = new();
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
            if (cardEntities.Exists(c => c.id == cardEntity.id))
            {
                return;
            }
            cardEntities.Add(cardEntity);
            UpdateCardList();
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

            UpdateCardList();
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
            UpdateCardList();
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
        private void CheckCardCount()
        {
            if (cards.Count < cardEntities.Count)
            {
                for (int i = cards.Count; i < cardEntities.Count; i++)
                {
                    InitCard();
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
}