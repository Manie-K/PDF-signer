﻿using System;
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
using PDFSignerApp.PDFVerify;

namespace PDFSignerApp
{
    /// <summary>
    /// Interaction logic for PDFVerifyView.xaml
    /// </summary>
    public partial class PDFVerifyView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PDFVerifyView"/> class.
        /// </summary>
        public PDFVerifyView()
        {
            InitializeComponent();
            this.DataContextChanged += PDFVerifyView_DataContextChanged;
        }

        private void PDFVerifyView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is PDFVerifyViewModel vm)
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
