using DAVM.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Windows.Documents;

namespace DAVM.Views
{
    /// <summary>
    /// Interaction logic for AboutView.xaml
    /// </summary>
    public partial class AboutView : MetroWindow
    {
        public AboutView()
        {
            InitializeComponent();
            SimpleIoc.Default.GetInstance<AboutViewModel>().CloseRequested += (s, e) => this.Close();

            if (App.GlobalConfig.MainWindow != null)
                Owner = App.GlobalConfig.MainWindow;
        }

        private void HandleRequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            string navigateUri = ((Hyperlink)sender).NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }
    }
}
