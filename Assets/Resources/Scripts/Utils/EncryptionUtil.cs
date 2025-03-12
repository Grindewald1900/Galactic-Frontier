using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class EncryptionUtil
{
    private static readonly string key = "ddddccccbbbbaaaa"; // 密钥，长度应为16、24或32字符
    private static readonly string iv = "aaaabbbbccccdddd"; // 初始化向量，长度应为16字符

    // 加密并保存文件到指定路径
    public static void EncryptAndSaveFile(string inputFilePath, string outputFilePath)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] ivBytes = Encoding.UTF8.GetBytes(iv);

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using (FileStream inputFileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
            using (FileStream outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
            using (CryptoStream cryptoStream = new CryptoStream(outputFileStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                inputFileStream.CopyTo(cryptoStream);
            }
        }
    }

    // 从Resources加载并解密文件内容
    public static byte[] LoadAndDecryptFile(string resourcePath)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] ivBytes = Encoding.UTF8.GetBytes(iv);

        // 从Resources加载加密文件
        TextAsset encryptedFile = Resources.Load<TextAsset>(resourcePath);
        if (encryptedFile == null)
        {
            Debug.LogError($"Resource at path '{resourcePath}' not found.");
            return null;
        }

        byte[] encryptedData = encryptedFile.bytes;

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using (MemoryStream memoryStream = new MemoryStream(encryptedData))
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (MemoryStream decryptedStream = new MemoryStream())
            {
                cryptoStream.CopyTo(decryptedStream);
                return decryptedStream.ToArray();
            }
        }
    }
}