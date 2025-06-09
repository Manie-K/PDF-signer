using System.Security.Cryptography;
using iText.Kernel.Pdf;
using System.IO;
using iText.Signatures;

namespace PDFSignerApp.Helpers
{
    public class CryptographicsHelper
    {
        const int SignatureBytesCount = 512;
        public CryptographicsHelper()
        {

        }

        public bool SignPDF(string pdfPath, string privateKeyPath, string pin, out string message)
        {
            message = "Signing PDF...";

            // 1. Decrypt private key
            // 2. Read PDF file
            // 3. Compute SHA256 hash of the PDF
            // 4. Sign the hash with the private key
            // 5. Write the signature to the PDF
            // 6. Save the signed PDF as new file


            // 1. Decrypt private key

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
                // 2. Read PDF file

                string? originalPath = Path.GetDirectoryName(pdfPath);
                if(originalPath == null)
                {
                    message = "Error: Original path is null.";
                    return false;
                }

                byte[] pdfBytes = File.ReadAllBytes(originalPath);


                // 3. Compute SHA256 hash of the PDF

                byte[] hash;
                using (SHA256 sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(pdfBytes);
                }


                // 4. Sign the hash with the private key

                RSA rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(decryptedPrivateKeyBytes, out _);

                byte[] signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);


                // 5. Write the signature to the PDF    &&    6. Save the signed PDF as new file

                string originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(pdfPath);
                string signedPath = Path.Combine(originalPath, originalFileNameWithoutExtension + "_signed.pdf");

                byte[] combinedBytes = new byte[pdfBytes.Length + signature.Length];
                Buffer.BlockCopy(pdfBytes, 0, combinedBytes, 0, pdfBytes.Length);
                Buffer.BlockCopy(signature, 0, combinedBytes, pdfBytes.Length, signature.Length);
                
                File.WriteAllBytes(signedPath, combinedBytes);

                message = "PDF signed successfully!";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Error signing PDF. {ex.Message}";
                return false;
            }
        }

        public bool VerifyPDFSignature(string signedPDFPath, string publicKeyPath, out string message)
        {
            // 1. Read the signed PDF file
            // 2. Calculate the length of the byte areas
            // 3. Extract the signature from the PDF
            // 4. Extract the unsigned PDF content
            // 5. Compute SHA256 hash of the unsigned PDF content
            // 6. Verify the signature using the public key and the hash
            // 7. Return the verification result

            try
            {
                // 1. Read the signed PDF file

                byte[] pdfBytes = File.ReadAllBytes(signedPDFPath);


                // 2. Calculate the length of the byte areas

                int signatureStartIndex = pdfBytes.Length - SignatureBytesCount;
                int pdfContentSize = pdfBytes.Length - SignatureBytesCount;
                if (signatureStartIndex < 0)
                {
                    message = "Invalid signed PDF file.";
                    return false;
                }


                // 3. Extract the signature from the PDF


                byte[] signatureBytes = new byte[SignatureBytesCount];
                Array.Copy(pdfBytes, signatureStartIndex, signatureBytes, 0, SignatureBytesCount);

                // 4. Extract the unsigned PDF content

                byte[] pdfContent = new byte[pdfContentSize];
                Array.Copy(pdfBytes, 0, pdfContent, 0, pdfContentSize);


                // 5. Compute SHA256 hash of the unsigned PDF content

                byte[] hash;
                using (SHA256 sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(pdfContent);
                }


                // 6. Verify the signature using the public key and the hash

                byte[] publicKeyBytes = File.ReadAllBytes(publicKeyPath);
                RSA rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

                bool isValid = rsa.VerifyHash(hash, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);


                // 7. Return the verification result

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
