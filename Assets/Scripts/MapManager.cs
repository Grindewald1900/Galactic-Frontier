using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
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

    }

    private void HideMap()
    {
        this.gameObject.SetActive(false);
        PlayerStatManager.Instance.ShowStats();
    }

    // TODO: Implement map loading
    public void LoadMap(string mapName)
    {

    }
}
