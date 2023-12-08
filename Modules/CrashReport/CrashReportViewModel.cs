using System;
using System.IO;

namespace CrashReport;

public class CrashReportViewModel
{
    public string CrashReport { get; }

    private static string APPLICATION_FOLDER = "WoWDatabaseEditor";
    private static string LOG_FILE = "WDE.log.txt";
    private static string LOG_FILE_OLD = "WDE.log.old.txt";
        
    private static string WDE_DATA_FOLDER => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APPLICATION_FOLDER);
    private static string FATAL_LOG_FILE => Path.Join(WDE_DATA_FOLDER, LOG_FILE);
    private static string FATAL_LOG_FILE_OLD => Path.Join(WDE_DATA_FOLDER, LOG_FILE_OLD);
    
    public CrashReportViewModel()
    {
        if (File.Exists(FATAL_LOG_FILE))
            CrashReport = File.ReadAllText(FATAL_LOG_FILE);
        else
            CrashReport = "(crash log not found :/)";
    }
}