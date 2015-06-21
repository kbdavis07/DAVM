using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DAVM.Model
{
    public class AzureSubscription : BindableObject
    {
        public CertificateCloudCredentials CloudCredentials { get; private set; }

        public AzureSubscription(AzureResourceController controller, string subscriptionID, string base64Certificate, string name) {

            if (String.IsNullOrEmpty(subscriptionID) || String.IsNullOrEmpty(base64Certificate) || String.IsNullOrEmpty(name))
                throw new ArgumentNullException();

            ID = subscriptionID;
            Controller = controller;            
            Resources = new ObservableCollection<AzureResource>();

            LastUpdate = DateTime.MinValue;
            Base64Certificate = base64Certificate;
            Name = name;
            CloudCredentials = new CertificateCloudCredentials(ID, new System.Security.Cryptography.X509Certificates.X509Certificate2(Convert.FromBase64String(Base64Certificate)));
        }

      
        public String ID
        {
            get;
            private set;
        }

        private String Base64Certificate {  get; set; }

        public  AzureResourceController Controller { get; private set; }

        public virtual ObservableCollection<AzureResource> Resources
        {
            get;
            set;
        }

        public virtual List<AzureVM> VMs
        {
            get {
                var vms= Resources.Where<AzureResource>((r) => r.GetType().BaseType == typeof(AzureVM));
                return vms.Select<AzureResource,AzureVM>((x)=>(AzureVM)x).ToList();
            }
        }

        public virtual IEnumerable<AzureWebSite> Websites
        {
            get
            {
                var vms = Resources.Where<AzureResource>((r) => r.GetType() == typeof(AzureWebSite));
                return vms.Select<AzureResource, AzureWebSite>((x) => (AzureWebSite)x).ToList();
            }
        }

        private DateTime _LastUpdate;
        public DateTime LastUpdate
        {
            get { return _LastUpdate; }
            set
            {
                if (value != _LastUpdate)
                {
                    _LastUpdate = value;
                    RaisePropertyChanged("LastUpdate");
                }
            }
        }

        private String _name;
        public String Name
        {
            get { return _name; }
            set {
                if (value != _name)
                {
                    _name = value;
                }
            }
        }

        /// <summary>
        /// Two Subscription are equal if have the same Name
        /// </summary>
        /// <param name="obj"></param>
        public override bool Equals(object obj)
        {
            AzureSubscription target = obj as AzureSubscription;
            if (target != null)
                return target.Name == this.Name;

            return false;
        }	
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToString()
        {
            return Name;
        }

        public async void RetrieveAllAsync()
        {
            LastUpdate = DateTime.Now;
            await Controller.RetrieveAllAsync(this);
        }

        public async void StartAll() {
            await Controller.StartAllAsync(this);
        }
        
        public async void StopAll()
        {
            await Controller.StopAllAsync(this);
        }

    }
}

