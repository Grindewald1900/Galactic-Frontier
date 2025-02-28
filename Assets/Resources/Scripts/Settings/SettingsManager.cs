using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections.Generic;

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
        DataUtil.SaveCardData(cardEntities);
    }

    public void ClearCards()
    {
        CardListManager.Instance.ClearCardEntities();
        List<CardEntity> cardEntities = CardListManager.Instance.GetCardEntities();
        DataUtil.SaveCardData(cardEntities);
    }
}