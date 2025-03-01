using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.UI;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Characters;

namespace Assets.Resources.Scripts.Battle
{
    public class LineupManager : MonoBehaviour
    {
        public static LineupManager Instance;
        public Transform contentParent;
        public GameObject portraitPrefab;
        public List<CardEntity> cardEntities = new();
        public List<PortraitSlot> portraitSlots = new();
        private int selectedIndex = -1;

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

        private void Init()
        {
            for (int i = 0; i < DefaultProperty.defaultLineupSize; i++)
            {
                if (contentParent.GetChild(i) == null) continue;
                PortraitSlot portraitSlot = contentParent.GetChild(i).GetComponent<PortraitSlot>();
                portraitSlot.slotIndex = i;

                //TODO:Fake data for testing
                // Character character = CardDataManager.Instance.GetCharacter();
                CardEntity cardEntity = new();
                Debug.Log("CardEntity Constructor Called ID: " + cardEntity.id);

                cardEntities.Add(cardEntity);
                PortraitEntity portraitEntity = new PortraitEntity().SetShowFrame(false);
                portraitSlot.SetPortrait(portraitEntity);
                portraitSlots.Add(portraitSlot);
            }
            SelectPortrait(selectedIndex);
        }

        public void SelectPortrait(int index)
        {
            if (index >= portraitSlots.Count || index < 0)
            {
                CardPreviewController.Instance.SetAddButtonInteractable(false);
                CardPreviewController.Instance.SetRemoveButtonInteractable(false);
                return;
            }
            else
            {
                CardPreviewController.Instance.SetAddButtonInteractable(true);
            }
            selectedIndex = index;
            foreach (PortraitSlot portraitSlot in portraitSlots)
            {
                portraitSlot.SetSelected(portraitSlot.slotIndex == index);
            }
        }

        // Add to current selected position
        public void AddLineupCard(CardEntity cardEntity)
        {
            if (cardEntity == null) return;
            if (cardEntities.Exists(c => c.id == cardEntity.id)) return;

            CardEntity selectedEntity = cardEntities[selectedIndex];
            // If selected index is not null, set its position to None
            if (selectedEntity.characterName != CharacterName.Default)
            {
                Debug.Log("Selected not null");
                CardEntity currentCard = CardListManager.Instance.GetCardEntityById(cardEntities[selectedIndex].id);
                currentCard?.SetLineupPosition(LineupPosition.None);
            }
            CardEntity card = CardListManager.Instance.GetCardEntityById(cardEntity.id);
            card?.SetLineupPosition((LineupPosition)selectedIndex);
            Debug.Log($"AddLineupCard lineup card at position {card.GetLineupPosition()} " + selectedIndex);

            // Update selected portrait slot
            PortraitEntity portraitEntity = new(cardEntity);
            cardEntities[selectedIndex] = cardEntity;
            portraitSlots.Find(p => p.slotIndex == selectedIndex).SetPortrait(portraitEntity);
        }

        public void SetLineupCardByPosition(int index, CardEntity cardEntity)
        {
            Debug.Log($"SetLineupCardByPosition card at position {index}");
            if (index >= portraitSlots.Count) return;
            if (cardEntity == null) return;

            PortraitEntity portraitEntity = new(cardEntity);
            cardEntities[index] = cardEntity;
            portraitSlots[index].SetPortrait(portraitEntity);
        }

        public int GetSelectedIndex()
        {
            return selectedIndex;
        }

        public void SetSelectedIndex(int index)
        {
            selectedIndex = index;
        }
    }
}
