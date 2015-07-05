using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Diagnostics;

namespace DAVM.Common
{
    public enum LogType
    {
        Error,
        Warning,
        Verbose,
        Info,
        Command
    }

    public class InfoMessage
    {
        public String Message { get; set; }
        public LogType Level { get; set; }
    }

    public class Logger
    {

        public static event EventHandler<InfoMessage> LogUpdated;

        public static void LogEntry(LogType level, string message)
        {            
            var iMessage = new InfoMessage() { Message = message, Level = level };
            LogEntry(iMessage);
        }


        private static void LogEntry(InfoMessage message)
        {
            bool notifyOther = true; //to filter out the verbose message in the UI

            var prefix = String.Empty;
            switch (message.Level)
            {
                case LogType.Error: { prefix = "[ERROR]"; break; }
                case LogType.Command: { prefix = "[COMMAND]"; notifyOther = false; break; }
                case LogType.Warning: { prefix = "[WARNING]"; break; }
                case LogType.Verbose: { prefix = "[VERBOSE]"; notifyOther = false;  break; }
                default: { prefix = "[INFO]"; break; }
            }

            if (notifyOther && LogUpdated != null)
                LogUpdated.BeginInvoke(null, message, null, null);
           
            Trace.WriteLine(String.Format("[{2}] {0} {1}", prefix, message.Message,DateTime.Now.ToLocalTime()));
        }

        public static void LogEntry(String message, Exception ex)
        {
            LogEntry(LogType.Error,message);
            LogEntry(LogType.Verbose, "EX: " + ex.Message);
            LogEntry(LogType.Verbose, "EX: " + ex.StackTrace);

            while (ex.InnerException != null)
            {
                LogEntry(new InfoMessage() { Message = ex.InnerException.Message, Level = LogType.Error });
                LogEntry(new InfoMessage() { Message = ex.InnerException.StackTrace, Level = LogType.Verbose });
                ex = ex.InnerException;
            }

            if (App.GlobalConfig.Telemetry != null)
            {
                ExceptionTelemetry exTel = new ExceptionTelemetry(ex);
                App.GlobalConfig.Telemetry.TrackException(exTel);
            }
        }

    }

}


