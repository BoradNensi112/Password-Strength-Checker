namespace SecurePass.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        bool TryDecrypt(string cipherText, out string plainText);
    }
}
