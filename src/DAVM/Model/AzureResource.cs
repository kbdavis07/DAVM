using DAVM.Common;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace DAVM.Model
{

    public abstract class AzureResource : BindableObject
    {
        #region Properties

        //for visual grouping
        public abstract String AzureResourceType
        {
            get;
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

        public ResourceStatus _status;
        public ResourceStatus Status
        {
            get { return _status; }
            set
            {
                if (value != _status)
                {
                    //special case, if the user started/stopped the VM, azure will answer with Unknown until has completely started/stopped
                    //this create confusion because the uer see this transition to UNKNOWN status -
                    //to avoid this I ignore this status if we are in the middle of a working
                    if (IsWorking && value == ResourceStatus.Unknown)
                        return;

                    _status = value;
                    IsWorking = false;

                    switch (value)
                    {
                        case ResourceStatus.Running:
                            if (Started != null)
                                Started.BeginInvoke(this, null, null, null);
                            Logger.LogEntry(LogType.Info, Name + " started");
                            break;
                        case ResourceStatus.Deallocated:
                        case ResourceStatus.Off:
                            if (Stopped != null)
                                Stopped.BeginInvoke(this, null, null, null);
                            Logger.LogEntry(LogType.Info, Name + " stopped");
                            break;
                        case ResourceStatus.Error:
                            if (ErrorOccured != null)
                                ErrorOccured.BeginInvoke(this, null, null, null);
                            break;
                        case ResourceStatus.Stopping:
                        case ResourceStatus.Starting:
                        case ResourceStatus.Updating:
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

        public AzureSubscription Subscription { get; internal set; }
        public bool CanBeStarted
        {
            get { return (Status != ResourceStatus.Running) && !IsWorking; }
            set { }
        }
        public bool CanBeStopped
        {
            get { return (Status != ResourceStatus.Deallocated) && !IsWorking; }
            set { }
        }
        public String Error
        {
            get;
            set;
        }
        #endregion

        #region events
        public event EventHandler Started;
        public event EventHandler Stopped;
        public event EventHandler<Exception> ErrorOccured;
        #endregion

        #region Methods

        public virtual string GetVerboseDetails()
        {
            return String.Empty;
        }

        public async Task StartAsync()
        {
            try
            {
                IsWorking = true;
                await Subscription.Controller.StartResourceAsync(this);
            }
            catch (Exception e)
            {
                Logger.LogEntry("START error for " + this.Name, e);
                Error = "Cannot start the resource";
                Status = ResourceStatus.Error;
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
                await Subscription.Controller.StopResourceAsync(this);
            }
            catch (Exception e)
            {
                Logger.LogEntry("STOP error for " + this.Name, e);
                Error = "Cannot stop the resource";
                Status = ResourceStatus.Error;
            }
            //finally
            //{
            //    IsWorking = false;
            //}
        }

        #endregion
    }
}
