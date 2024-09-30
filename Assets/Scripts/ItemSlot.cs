using UnityEngine;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour
{
    public Image selectedIcon;
    public Image itemImage;

    void Start()
    {
        Image[] images = GetComponentsInChildren<Image>();
        if (images.Length >= 3)
        {
            selectedIcon = images[1];
            itemImage = images[2];
        }
        else
        {
            Debug.Log("Not enough Image components found.");
        }
    }
}