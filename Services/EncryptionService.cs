using System;
using System.Security.Cryptography;
using System.Text;

namespace MqttMonitor.Services;

/// <summary>
/// 密码加密/解密服务
/// 使用 Windows Data Protection API (DPAPI) 进行加密
/// </summary>
public class EncryptionService
{
    // 用于标识加密数据的前缀
    private const string EncryptedPrefix = "ENCRYPTED:";

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
            // 将字符串转换为字节数组
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            // 使用 DPAPI 加密（仅当前用户可以解密）
            byte[] encryptedBytes = ProtectedData.Protect(
                plainBytes,
                null, // 可选的额外熵
                DataProtectionScope.CurrentUser // 仅当前用户可解密
            );

            // 转换为 Base64 并添加前缀标识
            return EncryptedPrefix + Convert.ToBase64String(encryptedBytes);
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

            // 使用 DPAPI 解密
            byte[] plainBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null, // 使用相同的熵（这里是null）
                DataProtectionScope.CurrentUser
            );

            // 转换回字符串
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            // 如果解密失败，可能是数据损坏或在不同用户/机器上运行
            throw new InvalidOperationException("解密失败，可能是密码数据损坏或在不同的用户账户下运行", ex);
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
