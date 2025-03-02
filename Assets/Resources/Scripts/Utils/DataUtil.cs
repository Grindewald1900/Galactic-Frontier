using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Cards;

namespace Assets.Resources.Scripts.Utils
{
    public static class DataUtil
    {
        private static readonly string dataPath = Application.persistentDataPath + "/data";
        private static readonly string playerDataPath = dataPath + "/playerData.json";
        private static readonly string playerCardPath = dataPath + "/playerCards.json";
        private static readonly string itemDataPath = dataPath + "/itemData.json";
        public static readonly string playerAvatarPath = dataPath + "/avatar.png";

        public static void SaveItemData(List<ItemEntity> items)
        {
            CheckIfPathExist(dataPath);
            string json = JsonUtility.ToJson(new ItemListWrapper { items = items }, true);
            string encryptedJson = EncryptBase64(json);
            File.WriteAllText(itemDataPath, encryptedJson);
            Debug.Log("物品数据已保存：" + itemDataPath + "，共" + items.Count + "个物品");
        }

        public static List<ItemEntity> LoadItemData()
        {
            CheckIfPathExist(dataPath);

            if (File.Exists(itemDataPath))
            {
                string encryptedJson = File.ReadAllText(itemDataPath);
                string json = DecryptBase64(encryptedJson);
                ItemListWrapper wrapper = JsonUtility.FromJson<ItemListWrapper>(json);
                Debug.Log("物品数据已加载：" + itemDataPath + "，共" + wrapper.items.Count + "个物品");
                return wrapper.items;
            }
            else
            {
                Debug.LogWarning("未找到存档文件，返回默认物品列表");
                return new List<ItemEntity>();
            }
        }

        public static void SaveCardData(List<CardEntity> cards)
        {
            CheckIfPathExist(dataPath);
            string json = JsonUtility.ToJson(new CardListWrapper { cardEntities = cards }, true);
            string encryptedJson = EncryptBase64(json);
            File.WriteAllText(playerCardPath, encryptedJson);
            Debug.Log("卡片数据已保存：" + playerCardPath + "，共" + cards.Count + "张卡片");
        }

        // **读取 List<CardEntity>（Base64 解密）**
        public static List<CardEntity> LoadCardData()
        {
            CheckIfPathExist(dataPath);
            if (File.Exists(playerCardPath))
            {
                string encryptedJson = File.ReadAllText(playerCardPath);
                string json = DecryptBase64(encryptedJson);
                CardListWrapper wrapper = JsonUtility.FromJson<CardListWrapper>(json);
                Debug.Log("卡片数据已加载：" + playerCardPath + "，共" + wrapper.cardEntities.Count + "张卡片");
                return wrapper.cardEntities;
            }
            else
            {
                Debug.LogWarning("未找到存档，返回默认卡片列表");
                return new List<CardEntity>();
            }
        }

        public static void SavePlayerData(PlayerEntity data)
        {
            CheckIfPathExist(dataPath);
            string json = JsonUtility.ToJson(data, true);
            string encryptedJson = EncryptBase64(json);
            File.WriteAllText(playerDataPath, encryptedJson);
            Debug.Log("数据已加密并保存：" + playerDataPath);
        }

        public static PlayerEntity LoadPlayerData()
        {
            CheckIfPathExist(dataPath);
            if (File.Exists(playerDataPath))
            {
                string encryptedJson = File.ReadAllText(playerDataPath);
                string json = DecryptBase64(encryptedJson);
                return JsonUtility.FromJson<PlayerEntity>(json);
            }
            else
            {
                Debug.LogWarning("未找到存档文件，返回默认数据");
                return new PlayerEntity("Player", "00000", 1, 100, CharacterTier.TierA, 1000, new string[] { "Newbie" }, 0, new string[] { "Basic Attack" });
            }
        }

        public static string EncryptBase64(string plainText)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(bytes);
        }

        public static string DecryptBase64(string encryptedText)
        {
            byte[] bytes = Convert.FromBase64String(encryptedText);
            return Encoding.UTF8.GetString(bytes);
        }

        public static void CheckIfPathExist(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) // **如果文件夹不存在**
            {
                Directory.CreateDirectory(directoryPath); // **创建文件夹**
                Debug.Log($"Path created: {directoryPath}");
            }
        }

        [Serializable]
        public class ItemListWrapper
        {
            public List<ItemEntity> items;
        }

        [Serializable]
        public class CardListWrapper
        {
            public List<CardEntity> cardEntities;
        }
    }
}