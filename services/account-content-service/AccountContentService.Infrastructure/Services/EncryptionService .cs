using AccountContentService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(IConfiguration config)
    {
        _key = Convert.FromBase64String(config["AESEncryption:Key"]);
        _iv = Convert.FromBase64String(config["AESEncryption:IV"]);
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var encryptor = aes.CreateEncryptor();

        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var sw = new StreamWriter(cs);

        sw.Write(plainText);
        sw.Close();

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText)) return cipherText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var decryptor = aes.CreateDecryptor();

            var buffer = Convert.FromBase64String(cipherText);

            using var ms = new MemoryStream(buffer);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
        catch (FormatException)
        {
            // Not a base64 string, might be plaintext
            return cipherText;
        }
        catch (CryptographicException)
        {
            // Invalid encrypted data, might be plaintext or corrupted
            return cipherText;
        }
    }
}