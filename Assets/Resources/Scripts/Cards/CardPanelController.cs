using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Assets.Resources.Scripts.Cards
{
    // Script for managing the card panel filter and sorting functionality
    public class CardPanelController : MonoBehaviour
    {
        public static CardPanelController Instance;
        public TMP_Dropdown orderDropdown;
        public TMP_Dropdown typeDropdown;

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        void Start()
        {
            ResetDropdownOptions();
            orderDropdown.onValueChanged.RemoveAllListeners(); // 先清空旧的事件绑定
            orderDropdown.onValueChanged.AddListener(OnOrderValueChanged);
            typeDropdown.onValueChanged.RemoveAllListeners();
            typeDropdown.onValueChanged.AddListener(OnTypeValueChanged);
        }

        public void ResetDropdownOptions()
        {
            List<string> orderOptions = new() { "Name Assending", "Name Desending", "Tier Assending", "Tier Desending", "Level Assending", "Level Desending", "Power Assending", "Power Desending" };
            List<string> typeOptions = new() { "All", "Assassin", "Magician", "Mechanician", "Monster", "Potioneer", "Warrior" };
            orderDropdown.ClearOptions(); // 清空所有选项，确保不会访问旧的 `OptionData`
            orderDropdown.AddOptions(orderOptions);
            orderDropdown.RefreshShownValue(); // 确保 UI 更新
            typeDropdown.ClearOptions();
            typeDropdown.AddOptions(typeOptions);
            typeDropdown.RefreshShownValue(); // 确保 UI 更新
        }

        public void OnOrderValueChanged(int index)
        {
            if (orderDropdown == null) return; // 防止 `Dropdown` 已销毁
            if (index >= 0 && index < orderDropdown.options.Count)
            {
                switch (index)
                {
                    case 0:
                        CardListManager.Instance.SortCards(CardSortOrder.NameAscending);
                        break;
                    case 1:
                        CardListManager.Instance.SortCards(CardSortOrder.NameDescending);
                        break;
                    case 2:
                        CardListManager.Instance.SortCards(CardSortOrder.TierAscending);
                        break;
                    case 3:
                        CardListManager.Instance.SortCards(CardSortOrder.TierDescending);
                        break;
                    case 4:
                        CardListManager.Instance.SortCards(CardSortOrder.LevelAscending);
                        break;
                    case 5:
                        CardListManager.Instance.SortCards(CardSortOrder.LevelDesending);
                        break;
                    case 6:
                        CardListManager.Instance.SortCards(CardSortOrder.PowerAscending);
                        break;
                    case 7:
                        CardListManager.Instance.SortCards(CardSortOrder.PowerDesending);
                        break;
                    default:
                        CardListManager.Instance.SortCards(CardSortOrder.Default);
                        break;
                }
            }
            else
            {
                Debug.LogWarning("Index out of range!");
            }
        }

        public void OnTypeValueChanged(int index)
        {
            if (typeDropdown == null) return; // 防止 `Dropdown` 已销毁
            if (index >= 0 && index < typeDropdown.options.Count)
            {
                switch (index)
                {
                    case 0:
                        CardListManager.Instance.FilterCardsByType(Archetype.Default);
                        break;
                    case 1:
                        CardListManager.Instance.FilterCardsByType(Archetype.Assassin);
                        break;
                    case 2:
                        CardListManager.Instance.FilterCardsByType(Archetype.Magician);
                        break;
                    case 3:
                        CardListManager.Instance.FilterCardsByType(Archetype.Mechanician);
                        break;
                    case 4:
                        CardListManager.Instance.FilterCardsByType(Archetype.Monster);
                        break;
                    case 5:
                        CardListManager.Instance.FilterCardsByType(Archetype.Potioneer);
                        break;
                    case 6:
                        CardListManager.Instance.FilterCardsByType(Archetype.Warrior);
                        break;
                    default:
                        CardListManager.Instance.FilterCardsByType(Archetype.Default);
                        break;
                }
            }
        }
    }
}