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
    private List<PlanetItem> items = new List<PlanetItem>();
    private int currentIndex = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void InitFocus()
    {
        if (items.Count > 0)
        {
            SetCurrentFocus(items[0]);
        }
    }

    public void SetCurrentFocus(PlanetItem newFocus)
    {
        foreach (PlanetItem item in items)
        {
            item.SetFocus(item == newFocus);
        }
        currentIndex = items.IndexOf(newFocus);
    }

    public void AddItem(Planet planet)
    {
        GameObject newItem = Instantiate(itemPrefab, contentParent);
        PlanetItem planetItem = newItem.GetComponent<PlanetItem>();
        planetItem.SetPlanet(planet);
        items.Add(planetItem);
    }

    public void RemoveItem(PlanetItem item)
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