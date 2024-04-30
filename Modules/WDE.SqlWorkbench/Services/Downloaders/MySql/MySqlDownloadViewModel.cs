using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Disposables;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WDE.SqlWorkbench.Services.Downloaders.MySql;

internal partial class MySqlDownloadViewModel : ObservableBase, IDialog, ITaskProgress
{
    private readonly IMySqlDownloader mySqlDbDownloader;
    private readonly IMessageBoxService messageBoxService;
    private readonly IFileSystem vfs;
    private readonly IMainThread mainThread;
    [Notify] private int downloadProgress;
    [Notify] private int downloadLength;
    [Notify] private bool isDownloading;
    [Notify] private bool isFetchingVersions;
    [Notify] private MySqlRelease? selectedMySqlDbVersion;
    [Notify] private string downloadText = "Download";
    [Notify] private bool isDownloaded;
    public ObservableCollection<MySqlRelease> MySqlVersions { get; } = new();

    public IAsyncCommand DownloadSelectedVersionCommand { get; }
    
    public string? DumpDownloadPath { get; private set; }
    public string? MySqlDownloadPath { get; private set; }

    private CancellationTokenSource? cts;
    
    public ICommand OpenLicenseCommand { get; }
    
    public MySqlDownloadViewModel(IMySqlDownloader mySqlDbDownloader,
        IMessageBoxService messageBoxService,
        IFileSystem vfs,
        IWindowManager windowManager,
        IMainThread mainThread)
    {
        this.mySqlDbDownloader = mySqlDbDownloader;
        this.messageBoxService = messageBoxService;
        this.vfs = vfs;
        this.mainThread = mainThread;
        AutoDispose(new ActionDisposable(() => cts?.Cancel()));
        var accept = new DelegateCommand(() =>
        {
            CloseOk?.Invoke();
        }, () => !IsDownloading && !IsFetchingVersions && SelectedMySqlDbVersion != null && IsDownloaded);
        Accept = accept;
        Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
        
        OpenLicenseCommand = new DelegateCommand(() =>
        {
            windowManager.OpenUrl("https://downloads.mysql.com/docs/licenses/mysqld-8.0-gpl-en.pdf"); 
        });
        
        DownloadSelectedVersionCommand = new AsyncCommand(async () =>
        {
            try
            {
                IsDownloading = true;
                cts = new();
                IsDownloaded = false;
                var path = new DirectoryInfo(vfs.ResolvePhysicalPath("/common/tools/").FullName);
                if (!path.Exists)
                    path.Create();

                var (dump, mysql) = await mySqlDbDownloader.DownloadMySqlAsync(SelectedMySqlDbVersion!.Value.Id, path.FullName, this, cts.Token);
                DumpDownloadPath = dump;
                MySqlDownloadPath = mysql;

                IsDownloaded = true;
                cts = null;
            }
            finally
            {
                IsDownloading = false;
                DownloadText = "Download";
            }
        }, _ => !IsDownloading && !IsFetchingVersions && SelectedMySqlDbVersion != null)
        .WrapMessageBox<Exception>(messageBoxService);

        PropertyChanged += (_, _) =>
        {
            accept.RaiseCanExecuteChanged();
            DownloadSelectedVersionCommand.RaiseCanExecuteChanged();
        };
        
        FetchVersionsAsync().ListenErrors();
    }

    private async Task FetchVersionsAsync()
    {
        try
        {
            IsFetchingVersions = true;
            var releases = await mySqlDbDownloader.GetReleasesAsync();
            MySqlVersions.AddRange(releases);
            SelectedMySqlDbVersion = MySqlVersions.Count == 0 ? null : MySqlVersions[0];
        }
        catch (Exception e)
        {
            await messageBoxService.SimpleDialog("Error", "Can't fetch MySql versions", e.Message);
        }
        finally
        {
            IsFetchingVersions = false;
        }
    }
    
    public int DesiredWidth => 400;
    public int DesiredHeight => 300;
    public string Title => "MySql DB Downloader";
    public bool Resizeable => true;
    public ICommand Accept { get; }
    public ICommand Cancel { get; }
    public event Action? CloseCancel;
    public event Action? CloseOk;
    
    public TaskState State { get; }
    public int CurrentProgress { get; }
    public int MaxProgress { get; }
    public string? CurrentTask { get; }

    public void Report(int currentProgress, int maxProgress, string? currentTask)
    {
        mainThread.Dispatch(() =>
        {
            DownloadProgress = currentProgress;
            DownloadLength = maxProgress;
            
            var downloadedMegabytes = (double)currentProgress / 1024 / 1024;
            var totalMegabytes = (double)maxProgress / 1024 / 1024;
            DownloadText = $"{downloadedMegabytes:0.00}/{totalMegabytes:0.00} MB";
        });
    }

    public void ReportFinished()
    {
    }

    public void ReportFail()
    {
    }

    public event Action<ITaskProgress>? Updated;
}