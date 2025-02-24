using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class MainScrollController : MonoBehaviour
{
    public ScrollRect scrollRect;   // 滚动组件
    public RectTransform content;   // Content 容器
    public GameObject itemPrefab;   // Item 预制体
    public List<GameObject> panels; // 存储所有 Panel

    public int totalItems = 10;  // 列表中的 UI 元素数量
    public float itemWidth = 200f; // 每个 UI 元素的宽度
    public float scaleFactor = 1.2f; // 点击后放大的大小
    public float scaleDuration = 0.2f; // 放大动画持续时间

    private GameObject selectedItem = null; // 当前选中的 Item
    private GameObject selectedPanel = null; // 当前显示的 Panel

    void Start()
    {
        HideAllPanels();
        InitializeItems();
        // 默认选中第一个 Item
        selectedItem = content.GetChild(0).gameObject;
        ShowPanel(0);
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
        if (LogUtil.CheckNull(itemPrefab, "itemPrefab")) return;
        if (LogUtil.CheckNull(content, "content")) return;

        for (int i = 0; i < totalItems; i++)
        {
            GameObject newItem = Instantiate(itemPrefab, content);
            if (LogUtil.CheckNull(newItem, "newItem")) continue;
            newItem.transform.localPosition = new Vector3(i * itemWidth, 0, 0);  // 水平排列

            TextMeshProUGUI itemTextComponent = newItem.GetComponentInChildren<TextMeshProUGUI>();
            if (LogUtil.CheckNull(itemTextComponent, "itemTextComponent")) continue;
            itemTextComponent.text = itemText[i];

            Transform targetTransform = newItem.transform.Find("Image");
            if (LogUtil.CheckNull(targetTransform, "targetTransform")) continue;
            Image targetImage = targetTransform.GetComponent<Image>();
            if (LogUtil.CheckNull(targetImage, "targetImage")) continue;
            Sprite sprite = ImageUtil.GetSpriteByName(ImageUtil.UIImagePath, itemText[i]);
            if (LogUtil.CheckNull(sprite, "sprite")) continue;
            targetImage.sprite = sprite;

            Button button = newItem.GetComponent<Button>();
            if (LogUtil.CheckNull(button, "button")) continue;
            int index = i;
            button.onClick.AddListener(() => OnItemClick(newItem, index));
        }
    }

    void OnItemClick(GameObject clickedItem, int index)
    {
        if (selectedItem == clickedItem) return; // 如果点击的是当前选中的，不做处理
        if (selectedItem != null)
        {
            StopAllCoroutines();
            StartCoroutine(ScaleItem(selectedItem, 1f)); // 还原大小
        }
        selectedItem = clickedItem;
        ShowPanel(index);
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

    void ShowPanel(int index)
    {
        //TODO: test code
        if (index == 6)
        {
            GalaxyGenerator.instance.ShowGalaxies();
        }
        else
        {
            GalaxyGenerator.instance.HideGalaxies();
        }
        HideAllPanels();

        if (index >= 0 && index < panels.Count)
        {
            selectedPanel = panels[index];
            selectedPanel.SetActive(true);
        }
    }

    void HideAllPanels()
    {
        foreach (var panel in panels)
        {
            panel.SetActive(false);
        }
    }
}