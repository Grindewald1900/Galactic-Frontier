using System;
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
    public class CardListManager : MonoBehaviour
    {
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform gridParent;

        public readonly List<Card> cards = new();
        public List<CardEntity> cardEntities = new();
        public static CardListManager Instance { get; private set; }
        public CardSortOrder Order { get; private set; } = CardSortOrder.Default;
        public IReadOnlyList<CardEntity> CardEntities => cardEntities;
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            InitCardList();
        }

        private void OnCardClicked(Card card)
        {
            if (card?.cardEntity == null) return;
            CardPreviewController.Instance?.ShowCardPreview(card.cardEntity);
            UnhighlightAllCards();
            card.Highlight(DefaultProperty.highlightCardScale);
        }
        public void InitCardList()
        {
            cardEntities = DataUtil.Instance.LoadCardData() ?? new List<CardEntity>();
            UpdateCardObjects(cardEntities.Count);
            SortCards(Order);
        }

        private void InitCard()
        {
            var cardGO = Instantiate(cardPrefab, gridParent);
            cardGO.SetActive(true);
            var card = cardGO.GetComponent<Card>();
            card.SetCardScale(DefaultProperty.defaultCardScale);
            cards.Add(card);
            card.OnCardClicked += OnCardClicked;
        }
        public void UpdateCardList(List<CardEntity> entities)
        {
            if (!gameObject.activeSelf) return;
            if (entities == null || entities.Count == 0)
            {
                CardPreviewController.Instance?.HideCardPreview();
                return;
            }
            UpdateCardObjects(entities.Count);
            int tempIndex = 0;
            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].GetLineupPosition() != LineupPosition.None) continue;
                cards[tempIndex].InitCard(entities[i]);
                tempIndex++;
            }
            for (int i = entities.Count - 1; i >= tempIndex; i--)
            {
                cards[i].HideCard();
            }
            if (cards.Count > 0)
            {
                cards[0].Highlight(DefaultProperty.highlightCardScale);
                CardPreviewController.Instance?.ShowCardPreview(cards[0].cardEntity);
            }
            DataUtil.Instance.SaveCardData(cardEntities);
        }
        public void UnhighlightAllCards()
        {
            foreach (var card in cards) card.Unhighlight(DefaultProperty.defaultCardScale);
        }
        public void SortCards(CardSortOrder sortOrder = CardSortOrder.Default)
        {
            Order = sortOrder;
            cardEntities = sortOrder switch
            {
                CardSortOrder.NameAscending or CardSortOrder.Default => cardEntities.OrderBy(card => card.characterName).ThenByDescending(card => card.CharacterTier).ToList(),
                CardSortOrder.NameDescending => cardEntities.OrderByDescending(card => card.characterName).ThenByDescending(card => card.CharacterTier).ToList(),
                CardSortOrder.TierAscending => cardEntities.OrderBy(card => card.CharacterTier).ToList(),
                CardSortOrder.TierDescending => cardEntities.OrderByDescending(card => card.CharacterTier).ToList(),
                CardSortOrder.LevelAscending => cardEntities.OrderBy(card => card.Level).ToList(),
                CardSortOrder.LevelDesending => cardEntities.OrderByDescending(card => card.Level).ToList(),
                CardSortOrder.PowerAscending => cardEntities.OrderBy(card => card.power).ToList(),
                CardSortOrder.PowerDesending => cardEntities.OrderByDescending(card => card.power).ToList(),
                _ => cardEntities
            };
            CardPanelController.Instance.typeDropdown.value = 0;
            UpdateCardList(cardEntities);
        }
        public void FilterCardsByType(Archetype cardType)
        {
            if (cardType == Archetype.Default)
            {
                SortCards(Order);
            }
            else
            {
                UpdateCardList(cardEntities.Where(c => c.archetype == cardType).ToList());
            }
        }
        public void AddCardEntity(CardEntity cardEntity)
        {
            if (cardEntity == null || cardEntities.Exists(c => c.id == cardEntity.id)) return;
            cardEntities.Add(cardEntity);
            UpdateCardList(cardEntities);
            DataUtil.Instance.SaveCardData(cardEntities);
        }
        public void AddCardEntity(List<CardEntity> mCardEntities)
        {
            if (mCardEntities == null) return;
            foreach (var cardEntity in mCardEntities)
            {
                if (cardEntities.Exists(c => c.id == cardEntity.id)) continue;
                cardEntities.Add(cardEntity);
            }
            UpdateCardList(cardEntities);
            DataUtil.Instance.SaveCardData(cardEntities);
        }
        public void RemoveCardEntity(CardEntity cardEntity)
        {
            if (cardEntity == null) return;
            cardEntities.RemoveAll(card => card.id == cardEntity.id);
            DataUtil.Instance.SaveCardData(cardEntities);
        }
        public void ClearCardEntities()
        {
            cardEntities.Clear();
            UpdateCardList(cardEntities);
            DataUtil.Instance.SaveCardData(cardEntities);
        }

        public List<CardEntity> GetCardEntities()
        {
            return cardEntities;
        }
        public List<CardEntity> GetInLineCardEntities()
        {
            return cardEntities.Where(entity => entity.GetLineupPosition() != LineupPosition.None).ToList();
        }
        public CardEntity GetCardEntityById(string id)
        {
            return cardEntities.Find(cards => cards.id == id);
        }
        private void UpdateCardObjects(int expectedCount)
        {
            while (cards.Count < expectedCount) InitCard();
            for (int i = cards.Count - 1; i >= expectedCount; i--)
            {
                Destroy(cards[i].gameObject);
                cards.RemoveAt(i);
            }
        }
        public void FakeCardList()
        {
            var fakeEntities = new List<CardEntity>();
            for (int i = 0; i < 30; i++)
            {
                var character = CardDataManager.Instance.GetCharacter();
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