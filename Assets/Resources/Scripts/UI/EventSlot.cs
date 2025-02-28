using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class EventSlot : MonoBehaviour, IPointerClickHandler
{
    // public Image selectedIcon;
    // public Image bacgroundImage;
    public Image eventImage;
    public Image tagImage;
    public Image tagFrame;
    public TextMeshProUGUI text;
    public int slotIndex;
    private bool isFocused = false;
    private Vector3 defaultScale = Vector3.one;
    private Vector3 focusedScale = new Vector3(1.1f, 1.1f, 1f);

    void Awake()
    {
        SetFocus(false); // Set the selected icon to false by default
    }

    public void SetEvent(EventEntity eventEntity)
    {
        if (eventEntity == null) return;

        EnableEvent(eventEntity.isActivated);

        text.text = eventEntity.startDate + " - " + eventEntity.endDate;
        eventImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.eventImagePath, eventEntity.eventName);
        SetEventTag(eventEntity.eventType);
    }

    public void SetFocus(bool focus)
    {
        isFocused = focus;
        transform.localScale = isFocused ? focusedScale : defaultScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (EventManager.instance.GetEvent(slotIndex).isActivated)
        {
            EventManager.instance.SetEventFocus(slotIndex);
        }
    }

    private void EnableEvent(bool isEnable)
    {
        eventImage.enabled = isEnable;
        tagImage.enabled = isEnable;
        text.enabled = isEnable;
    }

    private void SetEventTag(MyEventType eventType)
    {
        if (eventType == MyEventType.New)
        {
            // tagImage.gameObject.SetActive(true);
            tagFrame.gameObject.SetActive(true);
            tagFrame.gameObject.SetActive(true);
            tagImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.UIImagePath, "new");
            tagFrame.sprite = ImageUtil.GetSpriteByName(ImageUtil.UIImagePath, "new");
        }
        else
        {
            tagFrame.gameObject.SetActive(false);
            tagImage.gameObject.SetActive(false);
        }
    }
}