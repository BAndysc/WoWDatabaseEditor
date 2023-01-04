using System;
using System.IO;
using WDE.Common;

namespace WDE.MySqlDatabaseCommon.Services;

internal class RollingLogFile
{
    private const string LogFileNameFormat = "{0:yyyy_MM_dd}_{1}.log";
    private const int MaxFileSizeInBytes = 50 * 1024 * 1024; // 50 MB
    private const int MaxFileAgeInDays = 14;

    private string logFolder;
    private string currentLogFile = "";
    private int currentFileNumber;
    private long currentLogFileSize;

    public RollingLogFile(string absoluteLogFolder)
    {
        logFolder = absoluteLogFolder;
        if (!Directory.Exists(absoluteLogFolder))
            Directory.CreateDirectory(absoluteLogFolder);
        Initialize();
    }

    public void WriteLog(string message)
    {
        if (ShouldCreateNewLogFile())
        {
            CreateNewLogFile();
        }

        try
        {
            using var writer = File.AppendText(currentLogFile);
            writer.WriteLine(message);
            currentLogFileSize += writer.Encoding.GetByteCount(message) + writer.NewLine.Length;
        }
        catch (Exception ex)
        {
            LOG.LogError($"An error occurred while writing to the log file: {ex.Message}");
        }
    }

    private void Initialize()
    {
        try
        {
            CleanupOldLogFiles();
            FindLastLogFile();

            if (string.IsNullOrEmpty(currentLogFile))
            {
                currentFileNumber = 0;
                CreateNewLogFile();
            }
            else
            {
                currentLogFileSize = new FileInfo(currentLogFile).Length;
            }
        }
        catch (Exception ex)
        {
            LOG.LogError($"An error occurred during initialization: {ex.Message}");
        }
    }

    private bool ShouldCreateNewLogFile()
    {
        return currentLogFileSize >= MaxFileSizeInBytes;
    }

    private void CreateNewLogFile()
    {
        currentFileNumber++;
        currentLogFile = Path.Combine(logFolder, string.Format(LogFileNameFormat, DateTime.Today, currentFileNumber));

        try
        {
            using (File.Create(currentLogFile))
            {
            }

            currentLogFileSize = 0;
        }
        catch (Exception ex)
        {
            LOG.LogError($"An error occurred while creating a new log file: {ex.Message}");
        }
    }

    private void CleanupOldLogFiles()
    {
        try
        {
            var directory = new DirectoryInfo(logFolder);
            var logFiles = directory.GetFiles("*.log", SearchOption.TopDirectoryOnly);
            var currentDate = DateTime.Today;

            foreach (var file in logFiles)
            {
                if (TryExtractDateFromFileName(file.Name, out var fileDate, out _) &&
                    (currentDate - fileDate).TotalDays > MaxFileAgeInDays)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        LOG.LogError($"An error occurred while deleting old log files: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LOG.LogError($"An error occurred while cleaning up old log files: {ex.Message}");
        }
    }

    private bool TryExtractDateFromFileName(string fileName, out DateTime fileDate, out int fileNumber)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var fileParts = fileNameWithoutExtension.Split('_');

        if (fileParts.Length >= 4 && int.TryParse(fileParts[0], out var year) &&
            int.TryParse(fileParts[1], out var month) &&
            int.TryParse(fileParts[2], out var day) &&
            int.TryParse(fileParts[3], out fileNumber))
        {
            fileDate = new DateTime(year, month, day);
            return true;
        }

        fileDate = default;
        fileNumber = 0;
        return false;
    }

    private void FindLastLogFile()
    {
        try
        {
            var directory = new DirectoryInfo(logFolder);
            var logFiles = directory.GetFiles("*.log", SearchOption.TopDirectoryOnly);
            DateTime lastDate = DateTime.MinValue;
            int lastFileNumber = 0;
            var today = DateTime.Today;

            foreach (var file in logFiles)
            {
                if (TryExtractDateFromFileName(file.Name, out var fileDate, out var fileNumber))
                {
                    if (fileDate.Date < today)
                        continue;
                    
                    if (fileDate > lastDate)
                    {
                        lastDate = fileDate;
                        lastFileNumber = fileNumber;
                        currentLogFile = file.FullName;
                    }
                    else if (fileDate == lastDate && fileNumber > lastFileNumber)
                    {
                        lastFileNumber = fileNumber;
                        currentLogFile = file.FullName;
                    }
                }
            }

            if (lastDate != DateTime.MinValue)
            {
                currentFileNumber = lastFileNumber;
            }
        }
        catch (Exception ex)
        {
            LOG.LogError($"An error occurred while finding the last log file: {ex.Message}");
        }
    }
}