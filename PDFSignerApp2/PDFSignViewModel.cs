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
    public class PDFSignViewModel : ObservableObject
    {
        private const int PIN_LENGTH = 4;
        public event Action<System.Windows.Media.Brush>? OnMessageColorChanged;

        private readonly CryptographicsHelper _crypto;

        private string _pin = "0000";
        private string _PDFPath = "";
        private string _privateKeyPath = "C:\\Users\\franc\\Desktop\\SEMESTR 6\\BSK\\PDF-signer\\Generated keys\\private_key.key";
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
            _crypto = new CryptographicsHelper();
            SignPDFCommand = new RelayCommand(() => TryToSignPDF(), () => true);
            SelectPDFCommand = new RelayCommand(() => SelectPDFFile(), () => true);
            EnableUSBWatcher();
        }

        public void SelectPDFFile()
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
            watcher.EventArrived += (sender, e) =>
            {
                string? returnedPath = FindPrivateKeyOnUSB();
                if (returnedPath != null)
                {
                    PrivateKeyPath = returnedPath;
                    Message = "Private key found";
                }
            };

            watcher.Start();
        }
        private string? FindPrivateKeyOnUSB()
        {
            OnMessageColorChanged?.Invoke(new SolidColorBrush(Colors.Red));
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Removable && drive.IsReady)
                {
                    try
                    {
                        var keyFiles = Directory.GetFiles(drive.RootDirectory.FullName, "*.key", SearchOption.AllDirectories);
                        if (keyFiles.Length > 0)
                        {
                            OnMessageColorChanged?.Invoke(new SolidColorBrush(Colors.Green));
                            return keyFiles[0];
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Debug.WriteLine($"Error with accesing the drive {drive.Name}: {ex.Message}");
                        Message = $"Access denied to drive {drive.Name}. Please check permissions.";
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error while scanning the drive {drive.Name}: {ex.Message}");
                        Message = $"Error while scanning the drive {drive.Name}. Please try again.";
                    }
                }
            }

            return null;
        }
    }
}
