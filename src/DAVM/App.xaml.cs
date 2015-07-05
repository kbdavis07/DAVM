using DAVM.Model;
using DAVM.Common;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace DAVM
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static AppResources GlobalConfig {
            get;
            private set;
        }
       
        protected override void OnStartup(StartupEventArgs e)
        {
            //to change the default font, works in combination with the style
            FrameworkElement.StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata
            {
                DefaultValue = FindResource(typeof(Window))
            });

            this.Exit += App_Exit;

            //Application Insights global config
            TelemetryConfiguration.Active.InstrumentationKey = "4e9fa934-a7e3-4f39-b825-ff0a843ec0f0";
            //to get user and session
            TelemetryConfiguration.Active.ContextInitializers.Add(new UserSessionInitializer());

            GlobalConfig = new AppResources();

            GlobalConfig.Telemetry = new TelemetryClient();
            GlobalConfig.Telemetry.TrackEvent(TelemetryHelper.EVT_START);

            GlobalConfig.AppDirectory = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\DAVM");
            if (!GlobalConfig.AppDirectory.Exists)
                GlobalConfig.AppDirectory.Create();

            GlobalConfig.LogFileName = new FileInfo(GlobalConfig.AppDirectory.FullName + "\\AzureVMDashboardLog.txt");
            //for logging
            TraceListener Tracer = new TextWriterTraceListener(App.GlobalConfig.LogFileName.FullName);
            Tracer.TraceOutputOptions = TraceOptions.DateTime;
            Trace.Listeners.Add(Tracer);
            Trace.AutoFlush = true;

            //loggging start/stop
            Logger.LogEntry(LogType.Verbose, "***** Starting Dashboard-AzureVM *****");
            Logger.LogEntry(LogType.Verbose, String.Format("Version {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            this.Exit += (s, ev) => Logger.LogEntry(LogType.Verbose, "***** Exiting Dashboard-AzureVM *****");

            AppDomain.CurrentDomain.UnhandledException += (s, ue) => Logger.LogEntry("Unhandled Exception", (Exception)ue.ExceptionObject);

            InitApp();

        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            //report telemetry data
            if (GlobalConfig.Telemetry != null)
                GlobalConfig.Telemetry.Flush();
        }

        private void InitApp()
        {
            GlobalConfig.Controller = AzureResourceController.GetInstance(App.GlobalConfig.AppDirectory);
            GlobalConfig.CurrentSubscription = null;

            if (!File.Exists(DAVM.Properties.Settings.Default.PublishSettingsFile))
            {
                GlobalConfig.IsWellConfigured = false;
                Logger.LogEntry(LogType.Warning, "Publish Settings File does not exist");
            }
            else
            {
                var publishSettingsFile = new FileInfo(DAVM.Properties.Settings.Default.PublishSettingsFile);
                GlobalConfig.IsWellConfigured = GlobalConfig.Controller.InitializeController(publishSettingsFile);
                if (GlobalConfig.IsWellConfigured && GlobalConfig.Controller.AzureSubscriptions.Count > 0)
                    GlobalConfig.CurrentSubscription = GlobalConfig.Controller.AzureSubscriptions[0]; //always select the first

            }

			if (!String.IsNullOrEmpty(DAVM.Properties.Settings.Default.SSHClientPath))
			{
				GlobalConfig.SSHClientPath = new FileInfo(DAVM.Properties.Settings.Default.SSHClientPath);
				GlobalConfig.SSHClientCmdLine = DAVM.Properties.Settings.Default.SSHClientCmdLine;

			}
		}
    }
}