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
    public partial class MainWindow : Window
    {
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