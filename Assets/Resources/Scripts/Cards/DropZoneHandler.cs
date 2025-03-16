using System.Collections.Generic;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.UI;
using Assets.Resources.Scripts.Utils;
using UnityEngine;

namespace Assets.Resources.Scripts.Cards
{
    public class DropZoneHandler : MonoBehaviour
    {
        private readonly List<Vector3> initialPositions = new();
        private readonly List<PortraitSlot> portraitSlots = new();
        public static DropZoneHandler Instance { get; private set; }
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        void Start()
        {
            Debug.Log("Start Positions: childCount: " + transform.childCount);
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i) != null)
                {
                    // Only for read purposes
                    float correctedPos = CorrectPosition(transform.GetChild(i).position.x);
                    Debug.Log("Index: " + i + " position: " + correctedPos);
                    PortraitSlot slot = transform.GetChild(i).GetComponent<PortraitSlot>();
                    portraitSlots.Add(slot);
                    initialPositions.Add(slot.transform.localPosition);
                }
            }
        }
        /**
            public void OnDrop(PointerEventData eventData)
            {
                Debug.Log("Drop Card: " + eventData.pointerDrag.name);
                PortraitSlot droppedCard = eventData.pointerDrag.GetComponent<PortraitSlot>();
                if (droppedCard != null)
                {
                    droppedCard.transform.SetParent(transform);
                    DropCard(droppedCard);
                    // droppedCard.transform.SetSiblingIndex(newIndex);
                }
            }
        **/

        public void DropCard(PortraitSlot droppedCard, Vector3 droppedPosition)
        {
            Debug.Log("Child count: " + transform.childCount);
            droppedCard.transform.SetParent(transform);
            // float droppedCardX = droppedCard.transform.position.x - DefaultProperty.screenSize.X / 2;

            for (int i = 0; i < transform.childCount; i++)
            {
                // If its dropped card, do not use its current position.(could be large number like 45678), probably because drag and drop.
                float xPos = portraitSlots[i].slotIndex == droppedCard.slotIndex ? droppedPosition.x : portraitSlots[i].transform.localPosition.x;
                portraitSlots[i].xPosition = xPos;
                // Debug.Log("Child Name " + transform.GetChild(i).name + " position x: " + xPos);
            }
            // Sort the slots based on their x positions.
            portraitSlots.Sort((a, b) => a.xPosition.CompareTo(b.xPosition));
            for (int i = 0; i < portraitSlots.Count; i++)
            {
                if (droppedPosition.x == portraitSlots[i].xPosition)
                {
                    LineupManager.Instance.SetSelectedIndex(i);
                }
                portraitSlots[i].slotIndex = i;
            }
            UpdateSlots();
        }

        private void UpdateSlots()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                PortraitSlot slot = transform.GetChild(i).GetComponent<PortraitSlot>();
                slot.cardEntity.SetLineupPosition(slot.slotIndex);
                slot.transform.localPosition = initialPositions[slot.slotIndex];
            }
            DataUtil.Instance.SaveCardData(CardListManager.Instance.cardEntities);
        }

        private float CorrectPosition(float position)
        {
            return position * (1 / DefaultProperty.defaultCanvasScale);
        }
    }
}