using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.UI;
using Assets.Resources.Scripts.UI.Nexus;
using Assets.Resources.Scripts.Utils;
using Assets.Scripts.Utils;

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
        private int currentIndex = -1;
        private Button deleteButton;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        void OnDisable()
        {
            LoadSaveDeleteDialog.Close();
        }

        /// <summary>Reloads all player profiles from disk and creates their save-selection slots.</summary>
        public void LoadGame()
        {
            LocalizationUtil.Initialize();
            LoadSaveDeleteDialog.Close();
            RemoveAllItems();
            playerEntities = DataUtil.Instance.LoadPlayerEntities();
            Debug.Log("Loading game... players: " + playerEntities.Count);
            foreach (var player in playerEntities)
            {
                AddItem(player);
            }
            EnsureDeleteButton();
            RefreshDeleteButton();
            InitFocus();
        }

        public void InitFocus()
        {
            if (items.Count > 0)
            {
                SetCurrentFocus(items[0]);
            }
            else
            {
                currentIndex = -1;
                RefreshDeleteButton();
            }
        }

        /// <summary>Updates visual focus and binds the selected save as DataUtil's active player.</summary>
        public void SetCurrentFocus(GameLoadSlot newFocus)
        {
            foreach (var item in items)
            {
                item.SetFocus(item == newFocus);
            }
            currentIndex = newFocus != null ? items.IndexOf(newFocus) : -1;
            if (newFocus?.playerEntity == null)
            {
                RefreshDeleteButton();
                return;
            }

            // Update current player in DataUtil when item selected (also migrates save schema).
            if (!DataUtil.Instance.SetCurrentPlayer(newFocus.playerEntity))
            {
                Debug.LogError(
                    "[SAVE] Cannot use this save: " +
                    (DataUtil.Instance.LastSaveError ?? "unknown migration error"));
            }

            RefreshDeleteButton();
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
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var item = items[i];
                if (item?.playerEntity?.playerID != ID)
                    continue;
                items.RemoveAt(i);
                if (item != null)
                    Destroy(item.gameObject);
            }
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
            currentIndex = -1;
        }

        private void EnsureDeleteButton()
        {
            if (deleteButton != null)
                return;

            deleteButton = NexusUiFactory.CreateButton(
                transform, "Delete Save", UiText.LoadDelete,
                new Vector2(0f, 20f), new Vector2(220f, 44f),
                PromptDeleteSelected,
                NexusTheme.WithAlpha(NexusTheme.Red, 0.18f), NexusTheme.Red, 14f);
            var rect = deleteButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 20f);
        }

        private void RefreshDeleteButton()
        {
            if (deleteButton == null)
                return;
            bool hasSelection = currentIndex >= 0 && currentIndex < items.Count;
            deleteButton.interactable = hasSelection;
        }

        private void PromptDeleteSelected()
        {
            if (currentIndex < 0 || currentIndex >= items.Count)
                return;

            var slot = items[currentIndex];
            if (slot?.playerEntity == null)
                return;

            LoadSaveDeleteDialog.Show(transform, slot.playerEntity, ConfirmDelete);
        }

        private void ConfirmDelete(PlayerEntity player)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.playerID))
                return;

            if (!DataUtil.Instance.TryDeletePlayerSave(player.playerID))
            {
                Debug.LogError("[SAVE] Delete failed: " + (DataUtil.Instance.LastSaveError ?? "unknown error"));
                return;
            }

            LoadGame();
        }
    }
}
