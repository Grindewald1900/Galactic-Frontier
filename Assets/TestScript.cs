using UnityEngine;

public class TestScript : MonoBehaviour
{

    public GameObject cloudPrefab;
    public float spawnInterval = 2f;
    public Vector2 spawnAreaMin;
    public Vector2 spawnAreaMax;

    void Start()
    {
        spawnAreaMin = new Vector2(-15f, -2.5f);
        spawnAreaMax = new Vector2(0f, -1f);
        InvokeRepeating("SpawnObject", 0f, spawnInterval);
        LoadMap();
    }

    private void LoadMap()
    {
        Sprite sprite = ImageUtil.GetSpriteByName("Planets/", "Planet_A_1");
        MapListManager.Instance.AddItem(new Planet(sprite, "Lvl 5", "Planet_A_1", "Info"));
        MapListManager.Instance.AddItem(new Planet(sprite, "Lvl 6", "Planet_A_2", "Info"));
        MapListManager.Instance.AddItem(new Planet(sprite, "Lvl 7", "Planet_A_3", "Info"));
        MapListManager.Instance.AddItem(new Planet(sprite, "Lvl 7", "Planet_A_4", "Info"));
        MapListManager.Instance.AddItem(new Planet(sprite, "Lvl 7", "Planet_A_5", "Info"));
        MapListManager.Instance.AddItem(new Planet(sprite, "Lvl 7", "Planet_A_6", "Info"));

    }

    private void SpawnObject()
    {
        Vector2 spawnPosition = new Vector2(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y)
        );

        Instantiate(cloudPrefab, spawnPosition, Quaternion.identity);
    }
}
