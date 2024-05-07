using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Newtonsoft.Json;
using WDE.Common.Factories;
using WDE.Common.Services;
using WDE.Common.Utils;

namespace CrashReport;

public class CrashReportViewModel : INotifyPropertyChanged
{
    private readonly IApplicationReleaseConfiguration configuration;
    private readonly IHttpClientFactory clientFactory;
    public string CrashReport { get; }

    private static string APPLICATION_FOLDER = "WoWDatabaseEditor";
    private static string LOG_FILE = "WDE.log.txt";
    private static string LOG_FILE_OLD = "WDE.log.old.txt";
        
    private static string WDE_DATA_FOLDER => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APPLICATION_FOLDER);
    private static string FATAL_LOG_FILE => Path.Join(WDE_DATA_FOLDER, LOG_FILE);
    private static string FATAL_LOG_FILE_OLD => Path.Join(WDE_DATA_FOLDER, LOG_FILE_OLD);

    private bool reportSent;
    public AsyncCommand UploadReportCommand { get; }

    public string SendCrashLogButtonText { get; private set; } = "Send the crash log";

    public CrashReportViewModel(IApplicationReleaseConfiguration configuration, IHttpClientFactory clientFactory)
    {
        this.configuration = configuration;
        this.clientFactory = clientFactory;
        if (File.Exists(FATAL_LOG_FILE))
            CrashReport = File.ReadAllText(FATAL_LOG_FILE);
        else
        {
            CrashReport = "(crash log not found :/)";
            reportSent = true;
        }

        var server = configuration.GetString("UPDATE_SERVER");
        if (string.IsNullOrEmpty(server))
            reportSent = true;

        UploadReportCommand = new AsyncCommand(async () =>
        {
            reportSent = true;
            UploadReportCommand?.RaiseCanExecuteChanged();

            var httpClient = clientFactory.Factory();
            try
            {
                await httpClient.PostAsync(server + "/Log/Send", new StringContent(JsonConvert.SerializeObject(new
                {
                    log = CrashReport
                }), new MediaTypeHeaderValue("application/json")));
                SendCrashLogButtonText = "Crash log sent!";
                OnPropertyChanged(nameof(SendCrashLogButtonText));
            }
            catch (Exception e)
            {
                SendCrashLogButtonText = "Couldn't sent the crashlog: " + e.Message;
                OnPropertyChanged(nameof(SendCrashLogButtonText));
            }
        }, _ => !reportSent);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}