using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Planet;
using Assets.Resources.Scripts.Utils;
using UnityEngine;

namespace Assets.Resources.Scripts.Test
{
    public class TestScript : MonoBehaviour
    {
        public GameObject cloudPrefab;
        public float spawnInterval = 2f;
        public Vector2 spawnAreaMin;
        public Vector2 spawnAreaMax;

        void Start()
        {
            // spawnAreaMin = new Vector2(-15f, -2.5f);
            // spawnAreaMax = new Vector2(0f, -1f);
            // InvokeRepeating("SpawnObject", 0f, spawnInterval);
            // LoadMap(); 
        }

        private void LoadMap()
        {
            List<string> planetNames = new List<string>() { "Planet_A_1", "Planet_A_2", "Planet_A_3", "Planet_A_4", "Planet_A_5", "Planet_A_6" };
            for (int i = 0; i < planetNames.Count; i++)
            {
                _ = ImageUtil.GetSpriteByName("Planets/", planetNames[i]);
                PlanetListManager.Instance.AddItem(new PlanetEntity(planetNames[Random.Range(0, planetNames.Count)], "Lvl 5", planetNames[i], "This is a basic planet"));
            }
        }

        private void SpawnObject()
        {
            Vector2 spawnPosition = new(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y)
            );

            Instantiate(cloudPrefab, spawnPosition, Quaternion.identity);
        }
    }
}