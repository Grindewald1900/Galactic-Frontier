using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Horizontal looping strip: idle auto-scroll, hover pauses and enlarges the item.</summary>
    internal sealed class BridgeSectorCarousel : MonoBehaviour
    {
        public RectTransform content;
        public float stride = 196f;
        public int uniqueCount;
        public float pixelsPerSecond = 36f;
        public float hoverScale = 1.16f;

        private RectTransform hovered;

        private void Update()
        {
            if (content == null || uniqueCount <= 0)
                return;

            if (hovered == null)
            {
                var pos = content.anchoredPosition;
                pos.x -= pixelsPerSecond * Time.unscaledDeltaTime;
                float loop = uniqueCount * stride;
                if (loop > 1f && pos.x <= -loop)
                    pos.x += loop;
                content.anchoredPosition = pos;
            }

            for (int i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i) as RectTransform;
                if (child == null) continue;
                float target = child == hovered ? hoverScale : 1f;
                child.localScale = Vector3.Lerp(child.localScale, Vector3.one * target, 14f * Time.unscaledDeltaTime);
            }
        }

        public void SetHover(RectTransform item, bool on)
        {
            if (on)
            {
                hovered = item;
                if (item != null)
                    item.SetAsLastSibling();
                return;
            }

            if (hovered == item)
                hovered = null;
        }
    }

    internal sealed class BridgeSectorCarouselItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public BridgeSectorCarousel owner;
        public System.Action onClick;

        public void OnPointerEnter(PointerEventData eventData) =>
            owner?.SetHover(transform as RectTransform, true);

        public void OnPointerExit(PointerEventData eventData) =>
            owner?.SetHover(transform as RectTransform, false);

        public void OnPointerClick(PointerEventData eventData) =>
            onClick?.Invoke();
    }
}
