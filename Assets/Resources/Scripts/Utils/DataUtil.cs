using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

public static class DataUtil
{
    private static string playerDataPath = Application.persistentDataPath + "/playerData.json";
    private static string playerCardPath = Application.persistentDataPath + "/playerCards.json";
    private static string itemDataPath = Application.persistentDataPath + "/itemData.json";


    public static void SaveItemData(List<ItemEntity> items)
    {
        string json = JsonUtility.ToJson(new ItemListWrapper { items = items }, true);
        string encryptedJson = EncryptBase64(json);
        File.WriteAllText(itemDataPath, encryptedJson);
        Debug.Log("物品数据已保存：" + itemDataPath + "，共" + items.Count + "个物品");
    }

    public static List<ItemEntity> LoadItemData()
    {
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
        string json = JsonUtility.ToJson(new CardListWrapper { cards = cards }, true);
        string encryptedJson = EncryptBase64(json);
        File.WriteAllText(playerCardPath, encryptedJson);
        Debug.Log("卡片数据已保存：" + playerCardPath + "，共" + cards.Count + "张卡片");
    }

    // **读取 List<CardEntity>（Base64 解密）**
    public static List<CardEntity> LoadCardData()
    {
        if (File.Exists(playerCardPath))
        {
            string encryptedJson = File.ReadAllText(playerCardPath);
            string json = DecryptBase64(encryptedJson);
            CardListWrapper wrapper = JsonUtility.FromJson<CardListWrapper>(json);
            Debug.Log("卡片数据已加载：" + playerCardPath + "，共" + wrapper.cards.Count + "张卡片");
            return wrapper.cards;
        }
        else
        {
            Debug.LogWarning("未找到存档，返回默认卡片列表");
            return new List<CardEntity>();
        }
    }
    public static void SavePlayerData(PlayerEntity data)
    {
        string json = JsonUtility.ToJson(data, true);
        string encryptedJson = EncryptBase64(json);
        File.WriteAllText(playerDataPath, encryptedJson);
        Debug.Log("数据已加密并保存：" + playerDataPath);
    }

    public static PlayerEntity LoadPlayerData()
    {
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
}

[Serializable]
public class ItemListWrapper
{
    public List<ItemEntity> items;
}

[Serializable]
public class CardListWrapper
{
    public List<CardEntity> cards;
}