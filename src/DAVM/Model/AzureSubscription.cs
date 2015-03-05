using Microsoft.WindowsAzure;
using System;
using System.Collections.ObjectModel;

namespace DAVM.Model
{
    public class AzureSubscription : ModelBase
    {
        public CertificateCloudCredentials CloudCredentials { get; private set; }

        public AzureSubscription(AzureVMController controller, string subscriptionID, string base64Certificate, string name) {

            if (String.IsNullOrEmpty(subscriptionID) || String.IsNullOrEmpty(base64Certificate) || String.IsNullOrEmpty(name))
                throw new ArgumentNullException();

            ID = subscriptionID;
            Controller = controller;            
            VMs = new ObservableCollection<AzureVM>();
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

        public  AzureVMController Controller { get; private set; }

        public virtual ObservableCollection<AzureVM> VMs
        {
            get;
            set;
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

        public async void RetrieveVMs()
        {
            LastUpdate = DateTime.Now;
            await Controller.RetrieveVMsAsync(this);
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

