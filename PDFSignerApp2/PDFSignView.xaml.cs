using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PDFSignerApp
{
    /// <summary>
    /// Interaction logic for PDFSignView.xaml
    /// </summary>
    public partial class PDFSignView : UserControl
    {
        public PDFSignView()
        {
            InitializeComponent();
            this.DataContextChanged += PDFSignView_DataContextChanged;
            
        }

        private void PDFSignView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(e.NewValue is PDFSignViewModel vm)
            {
                if (vm != null)
                {
                    vm.OnMessageColorChanged += (Brush brush) =>
                    {
                        MsgTextBox.Foreground = brush;
                    };
                }
            }
        }
    }
}
