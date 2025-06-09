using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using PDFSignerApp.Helpers;

namespace PDFSignerApp
{
    /// <summary>
    /// ViewModel for the main window of the PDF Signer application.
    /// </summary>
    public class MainViewModel : ObservableObject
    {
        private object? _currentViewModel;

        /// <summary>
        /// Represents the viewmodel for the view selected in the main window.
        /// </summary>
        public object? CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        }

        /// <summary>
        /// Command for navigating to PDF signing view in the application.
        /// </summary>
        public ICommand PDFSignViewCommand { get; }

        /// <summary>
        /// Command for navigating to PDF veryfing view in the application.
        /// </summary>
        public ICommand PDFVerifyViewCommand { get; }


        /// <summary>
        /// Parameterless constructor for the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            PDFSignViewCommand = new RelayCommand(() => ShowPDFSignView(), ()=>true);
            PDFVerifyViewCommand = new RelayCommand(() => ShowPDFVerifyView(), () => true);

            ShowPDFSignView();
        }

        private void ShowPDFSignView()
        {
            CurrentViewModel = new PDFSignViewModel();
        }

        private void ShowPDFVerifyView()
        {
            CurrentViewModel = new PDFVerifyViewModel();
        }
    }
}
