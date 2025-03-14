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
        public GameObject expertiseItem;
        public SkillItemSlot[] skillItemSlots;
        public Transform expertiseContent;
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
            card.cardEntity.OnCardUpgraded += UpdateExpertise;
        }

        private void AddToLineup()
        {
            LineupManager.Instance.AddLineupCard(card.cardEntity);
        }

        private void UpgradeCard()
        {
            card.cardEntity.UpgradeCard();
        }

        private void RefreshUI()
        {
            UpdateCardInfo();
            HoverShowDetailPanel.Instance.SetDetailPanel(card.cardEntity);
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
            UpdateSkills();
        }

        private void UpdateCardInfo()
        {
            cardLevelText.text = "Level: " + card.cardEntity.Level.ToString();
            cardExpText.text = "Exp: " + card.cardEntity.CurrentExp.ToString() + "/" + card.cardEntity.ExpToNextLevel.ToString();
            cardPowerText.text = "Power: " + card.cardEntity.power.ToString();
            cardTierText.text = "Tier: " + card.cardEntity.CharacterTier.ToString();
            cardTypeText.text = "Type: " + card.cardEntity.archetype.ToString();
            SetUpgradeButtonInteractable(card.cardEntity.EvolutionPending);
        }

        private void UpdateSkills()
        {
            var skillEntities = CardDataManager.Instance.GetSkillsByCharacter(card.cardEntity.characterName);
            Debug.Log("UpdateSkills " + card.cardEntity.characterName + "Count: " + skillEntities.Count + " skills");
            if (skillEntities.Count == 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    Debug.Log("UpdateSkills " + skillEntities[i].skillName.en);
                    skillItemSlots[i].SetSkillItem(skillEntities[i]);
                }
            }
        }

        private void UpdateExpertise()
        {
            int expertiseCount = card.cardEntity.expertises.Count;
            int childCount = expertiseContent.childCount;
            if (childCount > 0)
            {
                for (int i = 0; i < childCount; i++)
                {
                    Destroy(expertiseContent.GetChild(i).gameObject);
                }
            }
            for (int i = 0; i < expertiseCount; i++)
            {
                GameObject expertiseGO = Instantiate(expertiseItem, expertiseContent);
                SkillItemSlot expertiseSlot = expertiseGO.GetComponent<SkillItemSlot>();
                expertiseSlot.SetExpertiseItem(card.cardEntity.expertises[i]);
                Debug.Log("Update Expertise " + card.cardEntity.expertises[i].attributeType);
            }
        }
    }
}