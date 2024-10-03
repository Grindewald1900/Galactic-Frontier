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
        CloseMapMenu();
    }

    public void ToggleMap()
    {
        if (this.gameObject.activeSelf)
        {
            this.gameObject.SetActive(false);
        }
        else
        {
            this.gameObject.SetActive(true);
        }
    }

    public void CloseMapMenu()
    {
        this.gameObject.SetActive(false);
    }
}
