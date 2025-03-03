using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Utils;
using UnityEngine.SceneManagement;

namespace Assets.Resources.Scripts.Settings
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance;
        public Button loadButton;
        public Button saveButton;
        public Button clearCardsButton;

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
            loadButton.onClick.AddListener(LoadGame);
            saveButton.onClick.AddListener(SaveGame);
            clearCardsButton.onClick.AddListener(ClearCards);
        }

        public void LoadGame()
        {
            // Load settings from PlayerPrefs
        }

        public void SaveGame()
        {
            List<CardEntity> cardEntities = CardListManager.Instance.GetCardEntities();
            DataUtil.Instance.SaveCardData(cardEntities);
            SceneManager.LoadScene("MainMenuScene");
        }

        public void ClearCards()
        {
            CardListManager.Instance.ClearCardEntities();
            List<CardEntity> cardEntities = CardListManager.Instance.GetCardEntities();
            DataUtil.Instance.SaveCardData(cardEntities);
        }
    }
}