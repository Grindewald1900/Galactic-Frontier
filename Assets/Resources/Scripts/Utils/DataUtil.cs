using UnityEngine;
using System;
using System.Text;
using System.IO;

public static class DataUtil
{
    private static string playerDataPath = Application.persistentDataPath + "/playerData.json";
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
            return new PlayerEntity("Player", 0, 1, 100, CharacterTier.TierA, 1000, new string[] { "Newbie" }, 0, new string[] { "Basic Attack" });
        }
    }


}