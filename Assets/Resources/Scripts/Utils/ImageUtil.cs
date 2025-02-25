using UnityEngine;

public static class ImageUtil
{
    public static string imageDefaultPath = "Images/";
    public static string statusImagePath = "Images/Status/";
    public static string UIImagePath = "Images/UI/";
    public static string badgeImagePath = "Images/Badges/";
    public static string planetImagePath = "Images/Planets/";
    public static string spellImagePath = "Images/Spells/";
    public static string itemImagePath = "Images/Items/";
    public static string characterImagePath = "Images/Cards/Characters/";
    public static string cardBkImagePath = "Images/Cards/Background/";
    public static string debuffImagePath = "Images/Debuffs/";
    public static string effectImagePath = "Prefabs/Effect/";
    private static Sprite defaultSprite = Resources.Load<Sprite>("Images/Default");
    public static Sprite GetSpriteByName(string imagePath, string imageName)
    {
        Sprite sprite = Resources.Load<Sprite>(imagePath + imageName);
        return sprite != null ? sprite : defaultSprite;
    }
}