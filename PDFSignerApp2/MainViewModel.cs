using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using PDFSignerApp.Helpers;

namespace PDFSignerApp
{
    public class MainViewModel : ObservableObject
    {
        private object _currentViewModel; //Should be BaseViewModel, but is small project
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        }

        public ICommand PDFSignViewCommand { get; }
        public ICommand PDFVerifyViewCommand { get; }

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
