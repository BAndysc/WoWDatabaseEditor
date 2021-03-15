using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace Updater
{
    class Program
    {
        static void Main(string[] args)
        {
            var excludeFiles = new HashSet<string>()
            {
                new FileInfo("Updater").FullName,
                new FileInfo("Updater.exe").FullName,
                new FileInfo("Updater.dll").FullName,
                new FileInfo("Updater.pdb").FullName,
                new FileInfo("update.zip").FullName,
            };
            var executable = FindExecutable();

            if (executable == null)
                return;
            
            var updateFile = new FileInfo("update.zip");
            
            if (!updateFile.Exists)
            {
                LaunchWowDatabaseEditor(executable);
                return;
            }

            if (!WaitUntilCantWriteExecutable(executable))
            {
                LaunchWowDatabaseEditor(executable);
                Console.WriteLine("Cannot overwrite executable. Canceling update");
                return;
            }

            var temporaryFolder = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            temporaryFolder.Create();
            
            var dir = new DirectoryInfo(".");
            ZipFile.ExtractToDirectory(updateFile.FullName, temporaryFolder.FullName, true);
            
            var diff = new DirectoryDiffer().GenerateDiff(dir, temporaryFolder).Where(f => !excludeFiles.Contains(f.FullName)).ToList();
            foreach (var fileDir in diff)
            {
                if (fileDir is DirectoryInfo directory)
                    directory.Delete(true);
                else
                    fileDir.Delete();
            }
            
            temporaryFolder.Delete(true);
            
            ZipFile.ExtractToDirectory(updateFile.FullName, dir.FullName, true);
            File.Delete("update.zip");

            LaunchWowDatabaseEditor(executable);
        }
        
        private static bool WaitUntilCantWriteExecutable(string executable)
        {
            int tries = 0;
            while (tries < 10)
            {
                if (TryOpenForWrite(executable))
                    return true;
                
                Thread.Sleep(1000);

                tries++;
            }

            return false;
        }

        private static bool TryOpenForWrite(string executable)
        {
            try
            {
                using var f = File.OpenWrite(executable);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private static string? FindExecutable()
        {
            if (File.Exists("WoWDatabaseEditorCore.WPF.exe"))
                return "WoWDatabaseEditorCore.WPF.exe";
            
            if (File.Exists("WoWDatabaseEditorCore.Avalonia.exe"))
                return "WoWDatabaseEditorCore.Avalonia.exe";
            
            if (File.Exists("WoWDatabaseEditorCore.Avalonia"))
                return "WoWDatabaseEditorCore.Avalonia";

            return null;
        }
        
        private static void LaunchWowDatabaseEditor(string executable)
        {
            Process.Start(executable);
        }
    }
}