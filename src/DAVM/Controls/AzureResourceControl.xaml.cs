using DAVM.Model;
using DAVM.Common;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace DAVM.Controls
{
    /// <summary>
    /// Interaction logic for VMControl.xaml
    /// </summary>
    public partial class AzureResourceControl : UserControl
    {
        public AzureResourceControl()
        {       
            InitializeComponent();           
        }

        public static readonly DependencyProperty VMProperty = DependencyProperty.Register("Resource", typeof(AzureResource), typeof(AzureResourceControl));
        
        public AzureResource Resource {
            get { return (AzureResource)GetValue(VMProperty); }
            set { SetValue(VMProperty,value); }
        }
    }
}
