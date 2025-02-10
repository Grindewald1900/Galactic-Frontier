using UnityEngine;

public static class ImageUtil
{
    public static string imageDefaultPath = "Images/";
    public static string statusImagePath = "Images/Status/";
    public static string characterImagePath = "Images/Cards/Characters/";
    public static string cardBkImagePath = "Images/Cards/Background/";

    private static Sprite defaultSprite = Resources.Load<Sprite>("Images/Default");
    public static Sprite GetSpriteByName(string imagePath, string imageName)
    {
        Sprite sprite = Resources.Load<Sprite>(imagePath + imageName);
        return sprite != null ? sprite : defaultSprite;
    }
}