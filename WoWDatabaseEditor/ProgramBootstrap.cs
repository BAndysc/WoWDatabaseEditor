using System.Diagnostics;
using System.IO;

namespace WoWDatabaseEditorCore
{
    public static class ProgramBootstrap
    {
        public static bool TryLaunchUpdaterIfNeeded()
        {
            if (!File.Exists("update.zip"))
                return false;
            
            if (!TryLaunch("Updater.exe"))
                return TryLaunch("Updater");
            
            return true;
        }
        
        private static bool TryLaunch(string file)
        {
            if (File.Exists(file))
            {
                Process.Start(file);
                return true;
            }

            return false;
        }
    }
}