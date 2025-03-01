using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Main;
using Assets.Resources.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

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
            {
                Instance = this;
            }
        }

        void Start()
        {
            FakeData();
            Init();
        }

        private void Init()
        {
            if (LogUtil.CheckNull(drawButtonOne, "drawButtonOne")) return;
            if (LogUtil.CheckNull(drawButtonTen, "drawButtonTen")) return;
            if (LogUtil.CheckNull(drawButtonReset, "drawButtonReset")) return;
            if (LogUtil.CheckNull(drawButton, "drawButton")) return;
            if (LogUtil.CheckNull(providerPrefab, "providerPrefab")) return;
            if (LogUtil.CheckNull(consumerPrefab, "consumerPrefab")) return;
            // Check Quantity
            HasEnoughQuantity();
            InitMaterialList();

            drawButtonOne.onClick.AddListener(() =>
            {
                Debug.Log("Draw One");
                AddDraw(1);
            });
            drawButtonTen.onClick.AddListener(() =>
            {
                Debug.Log("Draw Ten");
                AddDraw(10);
            });
            drawButtonReset.onClick.AddListener(() =>
            {
                Debug.Log("Reset");
                AddDraw(-drawCount);
            });
            drawButton.onClick.AddListener(() =>
            {
                Debug.Log("Draw");
                StartDraw();
            });
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
            MainScrollController.Instance.ShowPanel((int)MainMenuPanel.DRAWCARDS);
            CardResultManager.Instance.InitCards(drawCount);
        }

        private void HasEnoughQuantity()
        {
            bool hasOneDraw = true;
            bool hasTenDraw = true;
            bool hasReset = true;
            bool hasDraw = true;

            for (int i = 0; i < providerItems.Count; i++)
            {
                if (providerItems[i].quantity < itemQuantities[i])
                {
                    hasOneDraw = false;
                }
                if (providerItems[i].quantity < itemQuantities[i] * 10)
                {
                    hasTenDraw = false;
                }
                if (consumerItems[i].quantity < itemQuantities[i])
                {
                    hasDraw = false;
                }
                hasReset = drawCount > 0;
            }
            UpdateButtonState(hasOneDraw, hasTenDraw, hasReset, hasDraw);
        }

        private void UpdateQuantity()
        {
            for (int i = 0; i < itemQuantities.Count; i++)
            {
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

        private void FakeData()
        {
            int randomItemCounts = Random.Range(3, 6);
            List<string> itemNames = new List<string> { "Copper", "Steel", "GoldBar", "SteelBar", "Water", "Wood" };
            for (int i = 0; i < randomItemCounts; i++)
            {
                int randomCount = Random.Range(10, 20);
                ItemEntity providerItem = new ItemEntity("Item " + i, "Description " + i, itemNames[Random.Range(0, itemNames.Count)], 10 * i, ItemType.Material);
                providerItem.SetQuantity(Random.Range(100, 500));
                ItemEntity consumerItem = DeepCopyUtil.DeepCopy<ItemEntity>(providerItem);
                consumerItem.SetQuantity(0);
                providerItems.Add(providerItem);
                consumerItems.Add(consumerItem);
                itemQuantities.Add(randomCount);
            }
        }
    }
}