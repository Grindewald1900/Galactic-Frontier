using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Assets.Resources.Scripts.Cards
{
    // Script for managing the card panel filter and sorting functionality
    public class CardPanelController : MonoBehaviour
    {
        public CardPanelController instance;
        public TMP_Dropdown dropdown;

        public void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        void Start()
        {
            ResetDropdownOptions();
            dropdown.onValueChanged.RemoveAllListeners(); // 先清空旧的事件绑定
            dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        public void ResetDropdownOptions()
        {
            List<string> newOptions = new() { "Name", "Tier", "Level", "Power" };

            dropdown.ClearOptions(); // 清空所有选项，确保不会访问旧的 `OptionData`
            dropdown.AddOptions(newOptions);
            dropdown.RefreshShownValue(); // 确保 UI 更新
        }

        public void OnDropdownValueChanged(int index)
        {
            if (dropdown == null) return; // 防止 `Dropdown` 已销毁
            if (index >= 0 && index < dropdown.options.Count)
            {
                switch (index)
                {
                    case 0:
                        CardListManager.Instance.SortCardsByName();
                        break;
                    case 1:
                        CardListManager.Instance.SortCardsByTier();
                        break;
                    case 2:
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Debug.LogWarning("Index out of range!");
            }
        }
    }
}