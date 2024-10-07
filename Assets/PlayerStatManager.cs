using UnityEngine;

public class PlayerStatManager : MonoBehaviour
{
    public static PlayerStatManager Instance;
    public GameObject playerStats;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    void Start()
    {
        ShowStats();
    }

    public void ShowStats()
    {
        if (playerStats != null)
        {
            playerStats.SetActive(true);
        }
    }

    public void HideStats()
    {
        if (playerStats != null)
        {
            playerStats.SetActive(false);
        }
    }
}
