using UnityEngine;
using UnityEngine.UI;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Entity;

namespace Assets.Resources.Scripts.Cards
{
    public class CardPreviewController : MonoBehaviour
    {
        public static CardPreviewController Instance;
        public Button addButton;
        public Button removeButton;
        public Card card;

        public void Awake()
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

        private void Init()
        {
            addButton.onClick.AddListener(() => AddToLineup());
        }

        private void AddToLineup()
        {
            LineupManager.Instance.AddLineupCard(card.cardEntity);
            CardListManager.Instance.UpdateCardList();
        }

        public void SetAddButtonInteractable(bool isAddable)
        {
            addButton.interactable = isAddable;
        }

        public void SetRemoveButtonInteractable(bool isRemovable)
        {
            removeButton.interactable = isRemovable;
        }

        public void ShowCardPreview(CardEntity cardEntity)
        {
            card.InitCard(cardEntity);
            card.gameObject.SetActive(true);
        }
    }
}