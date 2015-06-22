using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAVM.Model
{
    public abstract class BindableObject : ObservableObject
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

        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;

                    RaisePropertyChanged("IsSelected");
                }
            }
        }
    }
}
