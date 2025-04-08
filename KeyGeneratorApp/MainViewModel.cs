using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Windows.Media;

namespace KeyGeneratorApp
{
    internal class MainViewModel : ObservableObject
    {
        private const int PIN_LENGTH = 4;
        public event Action<System.Windows.Media.Brush> OnMessageColorChanged = default;

        private string _pin = "0000";
        private string _outputPath = "";
        private string _privateKeyFileName = "";
        private string _publicKeyFileName = "";
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
                    OnPropertyChanged(nameof(Message));
                    OnPropertyChanged(nameof(IsMessageValid));
                }
            }
        }
        public bool IsMessageValid
        {
            get => _msg.Length > 0;
        }

        public MainViewModel()
        {
            GenerateKeysCommand = new RelayCommand(TryToGenerateKeys, () => true);
        }

        public void SelectDirectory()
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = @"C:\";

                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    string selectedFolder = folderDialog.SelectedPath;
                    OutputDirectory = selectedFolder;
                }
            }
        }

        
        private void TryToGenerateKeys()
        {
            if(IsDataValid())
            {
                GenerateKeys();
            }
        }

        private bool IsDataValid()
        {
            //Pin
            OnMessageColorChanged?.Invoke(new System.Windows.Media.SolidColorBrush(Colors.Red));
            if (String.IsNullOrEmpty(Pin))
            {
                Message = "Pin cannot be empty";
                return false;
            }
            if (Pin.Length != PIN_LENGTH)
            {
                Message = "Pin must be 4 characters long";
                return false;
            }
            if (!Pin.All(char.IsDigit))
            {
                Message = "Pin must contain only digits";
                return false;
            }

            //Output directory
            if(String.IsNullOrEmpty(OutputDirectory))
            {
                Message = "Output directory must be selected";
                return false;
            }

            //Private key name
            if (String.IsNullOrEmpty(PrivateKeyFileName))
            {
                Message = "Private key file name cannot be empty";
                return false;
            }

            //Public key name
            if (String.IsNullOrEmpty(PublicKeyFileName))
            {
                Message = "Public key file name cannot be empty";
                return false;
            }

            OnMessageColorChanged?.Invoke(new System.Windows.Media.SolidColorBrush(Colors.Green));
            Message = "";
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

                Message = "Keys generated successfully!";

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
            fileName += ".bin";
            try
            {
                File.WriteAllBytes(fileName, content);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving file {fileName}: {ex.Message}");
                Message = $"Error saving file {fileName}: {ex.Message}";
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