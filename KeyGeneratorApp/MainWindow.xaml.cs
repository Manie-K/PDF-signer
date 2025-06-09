using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KeyGeneratorApp
{
    /// <summary>
    /// View for the main window of the Key Generator application.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Generates a new instance of the MainWindow class and initializes the DataContext with MainViewModel.
        /// </summary>
        public MainWindow()
        {
            DataContext = new MainViewModel();
            InitializeComponent();

            var viewModel = (MainViewModel)this.DataContext;
            viewModel.OnMessageColorChanged += (System.Windows.Media.Brush brush) =>
            {
                MsgTextBox.Foreground = brush;
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (MainViewModel)this.DataContext;
            viewModel.SelectDirectory();
        }

    }
}