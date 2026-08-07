using UnityEngine;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.UI;
using Assets.Resources.Scripts.Utils;

namespace Assets.Resources.Scripts.Main
{
    /// <summary>
    /// Builds the load-game list from player directories and publishes the selected player to DataUtil.
    /// GameLoadSlot owns row interaction; this manager owns the collection and current focus.
    /// </summary>
    public class GameLoadManager : MonoBehaviour
    {
        public static GameLoadManager Instance;
        public GameObject itemPrefab;
        public Transform contentParent;
        private List<GameLoadSlot> items = new();
        private List<PlayerEntity> playerEntities = new();
        private int currentIndex = 0;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        /// <summary>Reloads all player profiles from disk and creates their save-selection slots.</summary>
        public void LoadGame()
        {
            RemoveAllItems();
            playerEntities = DataUtil.Instance.LoadPlayerEntities();
            Debug.Log("Loading game... players: " + playerEntities.Count);
            foreach (var player in playerEntities)
            {
                AddItem(player);
            }
            InitFocus();
        }

        public void InitFocus()
        {
            if (items.Count > 0)
            {
                SetCurrentFocus(items[0]);
            }
        }

        /// <summary>Updates visual focus and binds the selected save as DataUtil's active player.</summary>
        public void SetCurrentFocus(GameLoadSlot newFocus)
        {
            foreach (var item in items)
            {
                item.SetFocus(item == newFocus);
            }
            currentIndex = items.IndexOf(newFocus);
            // Update current player in DataUtil when item selected
            DataUtil.Instance.SetCurrentPlayer(newFocus.playerEntity);
        }

        public void AddItem(PlayerEntity player)
        {
            GameObject newItem = Instantiate(itemPrefab, contentParent);
            GameLoadSlot gameLoadSlot = newItem.GetComponent<GameLoadSlot>();
            gameLoadSlot.SetPlayer(player);
            items.Add(gameLoadSlot);
        }

        public void RemoveItem(string ID)
        {
            items.RemoveAll(item => item.playerEntity.playerID == ID);
        }

        public void RemoveAllItems()
        {
            foreach (var item in items)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            items.Clear();
            playerEntities.Clear();
        }
    }
}
