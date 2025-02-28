using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Transform originalParent;
    private int originalIndex;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>(); // **确保 Prefab 上有 CanvasGroup**
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("Start Dragging Card");
        originalParent = transform.parent;
        originalIndex = transform.GetSiblingIndex(); // 记录原始索引

        canvasGroup.blocksRaycasts = false; // **让拖拽中的卡牌不会阻挡射线**
        transform.SetParent(originalParent.parent); // **脱离 LayoutGroup**
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Debug.Log("Draging Card pos: " + eventData.position);
        rectTransform.position = eventData.position; // **拖拽跟随鼠标**
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("Start Dragging End");
        canvasGroup.blocksRaycasts = true; // **重新启用射线检测**
        // transform.SetParent(originalParent); // **回到 LayoutGroup**
        // transform.SetSiblingIndex(originalIndex); // **放回原索引**
    }
}