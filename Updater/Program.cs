using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace Updater
{
    class Program
    {
        private static void PrintError(Exception? e, string message)
        {
            if (e != null)
                Console.WriteLine(e.Message);
            Console.WriteLine("@@@@@@@@@@@@@@@@@");
            Console.WriteLine("@");
            Console.WriteLine("@");
            Console.WriteLine("@    ERROR WHILE INSTALLING THE WoW Database Editor UPDATE");
            Console.WriteLine("@");
            Console.WriteLine("@         " + message);
            Console.WriteLine("@");
            Console.WriteLine("@@@@@@@@@@@@@@@@@");
        }

        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine(" --- WoW Database Editor Updater ---");
            Console.WriteLine("    (sorry it is that ugly)");
            Console.WriteLine();
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
                LaunchWowDatabaseEditor(executable, args);
                return;
            }

            if (FindOldFiles())
            {
                PrintError(null, "There are some .old files in the editor directory. It means you have some custom changes to the editor files. Commit them or delete manually and run the update.exe again");
                Thread.Sleep(3000);
                LaunchWowDatabaseEditor(executable, args.Concat(new[]{"--skip-update"}).ToArray());
                Console.ReadKey();
                return;
            }

            if (!WaitUntilCantWriteExecutable(executable, out var exception))
            {
                PrintError(exception, "Can't write the .EXE file. Please close the WoW Database Editor and try again (No update has been installed).");
                Thread.Sleep(10000);
                LaunchWowDatabaseEditor(executable, args);
                return;
            }

            var temporaryFolder = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            temporaryFolder.Create();
            
            var dir = new DirectoryInfo(".");
            try
            {
                ZipFile.ExtractToDirectory(updateFile.FullName, temporaryFolder.FullName, true);
            }
            catch (Exception e)
            {
                PrintError(e, "will rollback to the previous version, please try to download the update again");
                File.Delete("update.zip");
                Console.ReadKey();
                return;
            }
            
            var diff = new DirectoryDiffer().GenerateDiff(dir, temporaryFolder).Where(f => !excludeFiles.Contains(f.FullName)).ToList();
            try
            {
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

                LaunchWowDatabaseEditor(executable, args);
            }
            catch (Exception e)
            {
                PrintError(e, "Unfortunately your installation might be broken now. Please redownload the editor. Sorry.");
                Console.ReadKey();
            }
        }
        
        private static bool WaitUntilCantWriteExecutable(string executable, out Exception? ex)
        {
            int tries = 0;
            ex = null;
            while (tries < 10)
            {
                if (TryOpenForWrite(executable, out ex))
                    return true;
                
                Thread.Sleep(1000);

                tries++;
            }

            return false;
        }

        private static bool TryOpenForWrite(string executable, out Exception? ex)
        {
            try
            {
                using var f = File.OpenWrite(executable);
                ex = null;
                return true;
            }
            catch (Exception e)
            {
                ex = e;
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
        
        private static void LaunchWowDatabaseEditor(string executable, string[] args)
        {
            Process.Start(executable, args);
        }

        private static byte[] CalculateFileMd5(string filename)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filename);
            return md5.ComputeHash(stream);
        }

        private static bool FindOldFiles()
        {
            foreach (var fileName in Directory.EnumerateFiles(Environment.CurrentDirectory, "*.old", SearchOption.AllDirectories))
            {
                var regularFileName = fileName.Substring(0, fileName.Length - 4);
                if (!File.Exists(regularFileName))
                    continue;

                var @new = CalculateFileMd5(regularFileName);
                var old = CalculateFileMd5(fileName);

                if (!@new.SequenceEqual(old))
                    return true;
            }

            return false;
        }
    }
}