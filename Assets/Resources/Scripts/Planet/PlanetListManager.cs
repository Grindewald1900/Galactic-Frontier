using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.UI;

namespace Assets.Resources.Scripts.Planet
{
    public class PlanetListManager : MonoBehaviour
    {
        public static PlanetListManager Instance;
        public GameObject itemPrefab;
        public Transform contentParent;
        private List<PlanetSlot> items = new();
        private List<PlanetEntity> planetEntities = new List<PlanetEntity>();
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

        public void SetCurrentFocus(PlanetSlot newFocus)
        {
            foreach (var item in items)
            {
                item.SetFocus(item == newFocus);
            }
            currentIndex = items.IndexOf(newFocus);
            RadarSystem.Instance.SetRadarItemFocus(items[currentIndex].currentPlanet.planetName);
            PlanetDetailManager.Instance.SetPlanetDetail(items[currentIndex].currentPlanet);
        }

        public void AddItem(PlanetEntity planet)
        {
            GameObject newItem = Instantiate(itemPrefab, contentParent);
            PlanetSlot planetSlot = newItem.GetComponent<PlanetSlot>();
            planetSlot.SetPlanet(planet);
            items.Add(planetSlot);
        }

        public void RemoveItem(PlanetSlot item)
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
}