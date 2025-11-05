using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MqttMonitor.Services;

/// <summary>
/// 密码加密/解密服务
/// 使用 AES 对称加密，密钥固定在代码中
/// 适用于本地配置文件的简单加密，可跨机器、跨用户使用
/// </summary>
public class EncryptionService
{
    // 用于标识加密数据的前缀
    private const string EncryptedPrefix = "ENCRYPTED:";

    // 固定的加密密钥 (32字节 = 256位)
    // 注意：这是硬编码的密钥，适用于本地配置文件的简单混淆
    // 不适用于需要高安全性的场景
    private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes("MqttMonitor2025!SecureConfig!!");

    // 固定的IV (16字节)
    private static readonly byte[] EncryptionIV = Encoding.UTF8.GetBytes("MqttMonitor2025!");

    /// <summary>
    /// 加密字符串
    /// </summary>
    /// <param name="plainText">明文</param>
    /// <returns>加密后的Base64字符串，带有ENCRYPTED:前缀</returns>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = EncryptionKey;
                aes.IV = EncryptionIV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock(); // 确保所有数据被写入
                    }

                    byte[] encrypted = ms.ToArray();
                    return EncryptedPrefix + Convert.ToBase64String(encrypted);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("加密失败", ex);
        }
    }

    /// <summary>
    /// 解密字符串
    /// </summary>
    /// <param name="encryptedText">加密的字符串（可能带ENCRYPTED:前缀）</param>
    /// <returns>解密后的明文</returns>
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        try
        {
            // 如果没有加密前缀，说明是明文，直接返回
            if (!encryptedText.StartsWith(EncryptedPrefix))
                return encryptedText;

            // 移除前缀并解析 Base64
            string base64 = encryptedText.Substring(EncryptedPrefix.Length);
            byte[] encryptedBytes = Convert.FromBase64String(base64);

            using (Aes aes = Aes.Create())
            {
                aes.Key = EncryptionKey;
                aes.IV = EncryptionIV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(encryptedBytes))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    using (var resultMs = new MemoryStream())
                    {
                        cs.CopyTo(resultMs);
                        byte[] decryptedBytes = resultMs.ToArray();
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // 如果解密失败，可能是数据损坏
            throw new InvalidOperationException("解密失败，可能是密码数据损坏", ex);
        }
    }

    /// <summary>
    /// 检查字符串是否已加密
    /// </summary>
    /// <param name="text">要检查的字符串</param>
    /// <returns>如果已加密返回true</returns>
    public bool IsEncrypted(string text)
    {
        return !string.IsNullOrEmpty(text) && text.StartsWith(EncryptedPrefix);
    }
}
