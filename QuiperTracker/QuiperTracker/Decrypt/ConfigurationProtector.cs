using System.Security.Cryptography;
using System.Text;

namespace QuiperTracker.Decrypt
{
    public static class ConfigurationProtector
    {
        private const string ENCRYPTION_KEY_SOURCE = "38B2144BCD2E5F5B7FB2AD2BCB5C7";
        private static readonly byte[] SALT = new byte[] { 0x53, 0x49, 0x53, 0x4c, 0x20, 0x44, 0x49, 0x41, 0x4d, 0x4c, 0x45, 0x52 };

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText)) return cipherText;

            try
            {
                // The logic below ensures that the derivation key and salt are always consistent
                string encryptionKey = ENCRYPTION_KEY_SOURCE;

                // Handle URL-encoded spaces which can occur in Base64
                cipherText = cipherText.Replace(" ", "+");
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(encryptionKey, SALT);
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        return Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                // In a real application, you would log this error and throw an application-stopping exception
                throw new InvalidOperationException("Could not decrypt configuration value.", ex);
            }
        }
    }
}
