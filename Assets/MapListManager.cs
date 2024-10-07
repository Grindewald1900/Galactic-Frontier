using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class MapListManager : MonoBehaviour
{
    public static MapListManager Instance;
    public GameObject itemPrefab;
    public Transform contentParent;
    private List<GameObject> items = new List<GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void AddItem(Planet planet)
    {
        GameObject newItem = Instantiate(itemPrefab, contentParent);
        PlanetItem planetItem = newItem.GetComponent<PlanetItem>();
        planetItem.SetPlanet(planet);
        items.Add(newItem);
    }

    public void RemoveItem(GameObject item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
            Destroy(item);
        }
    }

    public void RemoveAllItems()
    {
        foreach (var item in items)
        {
            Destroy(item);
        }
        items.Clear();
    }

    public void SortItemsByText()
    {

        items = items.OrderBy(item =>
        {
            var itemText = item.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
            return itemText != null ? itemText.text : "";
        }).ToList();

        for (int i = 0; i < items.Count; i++)
        {
            items[i].transform.SetSiblingIndex(i);
        }
    }
}