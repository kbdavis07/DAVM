using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using System;

namespace DAVM.ViewModels
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {

        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            ////if (ViewModelBase.IsInDesignModeStatic)
            ////{
            ////    // Create design time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DesignDataService>();
            ////}
            ////else
            ////{
            ////    // Create run time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DataService>();
            ////}

            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<AboutViewModel>();
            SimpleIoc.Default.Register<SettingsViewModel>();

        }

        public MainViewModel Main
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MainViewModel>();
            }
        }

        public AboutViewModel About
        {
            get
            {
                return ServiceLocator.Current.GetInstance<AboutViewModel>();
            }
        }

        public SettingsViewModel Settings
        {
            get
            {
                return ServiceLocator.Current.GetInstance<SettingsViewModel>();
            }
        }


        
        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}