using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.UI;

namespace Assets.Resources.Scripts.Cards
{
    public class CardPreviewController : MonoBehaviour
    {
        public static CardPreviewController Instance;
        public Button addButton;
        public Button removeButton;
        public Button upgradeButton;
        public Button dismissButton;
        public TextMeshProUGUI cardLevelText;
        public TextMeshProUGUI cardExpText;
        public TextMeshProUGUI cardPowerText;
        public TextMeshProUGUI cardTierText;
        public TextMeshProUGUI cardTypeText;

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
            upgradeButton.onClick.AddListener(() => UpgradeCard());
            SetUpgradeButtonInteractable(false);
            card.cardEntity.OnDataChanged += RefreshUI;
        }

        private void AddToLineup()
        {
            LineupManager.Instance.AddLineupCard(card.cardEntity);
        }

        private void UpgradeCard()
        {
            card.cardEntity.UpgradeCard();
            RefreshUI();
        }

        private void RefreshUI()
        {
            UpdateCardInfo();
        }

        public void SetAddButtonInteractable(bool isAddable)
        {
            addButton.interactable = isAddable;
        }

        public void SetRemoveButtonInteractable(bool isRemovable)
        {
            removeButton.interactable = isRemovable;
        }

        public void SetUpgradeButtonInteractable(bool isUpgradeable)
        {
            upgradeButton.interactable = isUpgradeable;
        }

        public void SetDismissButtonInteractable(bool isDismissable)
        {
            dismissButton.interactable = isDismissable;
        }

        public void ShowCardPreview(CardEntity cardEntity)
        {
            card.InitCard(cardEntity);
            card.gameObject.SetActive(true);
            SetUpgradeButtonInteractable(cardEntity.EvolutionPending);
            HoverShowDetailPanel.Instance.SetDetailPanel(cardEntity);
            UpdateCardInfo();
        }

        private void UpdateCardInfo()
        {
            cardLevelText.text = "Level: " + card.cardEntity.Level.ToString();
            cardExpText.text = "Exp: " + card.cardEntity.CurrentExp.ToString() + "/" + card.cardEntity.expToLevelUp.ToString();
            cardPowerText.text = "Power: " + card.cardEntity.power.ToString();
            cardTierText.text = "Tier: " + card.cardEntity.characterTier.ToString();
            cardTypeText.text = "Type: " + card.cardEntity.cardType.ToString();
            SetUpgradeButtonInteractable(card.cardEntity.EvolutionPending);
        }
    }
}