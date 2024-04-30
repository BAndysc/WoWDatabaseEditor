using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using DynamicData;
using WDE.Common;

namespace WoWDatabaseEditorCore.Managers
{
    public class FatalErrorHandler
    {
        private static string APPLICATION_FOLDER = "WoWDatabaseEditor";
        private static string LOG_FILE = "WDE.log.txt";
        private static string LOG_FILE_OLD = "WDE.log.old.txt";
        
        private static string WDE_DATA_FOLDER => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APPLICATION_FOLDER);
        private static string FATAL_LOG_FILE => Path.Join(WDE_DATA_FOLDER, LOG_FILE);
        private static string FATAL_LOG_FILE_OLD => Path.Join(WDE_DATA_FOLDER, LOG_FILE_OLD);
        
        public static void ExceptionOccured(Exception e, string[] args)
        {
            LOG.LogCritical(e.ToString());

            if (!Directory.Exists(WDE_DATA_FOLDER))
                Directory.CreateDirectory(WDE_DATA_FOLDER);
            
            var deploymentVersion = File.Exists("app.ini") ? File.ReadAllText("app.ini") : "unknown app data";
            File.WriteAllText(FATAL_LOG_FILE,  e + "\n\n" + deploymentVersion);

            StartCrashReporter(args);
        }

        private static void StartCrashReporter(string[] args)
        {
            string? exe = null;
            if (File.Exists("CrashReport.exe"))
                exe = "CrashReport.exe";
            else if (File.Exists("CrashReport"))
                exe = "CrashReport";

            if (!string.IsNullOrEmpty(exe))
            {
                var info = new ProcessStartInfo(exe)
                {
                    UseShellExecute = true
                };
                info.ArgumentList.Add("--crashed");
                foreach (var arg in args)
                    info.ArgumentList.Add(arg);
                Process.Start(info);
            }
        }

        public static bool HasFatalLog()
        {
            return File.Exists(FATAL_LOG_FILE);
        }

        public static string ConsumeFatalLog()
        {
            if (!HasFatalLog())
                return "";

            var log = File.ReadAllText(FATAL_LOG_FILE);
            
            if (File.Exists(FATAL_LOG_FILE_OLD))
                File.Delete(FATAL_LOG_FILE_OLD);
            
            File.Move(FATAL_LOG_FILE, FATAL_LOG_FILE_OLD);

            return log;
        }
    }
}
