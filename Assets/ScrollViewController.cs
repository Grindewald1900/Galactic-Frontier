using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ScrollViewController : MonoBehaviour
{
    public ScrollRect scrollRect;   // 滚动组件
    public RectTransform content;   // Content 容器
    public GameObject itemPrefab;   // Item 预制体

    public int totalItems = 10;  // 列表中的 UI 元素数量
    public float itemWidth = 200f; // 每个 UI 元素的宽度
    public float scaleFactor = 1.2f; // 点击后放大的大小
    public float scaleDuration = 0.2f; // 放大动画持续时间

    private GameObject selectedItem = null; // 当前选中的 Item

    void Start()
    {
        InitializeItems();
        // 默认选中第一个 Item
        selectedItem = content.GetChild(0).gameObject;
        StartCoroutine(ScaleItem(selectedItem, scaleFactor));
    }

    public void InitializeItems()
    {
        List<string> itemText = new List<string>(){
            "Character",
            "Cards",
            "Battle",
            "Inventory",
            "Building",
            "Shop",
            "Setting"
        };
        if (itemPrefab == null)
        {
            Debug.LogError("itemPrefab is not set.");
            return;
        }
        if (content == null)
        {
            Debug.LogError("content is not set.");
            return;
        }
        for (int i = 0; i < totalItems; i++)
        {
            GameObject newItem = Instantiate(itemPrefab, content);
            if (newItem == null)
            {
                Debug.LogError("Failed to instantiate itemPrefab.");
                return;
            }

            newItem.transform.localPosition = new Vector3(i * itemWidth, 0, 0);  // 水平排列

            TextMeshProUGUI itemTextComponent = newItem.GetComponentInChildren<TextMeshProUGUI>();
            if (itemTextComponent == null)
            {
                Debug.LogError("Text component not found in itemPrefab.");
                return;
            }
            itemTextComponent.text = itemText[i];

            Button button = newItem.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnItemClick(newItem));
            }
        }
    }

    void OnItemClick(GameObject clickedItem)
    {
        if (selectedItem == clickedItem) return; // 如果点击的是当前选中的，不做处理

        // 还原之前选中的 Item
        if (selectedItem != null)
        {
            StopAllCoroutines();
            StartCoroutine(ScaleItem(selectedItem, 1f)); // 还原大小
        }

        // 设置当前选中的 Item
        selectedItem = clickedItem;
        StartCoroutine(ScaleItem(selectedItem, scaleFactor)); // 放大
    }

    IEnumerator ScaleItem(GameObject item, float targetScale)
    {
        float elapsedTime = 0f;
        Vector3 startScale = item.transform.localScale;
        Vector3 endScale = Vector3.one * targetScale;

        while (elapsedTime < scaleDuration)
        {
            elapsedTime += Time.deltaTime;
            item.transform.localScale = Vector3.Lerp(startScale, endScale, elapsedTime / scaleDuration);
            yield return null;
        }

        item.transform.localScale = endScale;
    }
}