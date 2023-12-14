using System.Diagnostics;
using System.IO;

namespace WoWDatabaseEditorCore
{
    public static class ProgramBootstrap
    {
        public static bool TryLaunchUpdaterIfNeeded(string[] args)
        {
            if (!File.Exists("update.zip"))
                return false;
            
            if (!TryLaunch("Updater.exe", args))
                return TryLaunch("Updater", args);
            
            return true;
        }
        
        private static bool TryLaunch(string file, string[] args)
        {
            if (File.Exists(file))
            {
                Process.Start(file, args);
                return true;
            }

            return false;
        }
    }
}