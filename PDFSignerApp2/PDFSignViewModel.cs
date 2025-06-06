using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Media;

namespace PDFSignerApp
{
    internal class PDFSignViewModel : ObservableObject
    {
        private const int PIN_LENGTH = 4;
        private const string keyFileExtension = ".key";
        public event Action<System.Windows.Media.Brush> OnMessageColorChanged = default;

        private string _pin = "0000";
        private string _PDFPath = "";
        //TODO: delete static directory
        private string _privateKeyPath = "C:\\Users\\franc\\Desktop\\SEMESTR 6\\BSK\\PDF-signer\\Generated keys\\private_key.key";
        private string _msg = "";

        public RelayCommand SignPDFCommand { get; }
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

        public PDFSignViewModel()
        {
            SignPDFCommand = new RelayCommand(TryToSignPDF, () => true);
        }

        public void SelectDirectory()
        {
            /*using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = @"C:\";

                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    string selectedFolder = folderDialog.SelectedPath;
                    PrivateKeyDirectory = selectedFolder;
                }
            }*/
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
            Debug.WriteLine("Signing PDF...");

            byte[] fileBytes = File.ReadAllBytes(PrivateKeyDirectory);
            byte[] salt = fileBytes.Take(16).ToArray();
            byte[] iv = fileBytes.Skip(16).Take(16).ToArray();
            byte[] encryptedPrivateKey = fileBytes.Skip(32).ToArray();

            byte[] decryptedPrivateKeyBytes = DecryptPrivateKey(encryptedPrivateKey, salt, iv);

            try
            {
                byte[] pdfBytes = File.ReadAllBytes(_PDFPath);

                byte[] hash;
                using (SHA256 sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(pdfBytes);
                }

                RSA rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(decryptedPrivateKeyBytes, out _);

                byte[] signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                //using (PdfReader reader = new PdfReader(_PDFPath))
                ////TODO: change write directory
                //using (PdfWriter writer = new PdfWriter(_PDFPath + "\\signedPDF"))
                //using (PdfDocument pdfDoc = new PdfDocument(reader, writer))
                //{
                //    PdfDocumentInfo info = pdfDoc.GetDocumentInfo();
                //    info.SetMoreInfo("Signature", Convert.ToBase64String(signature));
                //}

                Message = "PDF signed successfully!";
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error signing PDF: {e.Message}");
                Message = "Error signing PDF.";
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
