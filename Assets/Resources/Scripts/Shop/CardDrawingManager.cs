using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Gacha;
using Assets.Resources.Scripts.Main;
using Assets.Resources.Scripts.UI;
using Assets.Resources.Scripts.Utils.Save;
using UnityEngine;
using UnityEngine.UI;
using static Assets.Resources.Scripts.Main.GameStatusManager;

namespace Assets.Resources.Scripts.Shop
{
    public class CardDrawingManager : MonoBehaviour
    {
        public static CardDrawingManager Instance;
        public GameObject providerPrefab;
        public GameObject consumerPrefab;
        public GameObject materialPrefab;
        public Transform providerContent;
        public Transform consumerContent;
        public Transform materialContent;
        public Button drawButtonOne;
        public Button drawButtonTen;
        public Button drawButtonReset;
        public Button drawButton;
        public List<ItemEntity> providerItems = new();
        public List<ItemEntity> consumerItems = new();
        private List<CardMaterialSlot> providerSlots = new();
        private List<CardMaterialSlot> consumerSlots = new();
        public List<int> itemQuantities = new();
        private int drawCount = 0;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        void Start()
        {
            TryLoadSampleMaterials();
            Init();
        }

        private void TryLoadSampleMaterials()
        {
            if (DevData.IsActive)
            {
                DevData.Current.FillSampleGachaMaterials(providerItems, consumerItems, itemQuantities);
                Debug.Log($"[DEV-DATA] Loaded {providerItems.Count} sample gacha materials (memory only).");
                return;
            }

            DevData.LogSkipped(nameof(CardDrawingManager) + ".FillSampleGachaMaterials");
            LoadLiveTicketMaterials();
        }

        private void LoadLiveTicketMaterials()
        {
            providerItems.Clear();
            consumerItems.Clear();
            itemQuantities.Clear();
            GachaService.EnsureLoaded();
            var have = GachaService.TicketCount();
            var ticket = ItemFactory.FromDef(GachaRules.TicketDefId, 1, EconomyConstants.DefaultQuality);
            ticket.quantity = Math.Max(0, have);
            var reserved = ItemFactory.FromDef(GachaRules.TicketDefId, 1, EconomyConstants.DefaultQuality);
            reserved.quantity = 0;
            providerItems.Add(ticket);
            consumerItems.Add(reserved);
            itemQuantities.Add(GachaRules.CostPerPull);
        }

        private void Init()
        {
            if (LogUtil.CheckNull(drawButtonOne, "drawButtonOne")) return;
            if (LogUtil.CheckNull(drawButtonTen, "drawButtonTen")) return;
            if (LogUtil.CheckNull(drawButtonReset, "drawButtonReset")) return;
            if (LogUtil.CheckNull(drawButton, "drawButton")) return;
            if (LogUtil.CheckNull(providerPrefab, "providerPrefab")) return;
            if (LogUtil.CheckNull(consumerPrefab, "consumerPrefab")) return;
            HasEnoughQuantity();
            InitMaterialList();

            drawButtonOne.onClick.AddListener(() => AddDraw(1));
            drawButtonTen.onClick.AddListener(() => AddDraw(10));
            drawButtonReset.onClick.AddListener(() => AddDraw(-drawCount));
            drawButton.onClick.AddListener(StartDraw);

            foreach (ItemEntity item in providerItems)
            {
                GameObject cardGO = Instantiate(providerPrefab, providerContent);
                cardGO.SetActive(true);
                CardMaterialSlot cardSlot = cardGO.GetComponent<CardMaterialSlot>();
                cardSlot.SetItem(item);
                providerSlots.Add(cardSlot);
            }
            foreach (ItemEntity item in consumerItems)
            {
                GameObject cardGO = Instantiate(consumerPrefab, consumerContent);
                cardGO.SetActive(true);
                CardMaterialSlot cardSlot = cardGO.GetComponent<CardMaterialSlot>();
                cardSlot.SetItem(item);
                consumerSlots.Add(cardSlot);
            }
        }

        private void InitMaterialList()
        {
            for (int i = 0; i < consumerItems.Count; i++)
            {
                ItemEntity item = consumerItems[i];
                GameObject cardGO = Instantiate(materialPrefab, materialContent);
                cardGO.SetActive(true);
                CardMaterialSlot cardSlot = cardGO.GetComponent<CardMaterialSlot>();
                cardSlot.SetItem(item);
                cardSlot.SetQuantity(itemQuantities[i]);
            }
        }

        private void AddDraw(int count)
        {
            drawCount += count;
            if (providerItems.Count != consumerItems.Count)
            {
                LogUtil.LogError("Provider and Consumer items count are not equal.");
                return;
            }
            for (int i = 0; i < providerItems.Count; i++)
            {
                providerItems[i].quantity -= itemQuantities[i] * count;
                consumerItems[i].quantity += itemQuantities[i] * count;
            }
            HasEnoughQuantity();
            UpdateQuantity();
        }

        private void StartDraw()
        {
            if (!DevData.IsActive)
            {
                var result = GachaService.TryPull(drawCount, grantImmediately: false);
                if (!result.Success)
                {
                    Debug.LogWarning("[GACHA] " + result.Message);
                    return;
                }

                MainScrollController.Instance.ShowPanel(CurrentScene.DRAWCARDS_MENU);
                CardResultManager.Instance.PresentResults(result.Cards, grantOnReport: true);
                drawCount = 0;
                LoadLiveTicketMaterials();
                HasEnoughQuantity();
                UpdateQuantity();
                return;
            }

            MainScrollController.Instance.ShowPanel(CurrentScene.DRAWCARDS_MENU);
            CardResultManager.Instance.InitCards(drawCount);
        }

        private void HasEnoughQuantity()
        {
            if (providerItems.Count == 0 || consumerItems.Count == 0)
            {
                UpdateButtonState(false, false, false, false);
                return;
            }

            bool hasOneDraw = true;
            bool hasTenDraw = true;
            bool hasReset = drawCount > 0;
            bool hasDraw = true;

            for (int i = 0; i < providerItems.Count; i++)
            {
                if (providerItems[i].quantity < itemQuantities[i])
                    hasOneDraw = false;
                if (providerItems[i].quantity < itemQuantities[i] * 10)
                    hasTenDraw = false;
                if (consumerItems[i].quantity < itemQuantities[i])
                    hasDraw = false;
            }
            UpdateButtonState(hasOneDraw, hasTenDraw, hasReset, hasDraw);
        }

        private void UpdateQuantity()
        {
            for (int i = 0; i < itemQuantities.Count; i++)
            {
                if (i >= providerSlots.Count || i >= consumerSlots.Count) continue;
                providerSlots[i].SetQuantity(providerItems[i].quantity);
                consumerSlots[i].SetQuantity(consumerItems[i].quantity);
            }
        }

        private void UpdateButtonState(bool oneState, bool tenState, bool resetState, bool drawState)
        {
            drawButtonOne.interactable = oneState;
            drawButtonTen.interactable = tenState;
            drawButtonReset.interactable = resetState;
            drawButton.interactable = drawState;
        }
    }
}
