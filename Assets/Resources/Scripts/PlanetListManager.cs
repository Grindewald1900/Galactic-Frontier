using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class PlanetListManager : MonoBehaviour
{
    public static PlanetListManager Instance;
    public GameObject itemPrefab;
    public Transform contentParent;
    private List<PlanetItem> items = new List<PlanetItem>();
    private List<PlanetEntity> planetEntities = new List<PlanetEntity>();
    private int currentIndex = 0;

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
        InitFocus();
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

    public void AddItem(PlanetEntity planet)
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

    public void FakeData()
    {
        List<string> planetNames = new List<string>() { "Planet_A_1", "Planet_A_2", "Planet_A_3", "Planet_A_4", "Planet_A_5", "Planet_A_6" };

        for (int i = 0; i < 10; i++)
        {
            PlanetEntity planetEntity = new PlanetEntity("Planet_A_1", "Level 1", "Planet", "Description 1");
            planetEntity.planetName = planetNames[Random.Range(0, planetNames.Count)];
            planetEntity.planetDescription = "Description " + i;
            planetEntity.planetLevel = "Level " + i;
            planetEntity.backgroundSprite = "planet" + i;
            planetEntities.Add(planetEntity);
            AddItem(planetEntity);
        }
    }
}