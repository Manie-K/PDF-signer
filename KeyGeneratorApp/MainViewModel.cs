using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace KeyGeneratorApp
{
    internal class MainViewModel : ObservableObject
    {
        private string _pin = "0000";
        private string _outputPath = "./";
        private string _privateKeyFileName = "privateKey";
        private string _publicKeyFileName = "publicKey";

        public string Pin
        {
            get => _pin;
            set
            {
                if (_pin != value)
                {
                    _pin = value;
                    OnPropertyChanged(); 
                }
            }
        }

        public string OutputDirectory
        {
            get => _outputPath;
            set
            {
                if (_outputPath != value)
                {
                    _outputPath = value;
                    OnPropertyChanged(); 
                }
            }
        }

        public string PrivateKeyFileName
        {
            get => _privateKeyFileName;
            set
            {
                if (_privateKeyFileName != value)
                {
                    _privateKeyFileName = value;
                    OnPropertyChanged(); 
                }
            }
        }

        public string PublicKeyFileName
        {
            get => _publicKeyFileName;
            set
            {
                if (_publicKeyFileName != value)
                {
                    _publicKeyFileName = value;
                    OnPropertyChanged(); 
                }
            }
        }

        public MainViewModel()
        {

        }

        void GenerateKeys()
        {
            using (RSA rsa = RSA.Create(4096))
            {
                byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();
                byte[] publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();

                byte[] encryptedPrivateKeyBytes = EncryptPrivateKey(privateKeyBytes);

                string privateKey = Convert.ToBase64String(privateKeyBytes);
                string encryptedPrivateKey = Convert.ToBase64String(encryptedPrivateKeyBytes);
                string publicKey = Convert.ToBase64String(publicKeyBytes);

                Console.WriteLine($"Private Key: {privateKey}");
                Console.WriteLine($"Encrypted Private Key: {encryptedPrivateKey}");
                Console.WriteLine($"Public Key: {publicKey}");
            }
        }

        byte[] EncryptPrivateKey(byte[] privateKeyBytes)
        {
            string pin = Pin;
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
                    encryptedPrivateKey = encryptor.TransformFinalBlock(privateKeyBytes, 0, privateKeyBytes.Length);
                }
            }

            return encryptedPrivateKey;
        }
    }
}