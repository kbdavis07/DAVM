using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace DAVM.Common
{
    class UserSessionInitializer : IContextInitializer
    {
    
       

        public void Initialize(TelemetryContext context)
        {
            //unique ID
            context.User.Id = Environment.ProcessorCount +
                "/" + Environment.MachineName +
                "/" + Environment.UserDomainName +
                "\\" + Environment.UserName;       
            context.Session.Id = Guid.NewGuid().ToString();
        }
    }

    


    public class TelemetryHelper {
        public static String EVT_START = "START";

        public static String METRIC_SUBSCRIPTION= "SUBSCRIPTIONS";
        public static String METRIC_VM = "VM";
        public static String METRIC_WEBSITE = "WEBSITE";


    }
}
