using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LineupManager : MonoBehaviour
{
    public static LineupManager Instance;
    public Transform contentParent;
    public GameObject portraitPrefab;
    public List<CardEntity> cardEntities = new List<CardEntity>();
    public List<PortraitSlot> portraitSlots = new List<PortraitSlot>();


    private Transform originalParent;
    private int originalIndex;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

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

            Character character = CardDataManager.Instance.GetCharacter();
            CardEntity cardEntity = CardDataManager.Instance.GetCardEntity(character);
            string name = cardEntity.characterName.ToString();
            PortraitEntity portraitEntity = new PortraitEntity(name, cardEntity.characterTier.ToString(), cardEntity.characterTier);
            portraitSlot.SetPortrait(portraitEntity);

            portraitSlots.Add(portraitSlot);
        }
    }

    public void SelectPortrait(int index)
    {
        if (index >= portraitSlots.Count) return;
        foreach (PortraitSlot portraitSlot in portraitSlots)
        {
            portraitSlot.SetSelected(portraitSlot.slotIndex == index);
        }
    }

}