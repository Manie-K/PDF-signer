using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using iText.Kernel.Pdf;
using Microsoft.Win32;
using PDFSignerApp.Helpers;
using System.Management;
using System.Linq.Expressions;

namespace PDFSignerApp
{
    /// <summary>
    /// ViewModel for signing PDF files with a digital signature.
    /// </summary>
    public class PDFSignViewModel : ObservableObject
    {
        private const int PIN_LENGTH = 4;

        /// <summary>
        /// Event triggered when the message color is changed.
        /// </summary>
        public event Action<System.Windows.Media.Brush>? OnMessageColorChanged;

        private readonly CryptographicsHelper _crypto;

        private string _pin = "0000";
        private string _PDFPath = "";
        private string _privateKeyPath = "";
        private string _msg = "";

        /// <summary>
        /// <see cref="RelayCommand"/> for signing the PDF file.
        /// </summary>
        public RelayCommand SignPDFCommand { get; }

        /// <summary>
        /// <see cref="RelayCommand"/> for selecting the PDF file to be signed."/>
        /// </summary>
        public RelayCommand SelectPDFCommand { get; }


        /// <summary>
        /// Represents the PIN used in encryption of private key.
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
        /// Represents the path to the private key file.
        /// </summary>  
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

        /// <summary>
        /// Represents the path to the PDF file to be signed.
        /// </summary>
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

        /// <summary>
        /// Represents the message displayed to the user, indicating the status of pdf signing process or errors.
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
        /// Parameterless constructor for PDFSignViewModel, 
        /// initializes commands and sets up USB watcher for detection of private key on USB.
        /// </summary>
        public PDFSignViewModel()
        {
            _crypto = new CryptographicsHelper();
            SignPDFCommand = new RelayCommand(() => TryToSignPDF(), () => true);
            SelectPDFCommand = new RelayCommand(() => SelectPDFFile(), () => true);
            EnableUSBWatcher();
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

        private void TryToSignPDF()
        {
            if (IsDataValid())
            {
                bool success = _crypto.SignPDF(PDFPath, PrivateKeyPath, Pin, out string message);

                Message = message;
                if (success)
                {
                    OnMessageColorChanged?.Invoke(new SolidColorBrush(Colors.Green));
                }
                else
                {
                    OnMessageColorChanged?.Invoke(new SolidColorBrush(Colors.Red));
                }
            }
        }

        private bool IsDataValid()
        {
            //Pin
            OnMessageColorChanged?.Invoke(new SolidColorBrush(Colors.Red));
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
                Message = "Private key path must be selected";
                return false;
            }

            //PDF directory
            if (String.IsNullOrEmpty(PDFPath))
            {
                Message = "PDF path must be selected";
                return false;
            }

            OnMessageColorChanged?.Invoke(new SolidColorBrush(Colors.Green));
            Message = "";
            return true;
        }

        private void EnableUSBWatcher()
        {
            var insertQuery = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
            var watcher = new ManagementEventWatcher(insertQuery);

            watcher.EventArrived += (sender, e) => CheckAndHandlePrivateKey();

            watcher.Start();

            CheckAndHandlePrivateKey();
        }

        private void CheckAndHandlePrivateKey()
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                string? returnedPath = FindPrivateKeyOnUSB();

                if (returnedPath != null)
                {
                    PrivateKeyPath = returnedPath;
                    OnMessageColorChanged?.Invoke(new SolidColorBrush(Colors.Green));
                    Message = "Private key found";
                }
                else
                {
                    OnMessageColorChanged?.Invoke(new SolidColorBrush(Colors.Red));
                    Message = "Private key not found on USB drive";
                }
            });
        }

        private string? FindPrivateKeyOnUSB()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Removable && drive.IsReady)
                {
                    try
                    {
                        var keyFiles = Directory.GetFiles(drive.RootDirectory.FullName, "*.key", SearchOption.AllDirectories);
                        if (keyFiles.Length > 0)
                        {
                            return keyFiles[0];
                        }
                    }
                    catch (Exception ex)
                    {
                        OnMessageColorChanged?.Invoke(new SolidColorBrush(Colors.Red));
                        Message = $"Error while scanning the drive {drive.Name}: {ex.Message}";
                    }
                }
            }

            return null;
        }
    }
}
