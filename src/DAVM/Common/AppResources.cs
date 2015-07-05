using DAVM.Model;
using GalaSoft.MvvmLight;
using MahApps.Metro.Controls;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DAVM.Common
{
    public class AppResources : ObservableObject
    {
        public TelemetryClient Telemetry { get; set; }

        public FileInfo LogFileName { get; set; }

		#region SSH
		public FileInfo SSHClientPath { get; set; }
		public String SSHClientCmdLine { get; set; }

		//placeholders for command line
		public static String PH_FQDN = "%FQDN%";
		public static String PH_PORT = "%PORT%";
		#endregion

		private bool _isWellConfigured = false;
		public bool IsWellConfigured {
            get {
                return _isWellConfigured; } set {
                _isWellConfigured = value;
                RaisePropertyChanged("IsWellConfigured"); } }

        private AzureSubscription _currentSubscription;
        public AzureSubscription CurrentSubscription
        {
            get { return _currentSubscription; }
            set
            {
                if (value != _currentSubscription)
                {
                    _currentSubscription = value;
                    RaisePropertyChanged("CurrentSubscription");
                }
            }
        }
        public DirectoryInfo AppDirectory { get; set; }

        public AzureResourceController Controller { get; set; }

		#region Views
		public MetroWindow MainWindow { get; set; }
		public MetroWindow SettingsWindow { get; set; }
		#endregion


    }
}
