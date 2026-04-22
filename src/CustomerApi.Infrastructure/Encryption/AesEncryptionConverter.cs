using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CustomerApi.Infrastructure.Encryption;

public sealed class AesEncryptionConverter : ValueConverter<string, string>
{
    public AesEncryptionConverter(string encryptionKey)
        : this(EncryptionExtensions.DeriveKey(encryptionKey)) { }

    private AesEncryptionConverter(byte[] key)
        : base(
            plaintext  => plaintext.Encrypt(key),
            ciphertext => ciphertext.Decrypt(key))
    { }
}
