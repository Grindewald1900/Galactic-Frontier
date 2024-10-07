using UnityEngine;

public static class ImageUtil
{
    private static Sprite defaultSprite = Resources.Load<Sprite>("Images/Default");
    public static Sprite GetSpriteByName(string imagePath, string imageName)
    {
        Sprite sprite = Resources.Load<Sprite>("Images/" + imagePath + imageName);

        if (sprite != null)
        {
            return sprite;
        }
        else
        {
            return defaultSprite;
        }
    }
}