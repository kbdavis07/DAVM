using System.ComponentModel;

namespace DAVM.Model
{

    public abstract class ModelBase : INotifyPropertyChanged
    {

        private bool _isWorking = false;
        public bool IsWorking
        {
            get { return _isWorking; }
            set
            {
                if (value != _isWorking)
                {
                    _isWorking = value;
					
					RaisePropertyChanged("IsWorking");
                    RaisePropertyChanged("IsIdle");
                }
            }
        }

        public bool IsIdle
        {
            get { return !_isWorking; }
        }  

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged.BeginInvoke(this, new PropertyChangedEventArgs(propertyName), null, null);
        }
       
    }
}
