using DAVM.Common;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Microsoft.WindowsAzure.Management.WebSites;
using Microsoft.WindowsAzure.Management.WebSites.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace DAVM.Model
{
    /// <remarks>Singleton</remarks>
    public class AzureResourceController : BindableObject
	{
		private static volatile AzureResourceController _instance;
		private static object _syncRoot = new Object();

		private static Timer _pollingTimer = new Timer(new TimerCallback(PollingStatus), null, 0, 15 * 1000);
		private static HashSet<AzureVM> _VMToRefresh = new HashSet<AzureVM>();
        private static HashSet<AzureWebSite> _WebsiteToRefresh = new HashSet<AzureWebSite>();
     
		//events used to notify the UI/ViewModels that the controller is busy or not
		public event EventHandler WorkStarted;
		public event EventHandler WorkCompleted;

        #region Properties
        private DirectoryInfo WorkingFolder { get; set; }

        /// <summary>
        /// File generated from Get-AzurePublishSettingsFile, contains a management certificate and all subscriptions
        /// </summary>   
        public FileInfo PublishSettingsFile
		{
			get;
			private set;
		}

		public bool ControllerInitialized;

        public virtual ObservableCollection<AzureSubscription> AzureSubscriptions
		{
			get;
			set;
		}

        #endregion

        #region Events
        private void RaiseCompletedEvent()
		{
            IsWorking = false;
			if (WorkCompleted != null)
				foreach(var m in WorkCompleted.GetInvocationList())
						m.DynamicInvoke(this,null);

        }

        private void RaiseStartedEvent()
		{
			IsWorking = true;
			if (WorkStarted != null)
				foreach (var m in WorkCompleted.GetInvocationList())
					m.DynamicInvoke(this, null);
        }

		public async Task StartAllAsync(IEnumerable<AzureResource> resources)
		{
			await Task.Factory.StartNew(() =>
			{
                Logger.LogEntry(LogType.Info, "Starting "+resources.Count()+" resources");

				//barrier used to disabled UI until we are working
				Barrier startingWorkingBarrier = new Barrier(resources.Count(), null);
				RaiseStartedEvent();
				Parallel.ForEach<AzureResource>(resources, vm =>
				{
					vm.StartAsync().Wait();
                    startingWorkingBarrier.SignalAndWait();
				});
				RaiseCompletedEvent();
				Logger.LogEntry(LogType.Info, "Resources started successfully");

			});
		}

		public async Task StopAllAsync(IEnumerable<AzureResource> resources)
		{

			await Task.Factory.StartNew(() =>
			{
                Logger.LogEntry(LogType.Info, "Stopping "+resources.Count()+" resources");

				RaiseStartedEvent();

				//barrier used to disabled UI until we are working
				Barrier stopWorkingBarrier = new Barrier(resources.Count(), null);
			
				Parallel.ForEach<AzureResource>(resources, vm =>
			    {
				   vm.StopAsync().Wait();
				   stopWorkingBarrier.SignalAndWait();
			    });
				RaiseCompletedEvent();
			    Logger.LogEntry(LogType.Info, "Resources stopped successfully");

			});

		}
        #endregion


        #region VM
        public async Task StopResourceAsync(AzureVM vm)
        {
            //could not do anything when the status is pending
            if (vm.Status == ResourceStatus.Deallocated)
            {
                RaiseCompletedEvent();
                return;
            }

            if (!ControllerInitialized)
			{
				Logger.LogEntry(LogType.Warning, "Controller not ready");
				return;
			}
			Logger.LogEntry(LogType.Info, "Stopping " + vm.Name);
			RaiseStartedEvent();


#if OFFLINE
			await Task.Delay(5000);
			vm.Status = VMStatus.Off;
			RaiseCompletedEvent();
#else

			try
			{				
				vm.Status = ResourceStatus.Stopping;

				ComputeManagementClient client = new ComputeManagementClient(vm.Subscription.CloudCredentials);
				var options = new VirtualMachineShutdownParameters();
				options.PostShutdownAction = PostShutdownAction.StoppedDeallocated;
				await client.VirtualMachines.ShutdownAsync(vm.ServiceName, vm.DeploymentName, vm.Name, options);

				PollingStatus(vm);
				_VMToRefresh.Add(vm);

			}
			catch (Microsoft.WindowsAzure.CloudException azEx)
			{
				//if another istance with the same deploymentID (==same cloud service) is starting/stopping, we cannot issue any command and need to wait
				//otherwise will get the following error:
				//     "ConflictError: Windows Azure is currently performing an operation with x-ms-requestid c5c29f4e69d270578f78d66e4282c9c4 on this deployment that requires exclusive access."
				//if so We just need to reissue the command after a random amount of time
				if (azEx.ErrorCode == "ConflictError")
				{
					RegisterForLazyUpdate(vm, ResourceStatus.Deallocated);
				}
			}
			catch (Exception pse)
			{
				vm.Status = ResourceStatus.Error;
				Logger.LogEntry("Could not stop " + vm.Name, pse);
				throw new Exception("Stop failed");
			}
			finally
			{
				RaiseCompletedEvent();
			}

#endif
		}

        public async Task StartResourceAsync(AzureResource resource)
        {
            if (resource != null)
            {
                if (resource.GetType() == typeof(AzureWebSite))
                    StartResourceAsync((AzureWebSite)resource);
                else if (resource.GetType().BaseType == typeof(AzureVM))
                    StartResourceAsync((AzureVM)resource);
            }
        }


        public async Task StartResourceAsync(AzureVM vm)
		{
			//could not do anything when the status is pending
			if (vm.Status == ResourceStatus.Running)
				return;

			if (!ControllerInitialized)
			{
				Logger.LogEntry(LogType.Warning, "Controller not ready");
				return;
			}

			Logger.LogEntry(LogType.Info, "Starting " + vm.Name);
			RaiseStartedEvent();
#if OFFLINE
			await Task.Delay(5000);
			vm.Status = VMStatus.Running;
			RaiseCompletedEvent();
			return;
#else

			try
			{
				RaiseStartedEvent();

				vm.Status = ResourceStatus.Starting;

				ComputeManagementClient client = new ComputeManagementClient(vm.Subscription.CloudCredentials);
				await client.VirtualMachines.StartAsync(vm.ServiceName, vm.DeploymentName, vm.Name);
				_VMToRefresh.Add(vm);

			}
			catch (Microsoft.WindowsAzure.CloudException azEx)
			{
				//if another istance with the same deploymentID (==same cloud service) is starting/stopping, we cannot issue any command and need to wait
				//otherwise will get the following error:
				//     "ConflictError: Windows Azure is currently performing an operation with x-ms-requestid c5c29f4e69d270578f78d66e4282c9c4 on this deployment that requires exclusive access."
				//if so We just need to reissue the command after a random amount of time
				if (azEx.ErrorCode == "ConflictError")
				{
					RegisterForLazyUpdate(vm,ResourceStatus.Running);
				}
			}
			catch (Exception pse)
			{
				vm.Status = ResourceStatus.Error;
				Logger.LogEntry("Could not start " + vm.Name, pse);
				throw new Exception("Start failed");
			}
			finally
			{
				RaiseCompletedEvent();
			}

#endif
		}

        /// <summary>
        /// Try to start/stop a VM with retry-logic
        /// </summary>
		private async void RegisterForLazyUpdate(AzureVM vm, ResourceStatus toStatus)
		{
            //sometime azure does not accept new operation for a while
            //the status change is not live, has a delay period
            //this method try multiple time until the status is transitioned to the target value

			Random r = new Random();
			int waitTime = r.Next(20, 60);

			Logger.LogEntry(LogType.Info,String.Format("{0} is waiting ({1} sec) because another instance on the same deployment is starting/stopping",vm.Name, waitTime));
			
            while (vm.Status != toStatus)
			{
				//wait a random amount of time before to try again
				await Task.Delay(1000 * waitTime);

				if(toStatus == ResourceStatus.Running)
					await StartResourceAsync(vm);

				if (toStatus == ResourceStatus.Deallocated)
					await StopResourceAsync(vm);
			}
		}

		public async Task RetrieveVMsAsync(AzureSubscription subscription)
		{
			if (!ControllerInitialized)
			{
				Logger.LogEntry(LogType.Warning, "Controller not ready");
				return;
			}

			if (subscription == null)
			{
				Logger.LogEntry(LogType.Warning, "No subscription selected");
				return;
			}

			Logger.LogEntry(LogType.Info, String.Format("Downloading VMs for \"{0}\"", subscription.Name));
			RaiseStartedEvent();

#if OFFLINE
			Random r = new Random();
			await Task.Delay(5000);
            for (int i = 0; i < 50; i++)
            {
                AzureVM vm = new AzureVM(subscription);
                vm.Name = "VM " + i;
                vm.FQDN = "vm.cloudapp.net";
                vm.ServiceName = vm.Name + "Service";
                vm.Size = "Small";
                vm.LocalIPAddress = IPAddress.Parse("192.168.1.1");
                vm.RemoteConnectionPort = 3389;
				vm.RemoteConnectionType = RemoteConnectionType.RDP;
				
                vm.Status = (VMStatus) r.Next(8);
				subscription.VMs.Add(vm);
            }
			//return;
			RaiseCompletedEvent();
#else


			await Task.Factory.StartNew(() =>
			{
				try
				{
				
					//Application.Current.Dispatcher.Invoke(() => subscription.VMs.Clear());

					//1 command for all machines
					ComputeManagementClient client = new ComputeManagementClient(subscription.CloudCredentials);

					var hostedServices = client.HostedServices.List();
					HashSet<string> lastVMNames = new HashSet<string>();
					foreach (var service in hostedServices)
					{
						var deployment = GetAzureDeyployment(client, service.ServiceName, DeploymentSlot.Production);
						if (deployment != null)
						{
							if (deployment.Roles.Count > 0)
							{
								foreach (var role in deployment.Roles)
								{
									if (role.RoleType == VirtualMachineRoleType.PersistentVMRole.ToString())
									{
										var vm = RetrieveVM(deployment, subscription, role, service);
										//this will check the status until it reaches a Ready or StoppedDeallocated 
										if(vm.Status != ResourceStatus.Running && vm.Status != ResourceStatus.Deallocated)
											_VMToRefresh.Add(vm);
                                        lastVMNames.Add(vm.Name);
									}
								}
							}
						}
					}

					//if a VM has been deleted whilst this program was open, I do not get the update
					//I need to clean the main collection, removing all the stale VMs 
					Application.Current.Dispatcher.Invoke(() =>
					{
						var notFoundVMs = subscription.VMs.Where((v) => !lastVMNames.Contains(v.Name));
						foreach (AzureVM vm in notFoundVMs)
							subscription.Resources.Remove(vm);
					}
					);

					Logger.LogEntry(LogType.Info, "VM Refresh completed");
				}
				catch (Exception pse)
				{
					Logger.LogEntry("Could not retrieve VMs", pse);
				}
				finally
				{
					RaiseCompletedEvent();
				}
			}
			);
#endif

		}

        /// <summary>
        /// Retrieve all the properties of a VM
        /// </summary>
		private AzureVM RetrieveVM(DeploymentGetResponse deployment, AzureSubscription subscription, Role role, HostedServiceListResponse.HostedService service)
		{
			//find the instance related to this VM, it does contains more information
			var realInstance = deployment.RoleInstances.Where((i) => i.RoleName == role.RoleName).FirstOrDefault();

            AzureVM vm;

            // if there is a VM with the same name in the main list use it
            var vms = subscription.VMs.Where((v) =>v.Name == role.RoleName && v.ServiceName == service.ServiceName);
                      
            if (vms.Count() > 0)
                vm = vms.First();
                //otherwise create a new one
            else
            {
                vm = new AzureVM(subscription);
                switch (role.OSVirtualHardDisk.OperatingSystem)
                {
                    case "Windows": { vm = new WindowsVM(subscription); break; }
                    default: { vm = new LinuxVM(subscription); break; }
                }
            }
            

            //refresh all properties
            vm.Location = service.Properties.Location;

			//resource group is not mandatory, might not exists
			if (service.Properties.ExtendedProperties != null && service.Properties.ExtendedProperties.ContainsKey("ResourceGroup"))
				vm.ResourceGroup = service.Properties.ExtendedProperties["ResourceGroup"];

			vm.DeploymentID = deployment.PrivateId;
			vm.Name = role.RoleName;
			vm.ServiceName = service.ServiceName;
			vm.Size = role.RoleSize;
			vm.DeploymentName = deployment.Name;
			vm.FQDN = deployment.Uri.DnsSafeHost;
			vm.OS = role.OSVirtualHardDisk.OperatingSystem;

			if (!String.IsNullOrEmpty(realInstance.IPAddress))
				vm.LocalIPAddress = IPAddress.Parse(realInstance.IPAddress);

			//get public IP, if any
			if (realInstance.PublicIPs != null && realInstance.PublicIPs.Count > 0)
			{
				if (!String.IsNullOrEmpty(realInstance.PublicIPs[0].Address))
					vm.LocalIPAddress = IPAddress.Parse(realInstance.PublicIPs[0].Address);
			}

			//seems that this SDK does not return all of these statues, but only some of them like ReadyRole / StoppedVM
			//keeping this switch for the future
			Logger.LogEntry(LogType.Info, "instance state: " + realInstance.InstanceStatus);

			switch (realInstance.InstanceStatus)
			{
				case RoleInstanceStatus.ReadyRole: { vm.Status = ResourceStatus.Running; break; }
				case RoleInstanceStatus.CreatingVM: { vm.Status = ResourceStatus.Starting; break; }
				case RoleInstanceStatus.CreatingRole: { vm.Status = ResourceStatus.Starting; break; }
				case RoleInstanceStatus.DeletingVM: { vm.Status = ResourceStatus.Stopping; break; }
				case RoleInstanceStatus.StartingVM: { vm.Status = ResourceStatus.Starting; break; }
				case RoleInstanceStatus.StoppingVM: { vm.Status = ResourceStatus.Stopping; break; }
				case RoleInstanceStatus.StoppedVM: { vm.Status = ResourceStatus.Off; break; }
				case RoleInstanceStatus.RestartingRole: { vm.Status = ResourceStatus.Starting; break; }
				case RoleInstanceStatus.BusyRole: { vm.Status = ResourceStatus.Updating; break; }
				case RoleInstanceStatus.RoleStateUnknown: { vm.Status = ResourceStatus.Unknown; break; }
				case RoleInstanceStatus.FailedStartingVM: { vm.Status = ResourceStatus.Error; break; }
				case RoleInstanceStatus.FailedStartingRole: { vm.Status = ResourceStatus.Error; break; }
				case RoleInstanceStatus.StartingRole: { vm.Status = ResourceStatus.Starting; break; }
				case RoleInstanceStatus.StoppingRole: { vm.Status = ResourceStatus.Stopping; break; }
				case RoleInstanceStatus.UnresponsiveRole: { vm.Status = ResourceStatus.Error; break; }
				//this is OFF and deallocated --> not paying (FREE)
				//this is not "documentated"
				case "StoppedDeallocated": { vm.Status = ResourceStatus.Deallocated; break; }
				default: { vm.Status = ResourceStatus.Unknown; break; }
			}

			//search for any RDP endpoint
			if (role.ConfigurationSets != null && role.ConfigurationSets.Count > 0)
			{
				foreach (ConfigurationSet cs in role.ConfigurationSets)
				{
					if (cs.ConfigurationSetType == ConfigurationSetTypes.NetworkConfiguration.ToString())
					{
						//dynamic ncs = cs ;
						foreach (InputEndpoint ie in cs.InputEndpoints)
						{
							if (ie.LocalPort == 3389 || (ie.Name.IndexOf("rdp", StringComparison.OrdinalIgnoreCase) >= 0) || (ie.Name.IndexOf("desktop", StringComparison.OrdinalIgnoreCase) >= 0))
							{
								vm.RemoteConnectionType = RemoteConnectionType.RDP;
								vm.RemoteConnectionPort = ie.Port.Value;
								Logger.LogEntry(LogType.Verbose, "Found RDP endpoint for " + vm.Name);
							}
							if (ie.Name.IndexOf("ssh", StringComparison.OrdinalIgnoreCase) >= 0)
							{
                                //if ssh is in the name
								vm.RemoteConnectionType = RemoteConnectionType.SSH;
								vm.RemoteConnectionPort = ie.Port.Value;
								Logger.LogEntry(LogType.Verbose, "Found SSH endpoint for " + vm.Name);
							}
						}
					}
				}
			}

            //because VMs are bound to UI                                            
            Application.Current.Dispatcher.Invoke(() =>
            {
                //adding if was missing
                if (!subscription.Resources.Contains(vm))
                    subscription.Resources.Add(vm);
            }
           );

            Logger.LogEntry(LogType.Info, String.Format("Found VM: {0} ({1})", vm.Name, vm.ServiceName));
			return vm;
		}

        #endregion

        #region Websites

        public async Task RetrieveWebsitesAsync(AzureSubscription subscription)
        {
            if (!ControllerInitialized)
            {
                Logger.LogEntry(LogType.Warning, "Controller not ready");
                return;
            }

            if (subscription == null)
            {
                Logger.LogEntry(LogType.Warning, "No subscription selected");
                return;
            }

            Logger.LogEntry(LogType.Info, String.Format("Downloading Websites for \"{0}\"", subscription.Name));
            RaiseStartedEvent();

            await Task.Factory.StartNew(() =>
            {
                try
                {

                    //Application.Current.Dispatcher.Invoke(() => subscription.VMs.Clear());

                    //1 command for all websites
                    using (var client = new WebSiteManagementClient(subscription.CloudCredentials))
                    {

                        var webspaces = client.WebSpaces.List();
                        HashSet<string> lastWebsiteNames = new HashSet<string>();
                        foreach (var webspace in webspaces)
                        {
                            Logger.LogEntry(LogType.Info, "Webspace found: " + webspace.Name);
                            var websites = client.WebSpaces.ListWebSites(webspace.Name, new WebSiteListParameters()).ToList();
                            foreach (var w in websites)
                            {                             
                                AzureWebSite appWeb = new AzureWebSite(subscription);

                                //management portal (is always on!)
                                var kuduUrl = w.HostNameSslStates.Where((s) => s.Name.Contains(".scm.")).First().Name;
                                appWeb.KuduUrl = new Uri("http://"+kuduUrl);

                                appWeb.Name = w.Name;
                                appWeb.WebspaceName = webspace.Name;
                                appWeb.Plan = w.Sku;                                
                                appWeb.Location = webspace.GeoRegion;
                                appWeb.FQDNs = new HashSet<string>(w.HostNames);
                                Logger.LogEntry(LogType.Info, "Website found: " + w.Name);
                                switch (w.State) {
                                    case "Running": { appWeb.Status = ResourceStatus.Running;  break; }
                                    case "Stopped": { appWeb.Status = ResourceStatus.Off; break; }
                                    default: { appWeb.Status = ResourceStatus.Unknown;  break; }
                                }

                                //this will check the status until it reaches a Ready or StoppedDeallocated 
                                if (appWeb.Status != ResourceStatus.Running && appWeb.Status != ResourceStatus.Deallocated)
                                    _WebsiteToRefresh.Add(appWeb);

                                //because Websites are bound to UI                                            
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    if (!subscription.Resources.Contains(appWeb))
                                        subscription.Resources.Add(appWeb);
                                });

                                lastWebsiteNames.Add(appWeb.Name);
                            }
                        }

                        //if a Website has been deleted whilst this program was open, I do not get the update
                        //I need to clean the main collection, removing all the stale Websites 
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var notFoundWebsite = subscription.Websites.Where((v) => !lastWebsiteNames.Contains(v.Name));
                            foreach (AzureWebSite w in notFoundWebsite)
                                subscription.Resources.Remove(w);
                        }
                        );
                    }

                    Logger.LogEntry(LogType.Info, "Website Refresh completed");
                }
                catch (Exception pse)
                {
                    Logger.LogEntry("Could not retrieve VMs", pse);
                }
                finally
                {
                    RaiseCompletedEvent();
                }
            }
            );
        }

        public async Task StopResourceAsync(AzureWebSite website)
        {
            if (website.Status == ResourceStatus.Off) {
                RaiseCompletedEvent();
                return;
            }

            if (!ControllerInitialized)
            {
                Logger.LogEntry(LogType.Warning, "Controller not ready");
                return;
            }

            Logger.LogEntry(LogType.Info, "Stopping " + website.Name);
            RaiseStartedEvent();
#if OFFLINE
            			await Task.Delay(5000);
            			website.Status = ResourceStatus.Off;
            			RaiseCompletedEvent();
            			return;
#else
            try
            {
                RaiseStartedEvent();

                website.Status = ResourceStatus.Stopping;

                WebSiteManagementClient client = new WebSiteManagementClient(website.Subscription.CloudCredentials);
                var deploymentResponse = client.WebSites.Get(website.WebspaceName, website.Name, null);
                var par = new WebSiteUpdateParameters();
                par.State = "Stopped";
                par.HostNames = website.FQDNs.ToList();

                client.WebSites.Update(deploymentResponse.WebSite.WebSpace, deploymentResponse.WebSite.Name, par);

                _WebsiteToRefresh.Add(website);

            }
            catch (Microsoft.WindowsAzure.CloudException azEx)
            {
                //if another istance with the same deploymentID (==same cloud service) is starting/stopping, we cannot issue any command and need to wait
                //otherwise will get the following error:
                //     "ConflictError: Windows Azure is currently performing an operation with x-ms-requestid c5c29f4e69d270578f78d66e4282c9c4 on this deployment that requires exclusive access."
                //if so We just need to reissue the command after a random amount of time
                if (azEx.ErrorCode == "ConflictError")
                {
                    //RegisterForLazyUpdate(vm, VMStatus.Running);
                }
            }
            catch (Exception pse)
            {
                website.Status = ResourceStatus.Error;
                Logger.LogEntry("Could not stop " + website.Name, pse);
                throw new Exception("Stop failed");
            }
            finally
            {
                RaiseCompletedEvent();
            }

#endif
        }

        public async Task StartResourceAsync(AzureWebSite website)
        {      
            if (website.Status == ResourceStatus.Running)
                return;

            if (!ControllerInitialized)
            {
                Logger.LogEntry(LogType.Warning, "Controller not ready");
                return;
            }

            Logger.LogEntry(LogType.Info, "Starting " + website.Name);
            RaiseStartedEvent();
#if OFFLINE
            			await Task.Delay(5000);
            			website.Status = ResourceStatus.Running;
            			RaiseCompletedEvent();
            			return;
#else
            try
            {
                RaiseStartedEvent();

                website.Status = ResourceStatus.Starting;

                WebSiteManagementClient client = new WebSiteManagementClient(website.Subscription.CloudCredentials);
                var deploymentResponse = client.WebSites.Get(website.WebspaceName, website.Name, null);
                var par = new WebSiteUpdateParameters();
                par.State = "Running";
                par.HostNames = website.FQDNs.ToList();

                client.WebSites.Update(deploymentResponse.WebSite.WebSpace, deploymentResponse.WebSite.Name, par);

                _WebsiteToRefresh.Add(website);

            }
            catch (Microsoft.WindowsAzure.CloudException azEx)
            {
                //if another istance with the same deploymentID (==same cloud service) is starting/stopping, we cannot issue any command and need to wait
                //otherwise will get the following error:
                //     "ConflictError: Windows Azure is currently performing an operation with x-ms-requestid c5c29f4e69d270578f78d66e4282c9c4 on this deployment that requires exclusive access."
                //if so We just need to reissue the command after a random amount of time
                if (azEx.ErrorCode == "ConflictError")
                {
                    //RegisterForLazyUpdate(vm, VMStatus.Running);
                }
            }
            catch (Exception pse)
            {
                website.Status = ResourceStatus.Error;
                Logger.LogEntry("Could not start " + website.Name, pse);
                throw new Exception("Start failed");
            }
            finally
            {
                RaiseCompletedEvent();
            }

#endif
        }
        #endregion

        #region Methods

        public async Task StopResourceAsync(AzureResource resource)
        {
            if (resource != null)
            {
                if (resource.GetType() == typeof(AzureWebSite))
                    StopResourceAsync((AzureWebSite)resource);
                else if (resource.GetType().BaseType == typeof(AzureVM))
                    StopResourceAsync((AzureVM)resource);
            }

        }

        private AzureResourceController(DirectoryInfo workingFolder)
        {
            ControllerInitialized = false;
            IsWorking = false;
            AzureSubscriptions = new ObservableCollection<AzureSubscription>();

            if (workingFolder.Exists)
                WorkingFolder = workingFolder;
            else
                Logger.LogEntry(LogType.Warning, "Working folder not exists: " + workingFolder.FullName);
        }

        public static AzureResourceController GetInstance(DirectoryInfo workingDir)
        {
            if (_instance == null)
            {
                lock (_syncRoot)
                {
                    if (_instance == null)
                        _instance = new AzureResourceController(workingDir);
                }
            }

            return _instance;
        }

        public async Task RetrieveAllAsync(AzureSubscription subscription)
        {
            await RetrieveVMsAsync(subscription);
            await RetrieveWebsitesAsync(subscription);
        }

        private static DeploymentGetResponse GetAzureDeyployment(ComputeManagementClient client, string serviceName, DeploymentSlot slot)
		{
			try
			{
				return client.Deployments.GetBySlot(serviceName, slot);

			}
			catch (CloudException ex)
			{

				if (ex.ErrorCode == "ResourceNotFound")
				{
					return null;
				}
				else
				{
					throw;
				}
			}
		}

		/// <summary>
		/// Check if Azure Powershell commands work, and discover all the subscriptions in the publishing settings file
		/// Then registers all the Subscriptions specified the file publishing settings file into the current user profile
		/// Each subscription will then be available for the user
		/// </summary>
		/// <param name="publishSettingsFile">File generated by Get-AzurePublishSettingsFile, contains all the subscirptions for the user and the management certificate</param>
		/// <returns>false in case of errors</returns>
		public bool InitializeController(FileInfo publishSettingsFile)
		{
			//STEPS
			// 1- Initialize each subscription

			bool result = false;
			Logger.LogEntry(LogType.Info, "Initializing Azure Controller");
			RaiseStartedEvent();

			try
			{
				if (publishSettingsFile == null || !publishSettingsFile.Exists)
					throw new Exception("Publish Settings file not valid");

				PublishSettingsFile = publishSettingsFile; //file exists!
				Logger.LogEntry(LogType.Verbose, "Publish Settings File: " + PublishSettingsFile.FullName);

				AzureSubscriptions.Clear();

				DiscoverSubscriptions(); //discover Azure Subscriptions and add them to the main collection

				Logger.LogEntry(LogType.Info, "Azure Controller Initialized!");
				result = true;
			}
			catch (Exception pse)
			{
				Logger.LogEntry("Could not initiliaze the Azure Controller", pse);
				result = false;
			}
			finally
			{
				ControllerInitialized = result;
				RaiseCompletedEvent();
			}

			return result;
		}

		/// <summary>
		/// Goes through the Publish settings file to find out all the Subscriptions, it adds the results to the Subscriptions collection
		/// </summary>
		private void DiscoverSubscriptions()
		{

			XmlDocument doc = new XmlDocument();
			doc.Load(PublishSettingsFile.FullName);

			var subNodes = doc.SelectNodes("//Subscription");
			foreach (XmlElement s in subNodes)
			{

				AzureSubscription newAS = new AzureSubscription(this, s.Attributes["Id"].Value, s.Attributes["ManagementCertificate"].Value, s.Attributes["Name"].Value);

				Logger.LogEntry(LogType.Info, String.Format("Found Azure Subscription: \"{0}\"", newAS.Name));
				if (!AzureSubscriptions.Contains(newAS))
					AzureSubscriptions.Add(newAS);
            }

            //statistics
            App.GlobalConfig.Telemetry.TrackMetric(TelemetryHelper.METRIC_SUBSCRIPTION, AzureSubscriptions.Count);
        }


        /// <summary>
        /// Execute the PS commands that create a file with all the Subscriptions and the management certificate
        /// This file is needed to execute the PS Azure commands
        /// </summary>
        public void DownloadPublishSettings()
		{
			try
			{
				Logger.LogEntry(LogType.Info, "Downloading Azure Publish Settings - Waiting for User");

				Process.Start("https://manage.windowsazure.com/publishsettings/index?client=powershell");
			}
			catch (Exception pse)
			{
				Logger.LogEntry("Could not Download Azure publish settings", pse);
			}
		}

        /// <summary>
        /// Check the status of pending resources until it is Ready or Stopped, maximum 5 minutes
        /// </summary>
        /// <param name="vm"></param>
        public static void PollingStatus(object state)
        {
            if (_WebsiteToRefresh.Count > 0)
            {
                try
                {
                    var website = _WebsiteToRefresh.First();
                    WebSiteManagementClient client = new WebSiteManagementClient(website.Subscription.CloudCredentials);
                    var deploymentResponse = client.WebSites.Get(website.WebspaceName, website.Name, new WebSiteGetParameters());
                    var appWeb = deploymentResponse.WebSite;

                    var previousStatus = website.Status;

                    switch (appWeb.State)
                    {
                        case "Running": { website.Status = ResourceStatus.Running; _WebsiteToRefresh.Remove(website); break; }
                        case "Stopped": { website.Status = ResourceStatus.Off; _WebsiteToRefresh.Remove(website); break; }
                        default: { website.Status = ResourceStatus.Unknown; break; }
                    }
                    Logger.LogEntry(LogType.Verbose, string.Format("Polling thread: Updated Website: {2} - status from {0} to {1}", previousStatus, website.Status, website.Name));
                }
                catch (Exception ex)
                {
                    Logger.LogEntry("Polling thread error: ", ex);
                }
                finally
                {
                    //  UIHelper.RegisterJumpList();
                }

            }

            if (_VMToRefresh.Count > 0)
            {
                try
                {
                    var firtVm = _VMToRefresh.First();
                    ComputeManagementClient client = new ComputeManagementClient(firtVm.Subscription.CloudCredentials);
                    var deploymentResponse = client.Deployments.GetByName(firtVm.ServiceName, firtVm.DeploymentName);
                    foreach (var roleInstance in deploymentResponse.RoleInstances)
                    {
                        var vm = _VMToRefresh.Where((v) => v.Name == roleInstance.InstanceName).FirstOrDefault();
                        if (vm != null)
                        {
                            var previousStatus = vm.Status;

                            switch (roleInstance.InstanceStatus)
                            {
                                case RoleInstanceStatus.ReadyRole: { vm.Status = ResourceStatus.Running; _VMToRefresh.Remove(vm); break; }
                                case RoleInstanceStatus.StartingVM: { vm.Status = ResourceStatus.Starting; break; }
                                case RoleInstanceStatus.StoppingVM: { vm.Status = ResourceStatus.Stopping; break; }
                                case RoleInstanceStatus.CreatingVM: { vm.Status = ResourceStatus.Starting; break; }
                                case RoleInstanceStatus.CreatingRole: { vm.Status = ResourceStatus.Starting; break; }
                                case RoleInstanceStatus.DeletingVM: { vm.Status = ResourceStatus.Stopping; break; }
                                case RoleInstanceStatus.StoppedVM: { vm.Status = ResourceStatus.Off; _VMToRefresh.Remove(vm); break; }
                                case RoleInstanceStatus.RestartingRole: { vm.Status = ResourceStatus.Starting; break; }
                                case RoleInstanceStatus.BusyRole: { vm.Status = ResourceStatus.Updating; break; }
                                case RoleInstanceStatus.RoleStateUnknown: { vm.Status = ResourceStatus.Unknown; break; }
                                case RoleInstanceStatus.FailedStartingVM: { vm.Status = ResourceStatus.Error; break; }
                                case RoleInstanceStatus.FailedStartingRole: { vm.Status = ResourceStatus.Error; break; }
                                case RoleInstanceStatus.StartingRole: { vm.Status = ResourceStatus.Starting; break; }
                                case RoleInstanceStatus.StoppingRole: { vm.Status = ResourceStatus.Stopping; break; }
                                case RoleInstanceStatus.UnresponsiveRole: { vm.Status = ResourceStatus.Error; break; }
                                //this is OFF and deallocated --> not paying (FREE)
                                //this is not "documentated"
                                case "StoppedDeallocated": { vm.Status = ResourceStatus.Deallocated; _VMToRefresh.Remove(vm); break; }

                                default: { vm.Status = ResourceStatus.Unknown; break; }
                            }
                            Logger.LogEntry(LogType.Verbose, string.Format("Polling thread: Updated VM: {2} - status from {0} to {1}", previousStatus, vm.Status, vm.Name));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogEntry("Polling thread error: ", ex);
                }
                finally
                {
                    UIHelper.RegisterJumpList();
                }

            }
        }
        #endregion

    }
}

