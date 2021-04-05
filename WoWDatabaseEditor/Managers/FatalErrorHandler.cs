using System;
using System.IO;

namespace WoWDatabaseEditorCore.Managers
{
    public class FatalErrorHandler
    {
        private static string APPLICATION_FOLDER = "WoWDatabaseEditor";
        private static string LOG_FILE = "WDE.log.txt";
        private static string LOG_FILE_OLD = "WDE.log.old.txt";
        
        private static string FATAL_LOG_FILE => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APPLICATION_FOLDER, LOG_FILE);
        private static string FATAL_LOG_FILE_OLD => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APPLICATION_FOLDER, LOG_FILE_OLD);
        
        public static void ExceptionOccured(Exception e)
        {
            Console.WriteLine(e.ToString());

            var deploymentVersion = File.Exists("app.ini") ? File.ReadAllText("app.ini") : "unknown app data";
            File.WriteAllText(FATAL_LOG_FILE,  e + "\n\n" + deploymentVersion);
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