using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

namespace Assets.Resources.Scripts.Test
{
    public class DebugPanelController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static DebugPanelController Instance;
        public GameObject debugPanel; // 调试面板
        public Button closeButton; // 关闭按钮
        private RectTransform rectTransform;
        private Canvas canvas;
        private Vector2 originalPosition;
        private Vector3 originalScale;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            originalScale = rectTransform.localScale;
            canvas = GetComponentInParent<Canvas>();
            debugPanel?.SetActive(false);
            closeButton.onClick.AddListener(HideDebugPanel);
        }

        public void ShowDebugPanel()
        {
            Debug.Log("Show debug panel");
            debugPanel?.SetActive(true);
        }

        public void HideDebugPanel()
        {
            Debug.Log("Hide debug panel");
            debugPanel?.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            originalPosition = rectTransform.anchoredPosition;
            // 开始拖拽时，缩小Image
            rectTransform.DOScale(0.8f, 0.2f).SetEase(Ease.OutQuad);
            Debug.Log("Pointer drag Down position: " + originalPosition);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (canvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.GetComponent<RectTransform>(),
                                                                    eventData.position,
                                                                    eventData.pressEventCamera,
                                                                    out Vector2 localPointerPosition);

            rectTransform.anchoredPosition = localPointerPosition;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float minValue = (rectTransform.rect.height - screenHeight) / 2;
            float maxValue = (screenHeight - rectTransform.rect.height) / 2;
            Debug.Log("Min Value: " + minValue + ", Max Value: " + maxValue);
            Vector2 worldPos = rectTransform.anchoredPosition;
            Debug.Log("Pointer drag Up position: " + worldPos);

            // 计算是否移动到左侧还是右侧
            bool moveToLeft = worldPos.x < 0;

            // 计算新的X坐标（左边界或右边界）
            float newX = moveToLeft ? (rectTransform.rect.width - screenWidth) / 2 : (screenWidth - rectTransform.rect.width) / 2;
            float newY = Mathf.Clamp(rectTransform.anchoredPosition.y, minValue, maxValue);
            Vector2 targetPosition = new(newX, newY);
            Debug.Log("traget drag position: " + targetPosition);
            StartCoroutine(MoveToPosition(targetPosition));
            rectTransform.DOScale(originalScale, 0.3f).SetEase(Ease.OutQuad);
        }

        private IEnumerator MoveToPosition(Vector2 targetPosition)
        {
            const float duration = 0.3f;
            float elapsedTime = 0f;
            Vector2 startPos = rectTransform.anchoredPosition;
            Debug.Log("startPos drag position: " + targetPosition);

            while (elapsedTime < duration)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPosition, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            rectTransform.anchoredPosition = targetPosition;
        }
    }
}