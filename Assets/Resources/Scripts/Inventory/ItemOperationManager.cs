using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using Assets.Resources.Scripts.Entity;

namespace Assets.Resources.Scripts.Inventory
{
    public class ItemOperationManager : MonoBehaviour
    {
        public static ItemOperationManager Instance;
        public TextMeshProUGUI progressText;
        public Button transferButton;
        public Button removeButton;
        public ItemEntity selectedItem;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            SetButtonInteractable(false, false);
        }

        void Start()
        {
            Init();
        }

        private void Init()
        {
            transferButton.onClick.AddListener(() => TransferItem(teleportTime: Random.Range(1f, 5f)));
            removeButton.onClick.AddListener(() => RemoveItem());
        }

        public void SetButtonInteractable(bool isTransferable, bool isRemovable)
        {
            transferButton.interactable = isTransferable;
            removeButton.interactable = isRemovable;
        }

        public void TransferItem(bool isRemote = true, float teleportTime = 3f)
        {
            StartCoroutine(TransferAfterDelay(teleportTime));
        }

        public void RemoveItem()
        {
            // Remove the item from the inventory
            if (selectedItem.isRemote)
                RemoteItemManager.Instance.UseItem(selectedItem, selectedItem.quantity);
            else
                ItemManager.Instance.UseItem(selectedItem, selectedItem.quantity);
        }

        private IEnumerator TransferAfterDelay(float teleportTime)
        {
            yield return StartCoroutine(UpdateTeleportProgress(teleportTime));
            Debug.Log($"传送完成！将 {selectedItem.itemName} 添加到玩家背包");
            ItemManager.Instance.AddItem(selectedItem);
            RemoteItemManager.Instance.UseItem(selectedItem, selectedItem.quantity);
        }

        IEnumerator UpdateTeleportProgress(float teleportTime)
        {
            float elapsedTime = 0f;

            while (elapsedTime < teleportTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / teleportTime) * 100f;
                progressText.text = $"{progress:F0}%";
                yield return null;
            }

            progressText.text = "Done";
        }
    }
}