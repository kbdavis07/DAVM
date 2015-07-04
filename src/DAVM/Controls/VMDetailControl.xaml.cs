using DAVM.Common;
using DAVM.Model;
using System.Windows;
using System.Windows.Controls;

namespace DAVM.Controls
{
    /// <summary>
    /// Interaction logic for VMDetailControl.xaml
    /// </summary>
    public partial class VMDetailControl : UserControl
    {
        public VMDetailControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public static readonly DependencyProperty VMProperty = DependencyProperty.Register("VM", typeof(AzureVM), typeof(VMDetailControl), new PropertyMetadata(null,VMChanged));

		public static void VMChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			VMDetailControl ctrl = obj as VMDetailControl;
			if (e.NewValue != null)
				VisualStateManager.GoToState(ctrl, "DefaultLayout", true);
			else
				VisualStateManager.GoToState(ctrl, "WelcomeLayout", true);
		}



		public AzureVM VM
		{
			get { return (AzureVM)GetValue(VMProperty); }
			set { SetValue(VMProperty, value);}
		}

		private void RemoteConnectionClick(object sender, RoutedEventArgs e)
		{
			UIHelper.LaunchRemoteConnection(VM);
		}
	}
}
