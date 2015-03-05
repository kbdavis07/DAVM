using DAVM.Common;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using System;
using System.IO;

namespace DAVM.ViewModels
{
	public class SettingsViewModel : ViewModelBase
    {
        public event EventHandler CloseRequested;

		public SettingsViewModel()
		{
		}

		#region Properties

		public String PublishSettingsFile
		{
			get { return Properties.Settings.Default.PublishSettingsFile; }
			set
			{
				if (value != Properties.Settings.Default.PublishSettingsFile)
				{
					Properties.Settings.Default.PublishSettingsFile = value;
					RaisePropertyChanged("PublishSettingsFile");
				}
			}
		}

		public String SSHClient
		{
			get { return Properties.Settings.Default.SSHClientPath; }
			set
			{
				if (value != Properties.Settings.Default.SSHClientPath)
				{
					Properties.Settings.Default.SSHClientPath = value;
					RaisePropertyChanged("SSHClient");
				}
			}
		}

		public String SSHClientArguments
		{
			get { return Properties.Settings.Default.SSHClientCmdLine; }
			set
			{
				if (value != Properties.Settings.Default.SSHClientCmdLine)
				{
					Properties.Settings.Default.SSHClientCmdLine = value;
					RaisePropertyChanged("SSHClientArguments");
				}
			}
		}



		#endregion

		#region commands


		private RelayCommand _CmdSelectSSHClient;
		public RelayCommand CmdSelectSSHClient
		{
			get
			{
				if (_CmdSelectSSHClient == null)
				{
					_CmdSelectSSHClient = new RelayCommand(() => DoCmdSelectSSHClient(), () => true);
				}
				return _CmdSelectSSHClient;
			}
		}

		private RelayCommand _CmdSelectPublishSettings;
        public RelayCommand CmdSelectPublishSettings
        {
            get
            {
                if (_CmdSelectPublishSettings == null)
                {
                    _CmdSelectPublishSettings = new RelayCommand(() => DoCmdSelectPublishSettingsFile(), () => true);
                }
                return _CmdSelectPublishSettings;
            }
        }

        private RelayCommand _CmdDownloadPublishSettings;
        public RelayCommand CmdDownloadPublishSettings
        {
            get
            {
                if (_CmdDownloadPublishSettings == null)
                {
                    _CmdDownloadPublishSettings = new RelayCommand(() => DoCmdDownloadPublishSettings(), () => true);
                }
                return _CmdDownloadPublishSettings;
            }
        }

        private RelayCommand _CmdSave;
        public RelayCommand CmdSave
        {
            get
            {
                if (_CmdSave == null)
                {
                    _CmdSave = new RelayCommand(() => DoCmdSave(), () => true);
                }
                return _CmdSave;
            }
        }


        private RelayCommand _CmdCancel;
        public RelayCommand CmdCancel
        {
            get
            {
                if (_CmdCancel == null)
                {
                    _CmdCancel = new RelayCommand(() =>
                    {
                        //if (!IsAzurePowershellInstalled)
                        //    Application.Current.Shutdown(-1);

                        if (CloseRequested != null)
                            CloseRequested(this, null);
                    }, () => true);
                }
                return _CmdCancel;
            }
        }

        #endregion

        #region Do commands


        private void DoCmdSave()
        {
			//SSH client is always saved
			if (!String.IsNullOrEmpty(Properties.Settings.Default.SSHClientCmdLine))
				App.GlobalConfig.SSHClientCmdLine = Properties.Settings.Default.SSHClientCmdLine;

			if (!String.IsNullOrEmpty(Properties.Settings.Default.SSHClientPath))
				App.GlobalConfig.SSHClientPath = new FileInfo(Properties.Settings.Default.SSHClientPath);

			Properties.Settings.Default.Save();

			//save other settings only when are correct

			bool wrongSettings = false;

			if (String.IsNullOrEmpty(Properties.Settings.Default.PublishSettingsFile))
                wrongSettings = true;

			if (!wrongSettings)
            {
                try
				{
					FileInfo newSettingsFile = new FileInfo(Properties.Settings.Default.PublishSettingsFile);
					var initResult = App.GlobalConfig.VMController.InitializeController(newSettingsFile);
					if (initResult)
					{
						if (App.GlobalConfig.VMController.AzureSubscriptions.Count > 0)
						{
							App.GlobalConfig.CurrentSubscription = App.GlobalConfig.VMController.AzureSubscriptions[0];	//always select the first
																														//App.GlobalConfig.CurrentSubscription.RetrieveVMs();
						}

						Properties.Settings.Default.Save();	//settings are good
						wrongSettings = false;
					}
					else
					{
						wrongSettings = true;
					}
				}
				catch (Exception fe)
				{
					Logger.LogEntry("Cannot save settings", fe);
                    wrongSettings = true;
                }
            }

			//if (wrongSettings && NotifyUser != null)
			if (wrongSettings)
                UIHelper.NotifyUser("The publish settings file provided looks not valid. Please download latest version using the \"Download publish settings\" button",false,App.GlobalConfig.SettingsWindow, false);
            else
                CmdCancel.Execute(null);


        }

        private void DoCmdDownloadPublishSettings()
        {
            //the user needs to download the file generated by Azure (the file contains the management certificate)
           // if (NotifyUser != null)
                UIHelper.NotifyUser("I'm going to download the publish settings file (.publishsettings) needed to manage the VMs. The file contains all the information required and a management certificate. Check your browser, authorize the download and copy the file in a secure location. Once downloaded you must set the path of the file in this settings page.", false, App.GlobalConfig.SettingsWindow);

            App.GlobalConfig.VMController.DownloadPublishSettings();
        }

		private void DoCmdSelectSSHClient()
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.CheckFileExists = true;
			ofd.CheckPathExists = true;
			ofd.ValidateNames = true;
			ofd.DefaultExt = ".exe";
			ofd.Filter = "Executable (*.exe)|*.exe";
			ofd.Multiselect = false;
			ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			var result = ofd.ShowDialog();
			if (result.HasValue && result.Value)
			{
				SSHClient = ofd.FileName;

				if (ofd.FileName.Contains("putty.exe"))
					SSHClientArguments = "%FQDN% %PORT%";

			}
		}

		private void DoCmdSelectPublishSettingsFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            ofd.ValidateNames = true;
            ofd.DefaultExt = ".publishsettings";
            ofd.Filter = "Azure Publish Settings (*.publishsettings)|*.publishsettings";
            ofd.Multiselect = false;
            ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var result = ofd.ShowDialog();
            if (result.HasValue && result.Value)
            {
                PublishSettingsFile = ofd.FileName;
            }
        }

        #endregion
    }
}
