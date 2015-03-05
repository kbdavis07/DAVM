﻿using DAVM.Common;
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DAVM.Model
{
	public enum RemoteConnectionType
	{
		RDP,
		SSH,
		None
	}
    public class AzureVM : ModelBase
    {
        public AzureVM(AzureSubscription owner)
        {
            Subscription = owner;
			RemoteConnectionPort = -1;
			Status = VMStatus.Unknown;
        }

        public event EventHandler Started;
        public event EventHandler Stopped;
        public event EventHandler<Exception> ErrorOccured;

		#region properties
		public AzureSubscription Subscription { get; internal set; }
        public bool CanBeStarted
        {
            get { return (Status != VMStatus.Running) && !IsWorking; }
            set { }
        }
        public bool CanBeStopped
        {
            get { return (Status != VMStatus.Deallocated) && !IsWorking; }
            set { }
        }
		public String Error
        {
            get;
            set;
        }
		public String DeploymentID
		{
			get;
			set;
		}
		public String DeploymentName
        {
            get;
            set;
        }

		public String _location;
		public String Location
		{
			get { return _location; }
			set
			{
				if (value != _location)
				{
					_location = value;
					RaisePropertyChanged("Location");
				}
			}
		}

		public String _name;
        public String Name
        {
            get { return _name; }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }

		public String _os;
		public String OS
		{
			get { return _os; }
			set
			{
				if (value != _os)
				{
					_os = value;
					RaisePropertyChanged("OS");
				}
			}
		}

		public bool SupportRemoteConnection
		{
			get { return (Status == VMStatus.Running) && RemoteConnectionPort > 0 && !IsWorking; }
			set { }
		}

		public int RemoteConnectionPort
		{
			get; set;
		}

		private String _serviceName;
        public String ServiceName
		{
			get { return _serviceName; }
			set
			{
				if (value != _serviceName)
				{
					_serviceName = value;
					RaisePropertyChanged("ServiceName");
				}
			}
		}
		public RemoteConnectionType RemoteConnectionType { get; set; }

		private IPAddress _localIPAddress;
        public IPAddress LocalIPAddress
        {
            get { return _localIPAddress; }
            set
            {
                if (value != _localIPAddress)
                {
                    _localIPAddress = value;
                    RaisePropertyChanged("LocalIPAddress");
                }
            }
        }

		private IPAddress _publicIpAddress;
		public IPAddress PublicIPAddress
		{
			get { return _publicIpAddress; }
			set
			{
				if (value != _publicIpAddress)
				{
					_publicIpAddress = value;
					RaisePropertyChanged("PublicIPAddress");
				}
			}
		}

		private String _size;
        public String Size
        {
            get { return _size; }
            set
            {
                if (value != _size)
                {
                    _size = value;
                    RaisePropertyChanged("Size");
                }
            }
        }

		public String _resourceGroup;
		public String ResourceGroup
		{
			get { return _resourceGroup; }
			set
			{
				if (value != _resourceGroup)
				{
					_resourceGroup = value;
					RaisePropertyChanged("ResourceGroup");
				}
			}
		}

		public String _fqdn;
        public String FQDN
        {
            get { return _fqdn; }
            set
            {
                if (value != _fqdn)
                {
                    _fqdn = value;
                    RaisePropertyChanged("FQDN");
                }
            }
        }

        public VMStatus _status;
        public VMStatus Status
        {
            get { return _status; }
            set
            {
                if (value != _status)
                {
					//special case, if the user started/stopped the VM, azure will answer with Unknown until has completely started/stopped
					//this create confusion because the uer see this transition to UNKNOWN status -
					//to avoid this I ignore this status if we are in the middle of a working
					if (IsWorking && value == VMStatus.Unknown)
						return;

                    _status = value;
                    IsWorking = false;

                    switch (value)
                    {
						case VMStatus.Running:
                            if (Started != null)
                                Started.BeginInvoke(this, null,null,null);
                            Logger.LogEntry(LogType.Info, Name + " started");
                            break;
						case VMStatus.Deallocated:
                        case VMStatus.Off:
                            if (Stopped != null)
                                Stopped.BeginInvoke(this, null, null, null);
                            Logger.LogEntry(LogType.Info, Name + " stopped");
							break;
						case VMStatus.Error:
							if (ErrorOccured != null)
								ErrorOccured.BeginInvoke(this, null, null, null);
							break;
						case VMStatus.Stopping:
						case VMStatus.Starting:
						case VMStatus.Updating:
							IsWorking = true;
							break;
						default:
							break;
					}

                    RaisePropertyChanged("Status");
                    RaisePropertyChanged("CanBeStarted");
                    RaisePropertyChanged("CanBeStopped");
                    RaisePropertyChanged("SupportRemoteConnection");

                }
            }
        }
		#endregion

		#region methods

		public string GetVerboseDetails() {
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("[{0}]\n", Name);
			sb.AppendLine(String.Format("Status: {0}", Status));
			sb.AppendLine(String.Format("Service: {0}", ServiceName));
			sb.AppendLine(String.Format("Local IP: {0}", LocalIPAddress));
			sb.AppendLine(String.Format("Public IP: {0}", PublicIPAddress));
			sb.AppendLine(String.Format("Size: {0}", Size));
			sb.AppendLine(String.Format("FQDN: {0}", FQDN));
			sb.AppendLine(String.Format("RDP/SSH port: {0}", RemoteConnectionPort));
			sb.AppendLine(String.Format("OS: {0}", OS));
			sb.AppendLine(String.Format("Location: {0}", Location));
			sb.AppendLine(String.Format("Resource Group: {0}", ResourceGroup));
			sb.AppendLine(String.Format("Deployment ID: {0}", DeploymentID));
			sb.AppendLine(String.Format("Subscription ID: {0}", Subscription.ID));
			return sb.ToString();
		}

		public async Task StartAsync()
        {
            try
            {
                IsWorking = true;
                await Subscription.Controller.StartVMAsync(this);
            }
            catch (Exception e)
            {
				Logger.LogEntry("START error for " + this.Name ,e);
                Error = "Cannot start the VM";
                Status = VMStatus.Error;
            }
            //finally
            //{
            //    IsWorking = false;
            //}


        }

        public async Task StopAsync()
        {
            try
            {
                IsWorking = true;
                await Subscription.Controller.StopVMAsync(this);
            }
            catch (Exception e)
            {
				Logger.LogEntry("STOP error for " + this.Name, e);
				Error = "Cannot stop the VM";
                Status = VMStatus.Error;
            }
            //finally
            //{
            //    IsWorking = false;
            //}
        }

        /// <summary>
        /// Two VM are equal is the service name and the hostname is the same
        /// </summary>
        /// <param name="obj"></param>
        public override bool Equals(object obj)
        {
            AzureVM target = obj as AzureVM;
            if (target != null)
                return target.Name == this.Name && target.ServiceName == this.ServiceName;

            return false;
        }

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		#endregion
	}

	//for future improvements
	public class WindowsVM : AzureVM {
		public WindowsVM(AzureSubscription owner) : base(owner)
		{
		}

	}

	public class LinuxVM : AzureVM
	{
		public LinuxVM(AzureSubscription owner) : base(owner)
		{	
		}

	}

}

