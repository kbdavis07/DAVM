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
    public partial class VMControl : UserControl
    {
        public VMControl()
        {       
            InitializeComponent();           
        }

        public static readonly DependencyProperty VMProperty = DependencyProperty.Register("VM", typeof(AzureVM), typeof(VMControl));
        
        public AzureVM VM {
            get { return (AzureVM)GetValue(VMProperty); }
            set { SetValue(VMProperty,value); }
        }
      
        private void StartClick(object sender, RoutedEventArgs e)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			VM.StartAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		}

		private void RemoteConnectionClick(object sender, RoutedEventArgs e)
		{
			UIHelper.LaunchRemoteConnection(VM);
        }


        private void StopClick(object sender, RoutedEventArgs e)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			VM.StopAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		}
    }
}
