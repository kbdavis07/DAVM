using DAVM.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using MahApps.Metro;
using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Shell;

namespace DAVM.Views
{

    public partial class MainView : MetroWindow
	{
		MainViewModel Model { get; set; }
        public MainView() 
		{
            InitializeComponent();

            Model = SimpleIoc.Default.GetInstance<MainViewModel>();

			//show settings UI if not well configured
			if (!App.GlobalConfig.IsWellConfigured)
				Model.CmdSettings.Execute(null);

            App.GlobalConfig.MainWindow = this;

            if (App.GlobalConfig.CurrentSubscription != null)
            {
                App.GlobalConfig.CurrentSubscription.Controller.WorkCompleted += Controller_WorkCompleted;
                //refresh on start
                App.GlobalConfig.CurrentSubscription.RetrieveAllAsync();
            }


        }

        private void Controller_WorkCompleted(object sender, EventArgs e)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				if (App.GlobalConfig.CurrentSubscription == null)
					return;

                //display the right layout, when ready
				if (App.GlobalConfig.CurrentSubscription.Resources.Count > 0)
				{
					var g = VisualStateManager.GetVisualStateGroups(mainGrid);					
					VisualStateGroup g1 = (VisualStateGroup)g[0];					
					VisualStateManager.GoToElementState(mainGrid, "DefaultLayout", true);
				}
				else
					VisualStateManager.GoToState(mainGrid, "WelcomeLayout", true);
			});
		}

		private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (App.GlobalConfig.CurrentSubscription != null && !App.GlobalConfig.Controller.IsWorking)
            {
                //automatic refresh if the elapsed time is greater than 1 hour 
                var timeDifference = (DateTime.Now - App.GlobalConfig.CurrentSubscription.LastUpdate);
                if (timeDifference.Hours >= 1)
                    App.GlobalConfig.CurrentSubscription.RetrieveAllAsync();
            }
        }

        private void HandleRequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            string navigateUri = ((Hyperlink)sender).NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }    
    }
}
