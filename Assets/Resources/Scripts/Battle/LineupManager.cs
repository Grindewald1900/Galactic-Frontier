using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.UI;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.CharacterPanel;

namespace Assets.Resources.Scripts.Battle
{
    public class LineupManager : MonoBehaviour
    {
        public static LineupManager Instance;
        public Transform contentParent;
        public GameObject portraitPrefab;
        // public List<CardEntity> cardEntities = new();
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

                // cardEntities.Add(cardEntity);
                PortraitEntity portraitEntity = new PortraitEntity().SetShowFrame(false);
                portraitSlot.SetPortrait(portraitEntity, cardEntity);
                portraitSlots.Add(portraitSlot);
            }
            for (int i = 0; i < CardListManager.Instance.GetCardEntities().Count; i++)
            {
                var entity = CardListManager.Instance.GetCardEntities()[i];
                if (entity.GetLineupPosition() != LineupPosition.None)
                {
                    Instance.AddLineupCard(entity, (int)entity.GetLineupPosition());
                }
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
        public void AddLineupCard(CardEntity cardEntity, int index)
        {
            Debug.Log(GetType().Name + "portraitSlots size: " + portraitSlots.Count);
            Debug.Log(GetType().Name + "index: " + index);
            if (cardEntity == null) return;
            if (index < 0 || index >= portraitSlots.Count) return;
            if (portraitSlots.Exists(p => p.cardEntity.id == cardEntity.id)) return;

            CardEntity selectedEntity = portraitSlots[index].cardEntity;
            // If selected index is not null, set its position to None
            if (selectedEntity.characterName != CharacterName.Default)
            {
                Debug.Log("Selected not null");
                selectedEntity.SetLineupPosition(LineupPosition.None);
                // CardEntity currentEntity = CardListManager.Instance.GetCardEntityById(selectedEntity.id);
                // currentEntity?.SetLineupPosition(LineupPosition.None);
            }
            cardEntity?.SetLineupPosition((LineupPosition)index);
            Debug.Log($"AddLineupCard lineup card at position {cardEntity.GetLineupPosition()} " + index);

            // Update selected portrait slot
            PortraitEntity portraitEntity = new(cardEntity);
            portraitSlots.Find(p => p.slotIndex == index).SetPortrait(portraitEntity, cardEntity);
            // Refresh card list after adding new card to lineup or removing from lineup
            CardListManager.Instance.UpdateCardList(CardListManager.Instance.cardEntities);
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
