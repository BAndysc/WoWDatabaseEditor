using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Prism.Commands;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Updater.Client;
using WDE.Updater.Data;
using WDE.Updater.Services;

namespace CrashReport;

public class MainWindowViewModel : ITaskProgress, INotifyPropertyChanged
{
    public object MainContent { get; }

    public ICommand RestartEditorCommand { get; }
    
    public ICommand RestartEditorWithConsoleCommand { get; }
    
    public ICommand DownloadEditorCommand { get; }
    
    public MainWindowViewModel()
    {
        RestartEditorWithConsoleCommand = new DelegateCommand(() =>
        {
            var locator = new ProgramFinder();
            if (locator.TryLocateIncludingCurrentDir("WoWDatabaseEditorCore.Avalonia.exe",
                    "WoWDatabaseEditorCore.Avalonia") is { } exe)
            {
                var info = new ProcessStartInfo(exe)
                {
                    UseShellExecute = true
                };
                info.ArgumentList.Add("--console");
                info.ArgumentList.AddRange(GlobalApplication.Arguments);
                Process.Start(info);
                ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)?.Shutdown();
            }
        });
        
        RestartEditorCommand = new DelegateCommand(() =>
        {
            var locator = new ProgramFinder();
            if (locator.TryLocateIncludingCurrentDir("WoWDatabaseEditorCore.Avalonia.exe",
                    "WoWDatabaseEditorCore.Avalonia") is { } exe)
            {
                var info = new ProcessStartInfo(exe)
                {
                    UseShellExecute = true
                };
                info.ArgumentList.AddRange(GlobalApplication.Arguments);
                Process.Start(info);
                ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)?.Shutdown();
            }
        });

        IFileSystem fs = new FileSystem();
        var releaseConfig = new ApplicationReleaseConfiguration(fs);
        var applicationVersion = new ApplicationVersion(releaseConfig);
        var httpFactory = new HttpClientFactory(applicationVersion);
        var updateService = new UpdateService(new UpdateServerDataProvider(releaseConfig),
            new UpdateClientFactory(httpFactory),
            new ApplicationVersion(releaseConfig),
            new ApplicationImpl(),
            fs,
            new StandaloneUpdater(new AutoUpdatePlatformService(), fs, new MessageBoxService()),
            new SettingsProvider(), new AutoUpdatePlatformService(), new UpdateVerifier());
        
        DownloadEditorCommand = new DelegateCommand(async () =>
        {
            IsDownloading = true;
            State = TaskState.InProgress;
            var update = await updateService.CheckForUpdates(true);
            await updateService.DownloadLatestVersion(this);
            await updateService.CloseForUpdate();
        }, () => updateService.CanCheckForUpdates());


        if (GlobalApplication.Arguments.IsArgumentSet("crashed"))
        {
            MainContent = new CrashReportViewModel(releaseConfig, httpFactory);
        }
        else
        {
            MainContent = new NoCrashViewModel();
        }
    }

    public TaskState State { get; set; }
    private bool isDownloading;

    public bool IsDownloading
    {
        get => isDownloading;
        set => SetField(ref isDownloading, value);
    }

    private int currentProgress;
    public int CurrentProgress
    {
        get => currentProgress;
        set => SetField(ref currentProgress, value);
    }

    private int maxProgress = 100;
    public int MaxProgress
    {
        get => maxProgress;
        set => SetField(ref maxProgress, value);
    }
    
    public string? CurrentTask { get; }
    
    public void Report(int currentProgress, int maxProgress, string? currentTask)
    {
        Dispatcher.UIThread.Post(() =>
        {
            CurrentProgress = (int)(currentProgress * 100.0 / maxProgress);
            MaxProgress = 100;//maxProgress;
        });
    }

    public void ReportFinished()
    {
        State = TaskState.FinishedSuccess;
    }

    public void ReportFail()
    {
        State = TaskState.FinishedFailed;
    }

    public event Action<ITaskProgress>? Updated;
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