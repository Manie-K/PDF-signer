using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Windows.Media;
using KeyGeneratorApp.Helpers;

namespace KeyGeneratorApp
{
    /// <summary>
    /// ViewModel for the main window of the Key Generator application.
    /// </summary>
    internal class MainViewModel : ObservableObject
    {
        private const int PIN_LENGTH = 4;
        private const string keyFileExtension = ".key";

        /// <summary>
        /// Event triggered when the message color is changed.
        /// </summary>
        public event Action<System.Windows.Media.Brush>? OnMessageColorChanged;

        private string _pin = "0000";
        private string _outputPath = "";
        private string _privateKeyFileName = "";
        private string _publicKeyFileName = "";
        private string _msg = "";

        /// <summary>
        /// <see cref="RelayCommand"/> for generating private and public keys.
        /// </summary>
        public RelayCommand GenerateKeysCommand { get; }

        /// <summary>
        /// Represents the PIN used for encrypting the private key.
        /// </summary>
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

        /// <summary>
        /// Represents the directory where the generated keys will be saved.
        /// </summary>
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


        /// <summary>
        /// Represents the file name for the private key.
        /// </summary>
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

        /// <summary>
        /// Represents the file name for the public key.
        /// </summary>
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

        /// <summary>
        /// Represents the message displayed to the user, indicating the status of key generation or errors.
        /// </summary>
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

        /// <summary>
        /// Indicates if the message should be shown to the user
        /// </summary>
        public bool IsMessageValid
        {
            get => _msg.Length > 0;
        }

        /// <summary>
        /// Parameterless constructor for the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            GenerateKeysCommand = new RelayCommand(TryToGenerateKeys, () => true);
        }

        /// <summary>
        /// Shows a dialog for selecting the output directory where the keys will be saved.
        /// </summary>
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
            fileName += keyFileExtension;
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