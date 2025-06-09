using Microsoft.Win32;
using PDFSignerApp.Helpers;
using System.Windows.Media;

namespace PDFSignerApp.PDFVerify
{
    /// <summary>
    /// ViewModel for verifying PDF signatures view.
    /// </summary>
    public class PDFVerifyViewModel : ObservableObject
    {
        /// <summary>
        /// Event that is triggered when the message color changes.
        /// </summary>
        public event Action<Brush>? OnMessageColorChanged;

        private readonly CryptographicsHelper _crypto;

        private string _PDFPath = "";
        private string _publicKeyPath = "";
        private string _msg = "";

        /// <summary>
        /// <see cref="RelayCommand"/> for verifying pdf file.
        /// </summary>
        public RelayCommand VerifyPDFCommand { get; }

        /// <summary>
        /// <see cref="RelayCommand"/> for selecting pdf file.
        /// </summary>
        public RelayCommand SelectPDFCommand { get; }

        /// <summary>
        /// <see cref="RelayCommand"/> for selecting public key file.
        /// </summary>
        public RelayCommand SelectPKCommand { get; }


        /// <summary>
        /// Represents the path to the public key file used for verifying the PDF signature.
        /// </summary>
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
        
        /// <summary>
        /// Represents the path to the PDF file that needs to be verified.
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
        /// Represents the message displayed to the user, indicating the status of pdf verification or errors.
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
        /// Initializes a new instance of the <see cref="PDFVerifyViewModel"/> class.
        /// </summary>
        public PDFVerifyViewModel()
        {
            _crypto = new CryptographicsHelper();
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
                bool success = _crypto.VerifyPDFSignature(PDFPath, PublicKeyPath, out string message);
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
            OnMessageColorChanged?.Invoke(new SolidColorBrush(Colors.Red));
            //Public key path
            if (string.IsNullOrEmpty(PublicKeyPath))
            {
                Message = "Public key path must be selected";
                return false;
            }

            //PDF path
            if (string.IsNullOrEmpty(PDFPath))
            {
                Message = "PDF path must be selected";
                return false;
            }

            OnMessageColorChanged?.Invoke(new SolidColorBrush(Colors.Green));
            Message = "";
            return true;
        }
    }
}
