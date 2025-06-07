using System.Security.Cryptography;
using PDFSignerApp.Helpers;

namespace TestsProject
{
    [TestClass]
    public sealed class CryptographicsHelperTests
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private CryptographicsHelper _crypto;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        
        

        [TestInitialize]
        public void TestInit()
        {
            _crypto = new CryptographicsHelper();
        }

        [TestCleanup]
        public void TestCleanup()
        {

        }

        [TestMethod]
        public void DecryptPrivateKey_ReturnsCorrectDecryptedKey()
        {
            string pin = "1234";
            byte[] encryptedPrivateKeyFile = EncryptPrivateKeyHelper(pin, out byte[] expectedPrivateKey);

            byte[] salt = encryptedPrivateKeyFile.Take(16).ToArray();
            byte[] iv = encryptedPrivateKeyFile.Skip(16).Take(16).ToArray();
            byte[] encryptedPrivateKey = encryptedPrivateKeyFile.Skip(32).ToArray();

            byte[] decrypted = _crypto.DecryptPrivateKey(encryptedPrivateKey, salt, iv, pin, out string message);

            Assert.AreSame(string.Empty, message, $"Decryption message should be empty on success. Instead was: {message}");

            Assert.AreEqual(expectedPrivateKey.Length, decrypted.Length, 
                "Decrypted private key length does not match expected length.");
            
            for (int i = 0; i < expectedPrivateKey.Length; i++)
            {
                Assert.AreEqual(expectedPrivateKey[i], decrypted[i], $"Byte {i} does not match.");
            }
        }



        private byte[] EncryptPrivateKeyHelper(string pin, out byte[] expectedPrivateKey)
        {
            using (RSA rsa = RSA.Create(4096))
            {
                byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();
                expectedPrivateKey = privateKeyBytes;

                byte[] encryptedPrivateKey;

                byte[] salt = RandomNumberGenerator.GetBytes(16);
                byte[] iv = RandomNumberGenerator.GetBytes(16);

                using (Aes aes = Aes.Create())
                {
                    var pbkdf2 = new Rfc2898DeriveBytes(pin, salt, 100_000, HashAlgorithmName.SHA256);
                    var key = pbkdf2.GetBytes(aes.KeySize / 8);

                    aes.Key = key;
                    aes.IV = iv;

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        byte[] cipherBytes = encryptor.TransformFinalBlock(privateKeyBytes, 0, privateKeyBytes.Length);

                        encryptedPrivateKey = new byte[salt.Length + iv.Length + cipherBytes.Length];
                        Buffer.BlockCopy(salt, 0, encryptedPrivateKey, 0, salt.Length);
                        Buffer.BlockCopy(iv, 0, encryptedPrivateKey, salt.Length, iv.Length);
                        Buffer.BlockCopy(cipherBytes, 0, encryptedPrivateKey, salt.Length + iv.Length, cipherBytes.Length);
                    }
                }

                return encryptedPrivateKey;
            }
        }
    }
}
