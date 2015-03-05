using DAVM.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace DAVM.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : MetroWindow
	{
        public SettingsView()
        {
            InitializeComponent();

			App.GlobalConfig.SettingsWindow = this;

			var viewModel = SimpleIoc.Default.GetInstance<SettingsViewModel>();
            viewModel.CloseRequested += (s,e) => this.Close();


            if (App.GlobalConfig.MainWindow != null)
                Owner = App.GlobalConfig.MainWindow;

        }

        private void HandleRequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            string navigateUri = ((Hyperlink)sender).NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }


        protected override void OnClosed(EventArgs e)
        {
           // SimpleIoc.Default.GetInstance<SettingsViewModel>().NotifyUser -= ShowMessage; //do not leak events
            base.OnClosed(e);
        }
    }
}
