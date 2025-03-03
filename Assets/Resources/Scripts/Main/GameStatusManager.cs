using UnityEngine;

public class GameStatusManager : MonoBehaviour
{
    public static GameStatusManager Instance;
    public bool isBattle = false;
    public bool isDrawingCard = false;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}