using System;
using System.Security.Cryptography;
using System.Text;

namespace CartoonCharacters.Services;

public class HashingService
{
    private readonly string _secretKey = "notepad";

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] iv = RandomNumberGenerator.GetBytes(16);

        using Aes aes = Aes.Create();
        aes.Key = GenerateKey(_secretKey, salt);
        aes.IV = iv;

        using ICryptoTransform encryptor = aes.CreateEncryptor();

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        byte[] encryptedBytes = encryptor.TransformFinalBlock(
            plainBytes,
            0,
            plainBytes.Length
        );

        byte[] result = new byte[salt.Length + iv.Length + encryptedBytes.Length];

        Array.Copy(salt, 0, result, 0, salt.Length);
        Array.Copy(iv, 0, result, salt.Length, iv.Length);
        Array.Copy(encryptedBytes, 0, result, salt.Length + iv.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        byte[] fullBytes = Convert.FromBase64String(encryptedText);

        byte[] salt = new byte[16];
        byte[] iv = new byte[16];
        byte[] encryptedBytes = new byte[fullBytes.Length - salt.Length - iv.Length];

        Array.Copy(fullBytes, 0, salt, 0, salt.Length);
        Array.Copy(fullBytes, salt.Length, iv, 0, iv.Length);
        Array.Copy(fullBytes, salt.Length + iv.Length, encryptedBytes, 0, encryptedBytes.Length);

        using Aes aes = Aes.Create();
        aes.Key = GenerateKey(_secretKey, salt);
        aes.IV = iv;

        using ICryptoTransform decryptor = aes.CreateDecryptor();

        byte[] decryptedBytes = decryptor.TransformFinalBlock(
            encryptedBytes,
            0,
            encryptedBytes.Length
        );

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private static byte[] GenerateKey(string secretKey, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(secretKey),
            salt,
            100000,
            HashAlgorithmName.SHA256,
            32
        );
    }
}