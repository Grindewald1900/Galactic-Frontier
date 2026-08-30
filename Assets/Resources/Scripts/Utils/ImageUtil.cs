using System;
using System.IO;
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
        public static string skillImagePath = "Images/Skills/";
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

        public static bool TrySaveSpritePng(Sprite sprite, string savePath)
        {
            if (sprite == null || string.IsNullOrEmpty(savePath))
                return false;

            Texture2D readable = CopyReadable(sprite.texture);
            if (readable == null)
                return false;
            try
            {
                byte[] imageData = readable.EncodeToPNG();
                File.WriteAllBytes(savePath, imageData);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AVATAR] Save failed: " + ex.Message);
                return false;
            }
            finally
            {
                UnityEngine.Object.Destroy(readable);
            }
        }

        public static Sprite LoadSpriteFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;
            try
            {
                byte[] imageData = File.ReadAllBytes(filePath);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(imageData))
                    return null;
                return Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AVATAR] Load failed: " + ex.Message);
                return null;
            }
        }

        private static Texture2D CopyReadable(Texture source)
        {
            if (source == null) return null;
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0);
            Graphics.Blit(source, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
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