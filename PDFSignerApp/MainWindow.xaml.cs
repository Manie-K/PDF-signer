using System.Text;
using System.Windows;

namespace PDFSignerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Parameterless constructor for the MainWindow class. Sets the DataContext to an instance of MainViewModel.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}