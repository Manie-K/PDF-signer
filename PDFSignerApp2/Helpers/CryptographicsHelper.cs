using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using System.Windows.Media;
using System.IO;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace PDFSignerApp.Helpers
{
    public class CryptographicsHelper
    {
        public CryptographicsHelper()
        {

        }

        public bool SignPDF(string pdfPath, string privateKeyPath, string pin, out string message)
        {
            message = "Signing PDF...";

            byte[] fileBytes = File.ReadAllBytes(privateKeyPath);
            byte[] salt = fileBytes.Take(16).ToArray();
            byte[] iv = fileBytes.Skip(16).Take(16).ToArray();
            byte[] encryptedPrivateKey = fileBytes.Skip(32).ToArray();

            byte[] decryptedPrivateKeyBytes = DecryptPrivateKey(encryptedPrivateKey, salt, iv, pin, out string msg);
            if(msg != string.Empty)
            {
                message = msg;
            }
            if (decryptedPrivateKeyBytes.Length == 0)
            {
                message = "Error decrypting private key.";
                return false;
            }

            try
            {
                string? originalPath = Path.GetDirectoryName(pdfPath);
                if(originalPath == null)
                {
                    message = "Error: Original path is null.";
                    return false;
                }

                string originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(pdfPath);
                string tempPath = Path.Combine(originalPath, originalFileNameWithoutExtension + "_temp.pdf");

                using (PdfReader reader = new PdfReader(pdfPath))
                using (PdfWriter writer = new PdfWriter(tempPath))
                using (PdfDocument pdfDoc = new PdfDocument(reader, writer))
                {}

                byte[] pdfBytes = File.ReadAllBytes(tempPath);

                byte[] hash;
                using (SHA256 sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(pdfBytes);
                }

                RSA rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(decryptedPrivateKeyBytes, out _);

                byte[] signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(pdfPath);
                string signedPath = Path.Combine(originalPath, originalFileNameWithoutExtension + "_signed.pdf");

                using (PdfReader reader = new PdfReader(tempPath))
                using (PdfWriter writer = new PdfWriter(signedPath))
                using (PdfDocument pdfDoc = new PdfDocument(reader, writer))
                {
                    PdfDocumentInfo info = pdfDoc.GetDocumentInfo();
                    info.SetMoreInfo("Signature", Convert.ToBase64String(signature));
                }

                message = "PDF signed successfully!";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Error signing PDF. {ex.Message}";
                return false;
            }
        }

        public bool VerifyPDFSignature(string pdfPath, string publicKeyPath, out string message)
        {
            try
            {
                string base64Signature;

                string? originalPath = Path.GetDirectoryName(pdfPath);
                if (originalPath == null)
                {
                    message = "Error: Original path is null.";
                    return false;
                }

                string originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(pdfPath);
                string tempPath = Path.Combine(originalPath, originalFileNameWithoutExtension + "_temp.pdf");

                using (PdfReader reader = new PdfReader(pdfPath))
                using (PdfWriter writer = new PdfWriter(tempPath))
                using (PdfDocument pdfDoc = new PdfDocument(reader, writer))
                {
                    base64Signature = pdfDoc.GetDocumentInfo().GetMoreInfo("Signature");

                    PdfDocumentInfo info = pdfDoc.GetDocumentInfo();
                    PdfDictionary catalog = pdfDoc.GetCatalog().GetPdfObject();
                    catalog.Remove(new PdfName("Signature"));
                    pdfDoc.Close();
                }

                byte[] fileBytes = File.ReadAllBytes(tempPath);

                byte[] hash;
                using (SHA256 sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(fileBytes);
                }

                if (string.IsNullOrEmpty(base64Signature))
                {
                    message = "No signature in PDF file";
                    return false;
                }

                byte[] signature = Convert.FromBase64String(base64Signature);
                byte[] publicKeyBytes = File.ReadAllBytes(publicKeyPath);

                RSA rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

                bool isValid = rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                if (isValid)
                {
                    message = "Signature is valid";
                    return true;
                }
                else
                {
                    message = "Signature is not valid";
                    return false;
                }
            }
            catch (Exception ex)
            {
                message = $"Error while verifying signature: {ex.Message}";
                return false;
            }
        }

        internal byte[] DecryptPrivateKey(byte[] encryptedPrivateKey, byte[] salt, byte[] iv, string pin, out string message)
        {
            byte[] decryptedPrivateKey = [];
            message = string.Empty;

            try
            {
                using Aes aes = Aes.Create();

                var pbkdf2 = new Rfc2898DeriveBytes(pin, salt, 100_000, HashAlgorithmName.SHA256);
                var key = pbkdf2.GetBytes(aes.KeySize / 8);
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                {
                    decryptedPrivateKey = decryptor.TransformFinalBlock(encryptedPrivateKey, 0, encryptedPrivateKey.Length);
                }
            }
            catch (Exception ex)
            {
                message = $"Error decrypting PDF: {ex.Message}";
            }

            return decryptedPrivateKey;
        }
    }
}
