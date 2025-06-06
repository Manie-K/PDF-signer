using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using iText.Kernel.Pdf;
using Microsoft.Win32;
using PDFSignerApp2.Helpers;

namespace PDFSignerApp
{
    internal class PDFSignViewModel : ObservableObject
    {
        private const int PIN_LENGTH = 4;
        public event Action<System.Windows.Media.Brush> OnMessageColorChanged = default;

        private string _pin = "0000";
        private string _PDFPath = "";
        private string _privateKeyPath = "";
        private string _msg = "";

        public RelayCommand SignPDFCommand { get; }
        public RelayCommand SelectPDFCommand { get; }
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

        public string PrivateKeyPath
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

        public string PDFPath
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
            SignPDFCommand = new RelayCommand(() => TryToSignPDF(), () => true);
            SelectPDFCommand = new RelayCommand(() => SelectPDFFile(), () => true);
        }

        public void SelectPDFFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PDF files (*.pdf)|*.pdf";
            openFileDialog.Title = "Select a PDF file";

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                Debug.Write(Message = $"Selected PDF file: {selectedFilePath}");
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
            if (String.IsNullOrEmpty(PrivateKeyPath))
            {
                Message = "Private key directory must be selected";
                return false;
            }

            //PDF directory
            if (String.IsNullOrEmpty(PDFPath))
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

            byte[] fileBytes = File.ReadAllBytes(PrivateKeyPath);
            byte[] salt = fileBytes.Take(16).ToArray();
            byte[] iv = fileBytes.Skip(16).Take(16).ToArray();
            byte[] encryptedPrivateKey = fileBytes.Skip(32).ToArray();

            byte[] decryptedPrivateKeyBytes = DecryptPrivateKey(encryptedPrivateKey, salt, iv);

            try
            {
                byte[] pdfBytes = File.ReadAllBytes(PDFPath);

                byte[] hash;
                using (SHA256 sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(pdfBytes);
                }

                RSA rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(decryptedPrivateKeyBytes, out _);

                byte[] signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                using (PdfReader reader = new PdfReader(PDFPath))
                //TODO: change write directory
                using (PdfWriter writer = new PdfWriter(PDFPath + "\\signedPDF"))
                using (PdfDocument pdfDoc = new PdfDocument(reader, writer))
                {
                    PdfDocumentInfo info = pdfDoc.GetDocumentInfo();
                    info.SetMoreInfo("Signature", Convert.ToBase64String(signature));
                }

                Message = "PDF signed successfully!";
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error signing PDF: {e.Message}");
                Message = "Error signing PDF.";
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
