using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    public TextMeshProUGUI planetName;
    public TextMeshProUGUI planetDescription;
    public Image planetImage;
    public static MapManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        HideMap();
    }

    public void ToggleMap()
    {
        if (this.gameObject.activeSelf)
        {
            HideMap();
        }
        else
        {
            ShowMap();
        }
    }

    private void ShowMap()
    {
        this.gameObject.SetActive(true);
        PlayerStatManager.Instance.HideStats();
        MapListManager.Instance.InitFocus();
    }

    private void HideMap()
    {
        this.gameObject.SetActive(false);
        PlayerStatManager.Instance.ShowStats();
    }

    public void SetPlanetInfo(Planet planet)
    {
        Debug.Log("Planet Name " + planet.planetName);
        planetName.text = planet.planetName;
        planetDescription.text = planet.planetDescription;
        planetImage.sprite = planet.backgroundSprite;
    }
    // TODO: Implement map loading
    public void LoadMap(string mapName)
    {

    }
}
