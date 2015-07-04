using DAVM.Common;
using DAVM.Model;
using System.Windows;
using System.Windows.Controls;

namespace DAVM.Controls
{
    public partial class ResourceControl : UserControl
    {
        public ResourceControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty VMProperty = DependencyProperty.Register("Resource", typeof(AzureResource), typeof(ResourceControl));

        public AzureResource Resource
        {
            get { return (AzureResource)GetValue(VMProperty); }
            set { SetValue(VMProperty, value); }
        }

        private void StartClick(object sender, RoutedEventArgs e)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Resource.StartAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private void RemoteConnectionClick(object sender, RoutedEventArgs e)
        {
            UIHelper.LaunchRemoteConnection(Resource);
        }


        private void StopClick(object sender, RoutedEventArgs e)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Resource.StopAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private void CopyDetails_Click(object sender, RoutedEventArgs e)
        {
            if (Resource != null)
            {
                Clipboard.SetText(Resource.GetVerboseDetails());
            }
        }
    }
}
