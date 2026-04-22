using System.Security.Cryptography;
using System.Text;

namespace CustomerApi.Infrastructure.Encryption;

public static class EncryptionExtensions
{
    public const string ConfigKey = "Encryption:Key";
    private const int IvLength = 16; // AES block size — IV is always 16 bytes

    public static string Encrypt(this string plaintext, byte[] key)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;

        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);

        return Convert.ToBase64String(result);
    }

    public static string Decrypt(this string ciphertext, byte[] key)
    {
        if (string.IsNullOrEmpty(ciphertext)) return ciphertext;

        var data = Convert.FromBase64String(ciphertext);
        var iv = data[..IvLength];
        var cipherBytes = data[IvLength..];

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plaintextBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plaintextBytes);
    }

    public static string ToSearchHash(this string plaintext, byte[] key)
    {
        if (string.IsNullOrEmpty(plaintext)) return string.Empty;

        using var hmac = new HMACSHA256(key);
        var bytes = Encoding.UTF8.GetBytes(plaintext.ToLowerInvariant());
        return Convert.ToBase64String(hmac.ComputeHash(bytes));
    }

    public static byte[] DeriveKey(string secret)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(secret));
    }
}
