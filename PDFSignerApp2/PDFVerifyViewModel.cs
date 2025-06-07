using iText.Kernel.Pdf;
using Microsoft.Win32;
using PDFSignerApp.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PDFSignerApp
{
    public class PDFVerifyViewModel : ObservableObject
    {
        public event Action<System.Windows.Media.Brush> OnMessageColorChanged = default;

        private string _PDFPath = "";
        private string _publicKeyPath = "";
        private string _msg = "";

        public RelayCommand VerifyPDFCommand { get; }
        public RelayCommand SelectPDFCommand { get; }
        public RelayCommand SelectPKCommand { get; }

        public string PublicKeyPath
        {
            get => _publicKeyPath;
            set
            {
                if (_publicKeyPath != value)
                {
                    _publicKeyPath = value;
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

        public PDFVerifyViewModel()
        {
            VerifyPDFCommand = new RelayCommand(() => TryToVerifyPDFSignature(), () => true);
            SelectPDFCommand = new RelayCommand(() => SelectPDFFile(), () => true);
            SelectPKCommand = new RelayCommand(() => SelectPKFile(), () => true);
        }

        private void SelectPDFFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PDF files (*.pdf)|*.pdf";
            openFileDialog.Title = "Select a PDF file";

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                PDFPath = selectedFilePath;
            }
        }
        private void SelectPKFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "KEY files (*.key)|*.key";
            openFileDialog.Title = "Select a KEY file";

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                PublicKeyPath = selectedFilePath;
            }
        }


        private void TryToVerifyPDFSignature()
        {
            if (IsDataValid())
            {
                VerifyPDFSignature();
            }
        }

        private bool IsDataValid()
        {
            OnMessageColorChanged?.Invoke(new SolidColorBrush(Colors.Red));
            //Public key path
            if (String.IsNullOrEmpty(PublicKeyPath))
            {
                Message = "Public key path must be selected";
                return false;
            }

            //PDF path
            if (String.IsNullOrEmpty(PDFPath))
            {
                Message = "PDF path must be selected";
                return false;
            }

            OnMessageColorChanged?.Invoke(new SolidColorBrush(Colors.Green));
            Message = "";
            return true;
        }

        private void VerifyPDFSignature()
        {
            try
            {
                OnMessageColorChanged?.Invoke(new SolidColorBrush(Colors.Red));
                byte[] pdfBytes = File.ReadAllBytes(_PDFPath);
                string base64Signature = "woda";

                string originalPath = Path.GetDirectoryName(PDFPath);
                string originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(PDFPath);
                string tempPath = Path.Combine(originalPath, originalFileNameWithoutExtension + "_temp.pdf");

                using (PdfReader reader = new PdfReader(PDFPath))
                using (PdfWriter writer = new PdfWriter(tempPath))
                using (PdfDocument pdfDoc = new PdfDocument(reader, writer))
                {
                    //base64Signature = pdfDoc.GetDocumentInfo().GetMoreInfo("Signature");

                    PdfDocumentInfo info = pdfDoc.GetDocumentInfo();
                    //info.SetMoreInfo("Signature", null);
                }

                byte[] fileBytes = File.ReadAllBytes(tempPath);

                byte[] hash;
                using (SHA256 sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(fileBytes);
                }

                if (string.IsNullOrEmpty(base64Signature))
                {
                    Message = "No signature in PDF file";
                    return;
                }

                byte[] signature = Convert.FromBase64String(base64Signature);
                byte[] publicKeyBytes = File.ReadAllBytes(_publicKeyPath);

                RSA rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

                bool isValid = rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                if (isValid)
                {
                    OnMessageColorChanged?.Invoke(new SolidColorBrush(Colors.Green));
                    Message = "Signature is valid";
                }
                else
                {
                    Message = "Signature is not valid";
                }
            }
            catch (Exception ex)
            {
                Message = $"Error while verifying signature: {ex.Message}";
            }
        }
    }
}
