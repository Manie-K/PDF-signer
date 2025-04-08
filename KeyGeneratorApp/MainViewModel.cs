using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows;

namespace KeyGeneratorApp
{
    internal class MainViewModel : ObservableObject
    {
        private const int PIN_LENGTH = 4;
        
        private string _pin = "0000";
        private string _outputPath = "./";
        private string _privateKeyFileName = "privateKey";
        private string _publicKeyFileName = "publicKey";
        private string _msg = "";

        public RelayCommand GenerateKeysCommand { get; }

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

        public string Message
        {
            get => _msg;
            set
            {
                if (_msg != value)
                {
                    _msg = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool IsMessageValid
        {
            get => _msg.Length > 0;
        }

        public MainViewModel()
        {
            GenerateKeysCommand = new RelayCommand(GenerateKeys, IsDataValid);
        }

        private bool IsDataValid()
        {
            //pin
            if (String.IsNullOrEmpty(Pin))
            {
                _msg = "Pin cannot be empty";
                return false;
            }
            if (Pin.Length != PIN_LENGTH)
            {
                _msg = "Pin must be 4 characters long";
                return false;
            }
            if (!Pin.All(char.IsDigit))
            {
                _msg = "Pin must contain only digits";
                return false;
            }

            //Private key name
            if (String.IsNullOrEmpty(PrivateKeyFileName))
            {
                _msg = "Private key file name cannot be empty";
                return false;
            }

            //Public key name
            if (String.IsNullOrEmpty(PublicKeyFileName))
            {
                _msg = "Public key file name cannot be empty";
                return false;
            }

            _msg = "";
            return true;
        }

        private void GenerateKeys()
        {
            Debug.WriteLine("Key generation started!");
            using (RSA rsa = RSA.Create(4096))
            {
                byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();
                byte[] publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();

                byte[] encryptedPrivateKeyBytes = EncryptPrivateKey(privateKeyBytes);

                string privateKey = Convert.ToBase64String(privateKeyBytes);
                string encryptedPrivateKey = Convert.ToBase64String(encryptedPrivateKeyBytes);
                string publicKey = Convert.ToBase64String(publicKeyBytes);

                Debug.WriteLine($"Private Key: {privateKey}");
                Debug.WriteLine($"Encrypted Private Key: {encryptedPrivateKey}");
                Debug.WriteLine($"Public Key: {publicKey}");

                try
                {
                    SaveToFile(Path.Combine(OutputDirectory, PrivateKeyFileName), encryptedPrivateKeyBytes);
                    SaveToFile(Path.Combine(OutputDirectory, PublicKeyFileName), publicKeyBytes);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error saving keys: {e.Message}");
                }
            }
        }

        private void SaveToFile(string fileName, byte[] content)
        {
            try
            {
                File.WriteAllBytes(fileName, content);
            }
            catch (Exception)
            {
            }
        }

        private byte[] EncryptPrivateKey(byte[] privateKeyBytes)
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