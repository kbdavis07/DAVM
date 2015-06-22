using DAVM.Common;
using DAVM.Model;
using System.Windows;
using System.Windows.Controls;

namespace DAVM.Controls
{
    /// <summary>
    /// Interaction logic for VMDetailControl.xaml
    /// </summary>
    public partial class WebSiteDetailControl : UserControl
    {
        public WebSiteDetailControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public static readonly DependencyProperty WebSiteProperty = DependencyProperty.Register("WebSite", typeof(AzureWebSite), typeof(WebSiteDetailControl), new PropertyMetadata(null,WebSiteChanged));

		public static void WebSiteChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
            WebSiteDetailControl ctrl = obj as WebSiteDetailControl;
			if (e.NewValue != null)
				VisualStateManager.GoToState(ctrl, "DefaultLayout", true);
			else
				VisualStateManager.GoToState(ctrl, "WelcomeLayout", true);
		}


		public AzureWebSite WebSite
		{
			get { return (AzureWebSite)GetValue(WebSiteProperty); }
			set { SetValue(WebSiteProperty, value);}
		}

		private void CopyDetails_Click(object sender, RoutedEventArgs e)
		{
			if (WebSite != null)
			{
				Clipboard.SetText(WebSite.GetVerboseDetails());
			}
		}
		private void RemoteConnectionClick(object sender, RoutedEventArgs e)
		{
			UIHelper.LaunchRemoteConnection(WebSite);
		}
	}
}
