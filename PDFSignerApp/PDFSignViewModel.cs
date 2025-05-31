using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PDFSignerApp
{
    public class PDFSignViewModel : ObservableObject
    {
        private const int PIN_LENGTH = 4;
        private const string keyFileExtension = ".key";
        public event Action<System.Windows.Media.Brush> OnMessageColorChanged = default;

        private string _pin = "0000";
        private string _PDFPath= "";
        private string _privateKeyPath = "";
        private string _msg = "";

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

        public string PrivateKeyDirectory
        {
            get => _privateKeyPath;
            set
            {
                if (_privateKeyPath != value)
                {
                    _privateKeyPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PDFDirectory
        {
            get => _PDFPath;
            set
            {
                if (_PDFPath != value)
                {
                    _PDFPath = value;
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

        public void SelectDirectory()
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = @"C:\";

                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    string selectedFolder = folderDialog.SelectedPath;
                    PrivateKeyDirectory = selectedFolder;
                }
            }
        }

        private void TryToSignPDF()
        {
            if (IsDataValid())
            {
                SignPDF();
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

            //Private key directory
            if (String.IsNullOrEmpty(PrivateKeyDirectory))
            {
                Message = "Private key directory must be selected";
                return false;
            }

            //PDF directory
            if (String.IsNullOrEmpty(PDFDirectory))
            {
                Message = "PDF directory must be selected";
                return false;
            }

            OnMessageColorChanged?.Invoke(new System.Windows.Media.SolidColorBrush(Colors.Green));
            Message = "";
            return true;
        }

        private void SignPDF()
        {
            Debug.WriteLine("Key generation started!");

            byte[] fileBytes = File.ReadAllBytes(PrivateKeyDirectory);
            byte[] salt = fileBytes.Take(16).ToArray();
            byte[] iv = fileBytes.Skip(16).Take(16).ToArray();
            byte[] encryptedPrivateKey = fileBytes.Skip(32).ToArray();

            byte[] decryptedPrivateKeyBytes = DecryptPrivateKey(encryptedPrivateKey);

            Message = "PDF signed successfully!";

            try
            {
                //TODO: save pdf
                //SaveToFile(Path.Combine(PDFDirectory, "SignedPDF"), decryptedPrivateKeyBytes);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error saving keys: {e.Message}");
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

        private byte[] DecryptPrivateKey(byte[] encryptedPrivateKey, byte[] salt, byte[] iv)
        {
            string pin = Pin;
            byte[] decryptedPrivateKey;

            using (Aes aes = Aes.Create())
            {
                var pbkdf2 = new Rfc2898DeriveBytes(pin, salt, 100_000, HashAlgorithmName.SHA256);
                var key = pbkdf2.GetBytes(aes.KeySize / 8);

                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                {
                    decryptedPrivateKey = decryptor.TransformFinalBlock(encryptedPrivateKey, 0, encryptedPrivateKey.Length);
                }
            }

            return decryptedPrivateKey;
        }



    }
}
