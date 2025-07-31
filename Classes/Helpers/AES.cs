using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class AesUtility
{
    // You can make these configurable
    private const int KeySize = 256;
    private const int BlockSize = 128;
    private const int DerivationIterations = 1000;

    public static string Encrypt(string plainText, string password)
    {
        byte[] saltBytes = GenerateRandomBytes(32);
        byte[] ivBytes = GenerateRandomBytes(16);
        byte[] keyBytes = GetKey(password, saltBytes);

        using (var aes = Aes.Create())
        {
            aes.KeySize = KeySize;
            aes.BlockSize = BlockSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using (var encryptor = aes.CreateEncryptor())
            using (var ms = new MemoryStream())
            {
                // prepend salt + IV
                ms.Write(saltBytes, 0, saltBytes.Length);
                ms.Write(ivBytes, 0, ivBytes.Length);

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public static string Decrypt(string encryptedText, string password)
    {
        byte[] fullCipher = Convert.FromBase64String(encryptedText);

        byte[] saltBytes = new byte[32];
        byte[] ivBytes = new byte[16];
        Array.Copy(fullCipher, 0, saltBytes, 0, saltBytes.Length);
        Array.Copy(fullCipher, saltBytes.Length, ivBytes, 0, ivBytes.Length);

        byte[] cipherBytes = new byte[fullCipher.Length - saltBytes.Length - ivBytes.Length];
        Array.Copy(fullCipher, saltBytes.Length + ivBytes.Length, cipherBytes, 0, cipherBytes.Length);

        byte[] keyBytes = GetKey(password, saltBytes);

        using (var aes = Aes.Create())
        {
            aes.KeySize = KeySize;
            aes.BlockSize = BlockSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using (var decryptor = aes.CreateDecryptor())
            using (var ms = new MemoryStream(cipherBytes))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }

    private static byte[] GenerateRandomBytes(int length)
    {
        var randomBytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return randomBytes;
    }

    private static byte[] GetKey(string password, byte[] salt)
    {
        using (var keyDerivation = new Rfc2898DeriveBytes(password, salt, DerivationIterations))
        {
            return keyDerivation.GetBytes(KeySize / 8);
        }
    }
}
