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
    }


    void Update()
    {

    }

    void SpawnObject()
    {
        Vector2 spawnPosition = new Vector2(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y)
        );

        Instantiate(cloudPrefab, spawnPosition, Quaternion.identity);
    }
}
