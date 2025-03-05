using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Resources.Scripts.UI
{
    public class HoverShowEditButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject editButton; // 需要显示/隐藏的编辑按钮

        void Start()
        {
            editButton?.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            editButton?.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            editButton?.SetActive(false);
        }
    }
}