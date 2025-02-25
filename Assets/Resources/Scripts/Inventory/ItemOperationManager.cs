using UnityEngine;

public class ItemOperationManager : MonoBehaviour
{
    public static ItemOperationManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void TransferItem(ItemEntity item, bool isRemote)
    {
        RemoteItemManager.Instance.UseItem(item);
        ItemManager.Instance.AddItem(item);
    }
}