using DAVM.Model;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;

namespace DAVM.Common
{
	public class UIHelper
	{
		private static Cursor currentCur = Mouse.OverrideCursor;

		public static void SetWaitCursor()
		{

			Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
		}

		public static void SetNormalCursor()
		{
			Mouse.OverrideCursor = currentCur;
		}

		public static void LaunchRemoteConnection(AzureVM vm)
		{
			try
			{
				if (vm.RemoteConnectionType == RemoteConnectionType.RDP)
				{
					Process mstsc = new Process();
					mstsc.StartInfo.FileName = "mstsc.exe";
					mstsc.StartInfo.Arguments = String.Format("/v:{0}:{1}", vm.FQDN, vm.RemoteConnectionPort);
					mstsc.Start();
				}
				else
				{
					if (App.GlobalConfig.SSHClientPath == null || !App.GlobalConfig.SSHClientPath.Exists || String.IsNullOrEmpty(App.GlobalConfig.SSHClientCmdLine))
					{
						UIHelper.NotifyUser("Could not start your SSH client, please check the configuration", false,App.GlobalConfig.MainWindow,true);
						return;
					}

					Process mstsc = new Process();
					mstsc.StartInfo.FileName = App.GlobalConfig.SSHClientPath.FullName;
					App.GlobalConfig.SSHClientCmdLine = App.GlobalConfig.SSHClientCmdLine.Replace(AppResources.PH_FQDN, vm.FQDN);
					App.GlobalConfig.SSHClientCmdLine = App.GlobalConfig.SSHClientCmdLine.Replace(AppResources.PH_PORT, "" + vm.RemoteConnectionPort);
					mstsc.StartInfo.Arguments = App.GlobalConfig.SSHClientCmdLine;
					mstsc.Start();
				}
			}
			catch (Exception ex)
			{
				Logger.LogEntry("Could not launch the remote connection", ex);
				UIHelper.NotifyUser("Could not start the remote connection: " + ex.Message, false, App.GlobalConfig.MainWindow, true);

			}
		}

		/// <summary>
		/// Add an RDP/SSH connection link to the TaskBar JumpList
		/// </summary>
		public static void RegisterJumpList()
		{
			System.Windows.Application.Current.Dispatcher.Invoke(() =>
			{
				if (App.GlobalConfig.CurrentSubscription == null || !App.GlobalConfig.CurrentSubscription.Controller.ControllerInitialized)
					return;

				JumpList jumpList = new JumpList();
				jumpList.ShowRecentCategory = true;
				jumpList.JumpItems.Add(new JumpTask());

				//the jump list will contains a maximum of 5 VMs - selecting only the ones that are running on the last refresh
				foreach (AzureVM vm in App.GlobalConfig.CurrentSubscription.VMs.Where((v) => v.Status == VMStatus.Running).Take(5))
				{
					JumpTask t = new JumpTask();
					if (vm.RemoteConnectionType == RemoteConnectionType.RDP)
					{
						t.ApplicationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "mstsc.exe");
						t.Arguments = String.Format("/v:{0}:{1}", vm.FQDN, vm.RemoteConnectionPort);
					}
					else
					{
						if (App.GlobalConfig.SSHClientPath != null && App.GlobalConfig.SSHClientPath.Exists && !String.IsNullOrEmpty(App.GlobalConfig.SSHClientCmdLine))
						{
							t.ApplicationPath = App.GlobalConfig.SSHClientPath.FullName;
							App.GlobalConfig.SSHClientCmdLine = App.GlobalConfig.SSHClientCmdLine.Replace(AppResources.PH_FQDN, vm.FQDN);
							App.GlobalConfig.SSHClientCmdLine = App.GlobalConfig.SSHClientCmdLine.Replace(AppResources.PH_PORT, "" + vm.RemoteConnectionPort);
							t.Arguments = App.GlobalConfig.SSHClientCmdLine;
						}
					}
					t.IconResourcePath = Path.Combine(Environment.CurrentDirectory, "davm.exe");
					t.IconResourceIndex = 0;
					t.Description = "Launch remote connection";
					t.Title = vm.Name;
					t.CustomCategory = "Remote connection";
					jumpList.JumpItems.Add(t);

				}
				JumpList.SetJumpList(App.Current, jumpList);
			});
		}

		public static bool NotifyUser(string message, bool canCancel, MetroWindow parent, bool isError = false)
		{
			var res = MessageBox.Show(message,"DAVM",MessageBoxButton.OK, isError?MessageBoxImage.Error :MessageBoxImage.Information);
			if (res == MessageBoxResult.OK)
				return true;
			else
				return false;
		}
	}

	#region Converters
	public class VMStatusToTooltipConverter : IValueConverter
	{
		public object Convert(object value, Type targetType,
			object parameter, CultureInfo culture)
		{
			VMStatus status = (VMStatus)value;

			switch (status)
			{
				case VMStatus.Running:
					return "VM started";
				case VMStatus.Deallocated:
					return "VM Stopped and deallocated (NOT counting for billing)";
				case VMStatus.Off:
					return "Guest OS Stopped or not responding (COUNTS for billing)";
				case VMStatus.Error:
					return "Error occured, check the log file";
				case VMStatus.Updating:
					return "Refreshing status, Azure is working on it";
			}

			return String.Empty;
		}

		public object ConvertBack(object value, Type targetType,
			object parameter, CultureInfo culture)
		{
			// Do the conversion from visibility to bool
			return value;
		}
	}

	public class IPAddressToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType,
			object parameter, CultureInfo culture)
		{
			var ip = value as IPAddress;
			if (ip != null)
			{
				return ip.ToString();
			}

			return "";
		}

		public object ConvertBack(object value, Type targetType,
			object parameter, CultureInfo culture)
		{
			// Do the conversion from visibility to bool
			return value;
		}
	}

	public class VMStatusToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType,
			object parameter, CultureInfo culture)
		{
			VMStatus status = (VMStatus)value;

			switch (status)
			{
				case VMStatus.Running:
					return new SolidColorBrush(Colors.Green);
				case VMStatus.Off:
					return new SolidColorBrush(Colors.Gray);
				case VMStatus.Deallocated:
					return new SolidColorBrush(Colors.Black);
				case VMStatus.Error:
					return new SolidColorBrush(Colors.Red);
				case VMStatus.Unknown:
					return new SolidColorBrush(Colors.LightGray);
				default:
					return new SolidColorBrush(Colors.Orange);
			}
		}

		public object ConvertBack(object value, Type targetType,
			object parameter, CultureInfo culture)
		{
			// Do the conversion from visibility to bool
			return value;
		}
	}

	public class StringToUpperConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!String.IsNullOrEmpty((string)value))
				return ((string)value).ToUpper();


			return value;
		}
		public object ConvertBack(object value, Type targetType,
			  object parameter, CultureInfo culture)
		{
			// Do the conversion from visibility to bool
			return value;
		}
	}

	public class InfoMessageToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return System.Windows.Visibility.Collapsed;

			InfoMessage status = (InfoMessage)value;

			switch (status.Level)
			{
				case LogType.Error:
					return System.Windows.Visibility.Visible;
				default:
					return System.Windows.Visibility.Collapsed;
			}

		}

		public object ConvertBack(object value, Type targetType,
			object parameter, CultureInfo culture)
		{
			// Do the conversion from visibility to bool
			return value;
		}
	}

	public class IsEnabledToProgressStateConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return TaskbarItemProgressState.None;
			var converted = (bool)value;
			return converted == true ? TaskbarItemProgressState.Indeterminate : TaskbarItemProgressState.None;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{

			return null;
		}
	}

	/// <summary>
	/// Show the element if the value is not NULL
	/// </summary>
	public class ExistToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return System.Windows.Visibility.Collapsed;

			else
				return System.Windows.Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType,
			object parameter, CultureInfo culture)
		{
			return value;
		}
	}

	public class RemoteConnectionTypeToisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((RemoteConnectionType)value != RemoteConnectionType.None)
				return System.Windows.Visibility.Visible;
			else
				return System.Windows.Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType,
			object parameter, CultureInfo culture)
		{
			return value;
		}
	}

	public class NotExistToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return System.Windows.Visibility.Visible;

			else
				return System.Windows.Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType,
			object parameter, CultureInfo culture)
		{
			return value;
		}
	}

	public class VMToOSImageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return "";
			else {
				if(value is LinuxVM)
					return "/Resources/vm_linux.png";
				else
					return "/Resources/vm_windows.png";
			}
		}

		public object ConvertBack(object value, Type targetType,
			object parameter, CultureInfo culture)
		{
			return value;
		}
	}


	#endregion

	#region Validation Rules
	public class FileExistValidationRule : ValidationRule
	{
		public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
		{
			string errorMessage = "File not exist";
			if (value == null)
				return new ValidationResult(false, errorMessage);

			String insertedValue = value as String;
			if (String.IsNullOrEmpty(insertedValue))
				return new ValidationResult(false, errorMessage);

			bool result = File.Exists(insertedValue);
			if (!result)
				return new ValidationResult(false, errorMessage);

			//if exist then check if valid PUBLISH file
			result = App.GlobalConfig.VMController.InitializeController(new FileInfo(insertedValue));
            return result ? ValidationResult.ValidResult : new ValidationResult(false, "Invalid Azure subcription file");
		}

	}
	#endregion
}
