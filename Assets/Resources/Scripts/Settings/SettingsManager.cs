using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.Save;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.Test;
using Assets.Resources.Scripts.Scene;

namespace Assets.Resources.Scripts.Settings
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance;
        public Button generalButton;
        public Button graphicButton;
        public Button giftCodeButton;
        public Button clearCardsButton;
        public Button loadButton;
        public Button saveButton;
        public TMP_InputField inputField; // PY输入框
        public List<GameObject> panels;

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

        public void Init()
        {
            HideAllPanels();
            generalButton.onClick.AddListener(() => ShowPanel(0));
            graphicButton.onClick.AddListener(() => ShowPanel(1));
            giftCodeButton.onClick.AddListener(ShowGiftCode);
            clearCardsButton.onClick.AddListener(ClearCards);
            loadButton.onClick.AddListener(ShowGeneralSettings);
            saveButton.onClick.AddListener(SaveGame);
        }

        public void ShowGeneralSettings()
        {
            ShowPanel(0);
        }

        public void SaveGame()
        {
            List<CardEntity> cardEntities = CardListManager.Instance.GetCardEntities();
            DataUtil.Instance.SaveCardData(cardEntities);
            SceneLoader.Instance.LoadScene(nameof(SceneLoader.SceneName.MainScene));
        }

        public void ClearCards()
        {
            CardListManager.Instance.ClearCardEntities();
            List<CardEntity> cardEntities = CardListManager.Instance.GetCardEntities();
            DataUtil.Instance.SaveCardData(cardEntities);
        }

        public void ShowGiftCode()
        {
            ShowPanel(2);
            if (inputField != null)
            {
                inputField.onEndEdit.AddListener(OnEndEdit);
            }
        }

        void HideAllPanels()
        {
            foreach (var panel in panels)
            {
                panel.SetActive(false);
            }
        }

        void ShowPanel(int index)
        {
            Debug.Log("ShowPanel: " + index);
            if (index == -1) return; // 避免重复执行
            for (int i = 0; i < panels.Count; i++)
            {
                panels[i].SetActive(i == index); // 关闭所有面板
            }
        }

        void OnEndEdit(string input)
        {
            Debug.Log("Input: " + input);
            if (input.Equals(DefaultProperty.CODE_DEBUG_BOARD))
            {
                DebugPanelController.Instance.ShowDebugPanel();
            }
            else if (input.Equals(DefaultProperty.CODE_DEV_DATA))
            {
                var next = !DevDataSettings.Enabled;
                DevDataSettings.SetSessionEnabled(next);
                Debug.Log($"[DEV-DATA] Gift code toggled Dev Data Mode to {next}. Reload scenes to apply sample providers.");
            }
        }
    }
}