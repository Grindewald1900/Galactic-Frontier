using System.IO;
using Assets.Resources.Scripts.Props;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.Utils
{
    public static class ImageUtil
    {
        public static string imageDefaultPath = "Images/";
        public static string statusImagePath = "Images/Status/";
        public static string UIImagePath = "Images/UI/";
        public static string badgeImagePath = "Images/Badges/";
        public static string planetImagePath = "Images/Planets/";
        public static string spellImagePath = "Images/Spells/";
        public static string itemImagePath = "Images/Items/";
        public static string eventImagePath = "Images/Events/";
        public static string characterImagePath = "Images/Cards/Characters/";
        public static string cardBkImagePath = "Images/Cards/Background/";
        public static string debuffImagePath = "Images/Debuffs/";
        public static string effectImagePath = "Prefabs/Effect/";

        private static readonly Sprite defaultSprite = UnityEngine.Resources.Load<Sprite>("Images/Default");
        // Get sprite by name from Resources folder
        public static Sprite GetSpriteByName(string imagePath, string imageName)
        {
            Sprite sprite = UnityEngine.Resources.Load<Sprite>(imagePath + imageName);
            return sprite != null ? sprite : defaultSprite;
        }

        // Get sprite by name from Resources folder
        public static Sprite GetSpriteByName(string imageFullPath)
        {
            Sprite sprite = UnityEngine.Resources.Load<Sprite>(imageFullPath);
            return sprite != null ? sprite : defaultSprite;
        }

        // Save sprite to local file
        public static void SaveSprite(Image targetImage, string savePath)
        {
            if (targetImage.sprite == null)
            {
                Debug.LogError("Sprite is null, can't save it.");
                return;
            }

            Texture2D texture = targetImage.sprite.texture;
            byte[] imageData = texture.EncodeToPNG(); // **转换为 PNG**

            File.WriteAllBytes(savePath, imageData); // **保存到本地**
            Debug.Log($"Avatar stored at {savePath}");
        }

        // Get sprite from local file
        public static void LoadSprite(Image targetImage, string filePath)
        {
            Debug.Log("Loading sprite from: " + filePath);
            if (File.Exists(filePath))
            {
                byte[] imageData = File.ReadAllBytes(filePath);
                RectTransform rectTransform = targetImage.GetComponent<RectTransform>();
                Texture2D texture = new((int)rectTransform.rect.width, (int)rectTransform.rect.height);
                texture.LoadImage(imageData);

                targetImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                Debug.Log("Sprite loaded from local file.");
            }
            else
            {
                Debug.Log("Sprite file not found.");
            }
        }
    }
}