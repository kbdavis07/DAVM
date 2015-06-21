using GalaSoft.MvvmLight;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using System.Diagnostics;
using System;
using DAVM.Common;
using DAVM.Views;
using System.Windows.Shell;
using DAVM.Model;
using System.IO;
using System.Windows;


namespace DAVM.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region Properties

        private InfoMessage _statusMessage;
        public InfoMessage StatusMessage
        {
            get { return _statusMessage; }
            set
            {
                if (value != _statusMessage)
                {
                    _statusMessage = value;
                    RaisePropertyChanged("StatusMessage");
                }
            }
        }

        #endregion

        public MainViewModel()
        {
            Logger.LogUpdated += (s,message) => { StatusMessage = message; };
			//App.GlobalConfig.CurrentSubscription.Controller.WorkStarted += Controller_WorkStarted;
			if(App.GlobalConfig.CurrentSubscription != null)
				App.GlobalConfig.CurrentSubscription.Controller.WorkCompleted += Controller_WorkCompleted; ;
		}

		private void Controller_WorkCompleted(object sender, EventArgs e)
		{
			Application.Current.Dispatcher.Invoke(() =>UIHelper.RegisterJumpList());
        }

		//private void Controller_WorkStarted(object sender, EventArgs e)
		//{
			
		//}

		public void StartAll()
        {
            App.GlobalConfig.CurrentSubscription.StartAll();
        }

        public void StopAll()
        {
            App.GlobalConfig.CurrentSubscription.StopAll();
        }

        public void RefreshAll()
        {
			App.GlobalConfig.CurrentSubscription.RetrieveAllAsync();
        }

		#region Commands

		private RelayCommand _CmdSDP;
		public RelayCommand CmdSDP
		{
			get
			{
				if (_CmdSDP == null)
				{
					_CmdSDP = new RelayCommand(() => Process.Start(new ProcessStartInfo("https://home.diagnostics.support.microsoft.com/SelfHelp?knowledgebaseArticleFilter=2976864")), () => true);
				}
				return _CmdSDP;
			}
		}

		private RelayCommand _CmdAbout;
		public RelayCommand CmdAbout
		{
			get
			{
				if (_CmdAbout == null)
				{
					_CmdAbout = new RelayCommand(() => new AboutView().ShowDialog(), () => true);
				}
				return _CmdAbout;
			}
		}


		private RelayCommand _CmdSettings;
		public RelayCommand CmdSettings
		{
			get
			{
				if (_CmdSettings == null)
				{
					_CmdSettings = new RelayCommand(() => new SettingsView().ShowDialog(), () => true);
				}
				return _CmdSettings;
			}
		}


		private RelayCommand _CmdOpenLog;
		public RelayCommand CmdOpenLog
		{
			get
			{
				if (_CmdOpenLog == null)
				{
					_CmdOpenLog = new RelayCommand(() => Process.Start(App.GlobalConfig.LogFileName.FullName), () => true);
				}
				return _CmdOpenLog;
			}
		}


		private RelayCommand _CmdRefresh;
		public RelayCommand CmdRefresh
		{
			get
			{
				if (_CmdRefresh == null)
				{
					_CmdRefresh = new RelayCommand(() => RefreshAll(), () => true);
				}
				return _CmdRefresh;
			}
		}

		private RelayCommand _CmdStart;
		public RelayCommand CmdStart
		{
			get
			{
				if (_CmdStart == null)
				{
					_CmdStart = new RelayCommand(() => StartAll(), () => true);
				}
				return _CmdStart;
			}
		}

		private RelayCommand _CmdStop;
		public RelayCommand CmdStop
		{
			get
			{
				if (_CmdStop == null)
				{
					_CmdStop = new RelayCommand(() => StopAll(), () => true);
				}
				return _CmdStop;
			}
		}
		#endregion
	}
}