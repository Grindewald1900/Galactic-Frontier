using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Resources.Scripts.UI
{
    public class HoverShowEditButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Transform newParent;
        public Transform originalParent;
        public GameObject editButton; // 需要显示/隐藏的编辑按钮
        public bool moveToParent = false;

        void Start()
        {
            editButton?.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            editButton?.SetActive(true);
            if (moveToParent)
            {
                editButton.transform.SetParent(newParent);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            editButton?.SetActive(false);
            if (moveToParent)
            {
                editButton.transform.SetParent(originalParent);
            }
        }
    }
}