using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Reflection;

namespace DAVM.ViewModels
{
    public class AboutViewModel : ViewModelBase
    {
        #region Properties

        public event EventHandler CloseRequested;

        public String Version
        {
            get {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            set
            {
            }
        }

        #endregion

        public AboutViewModel()
        {            
            
        }

        #region Commands

        private RelayCommand _CmdClose;
        public RelayCommand CmdClose
        {
            get
            {
                if (_CmdClose == null)
                {
                    _CmdClose = new RelayCommand(() =>
                    {
                        if (CloseRequested != null)
                            CloseRequested(this, null);
                    }, () => true);
                }
                return _CmdClose;
            }
        }

        #endregion 
    }
}