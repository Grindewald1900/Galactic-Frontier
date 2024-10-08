using System.Collections.Generic;
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
        List<string> planetNames = new List<string>() { "Planet_A_1", "Planet_A_2", "Planet_A_3", "Planet_A_4", "Planet_A_5", "Planet_A_6" };
        for (int i = 0; i < planetNames.Count; i++)
        {
            Sprite sprite = ImageUtil.GetSpriteByName("Planets/", planetNames[i]);
            MapListManager.Instance.AddItem(new Planet(sprite, "Lvl 5", planetNames[i], "This is a basic planet"));
        }
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
