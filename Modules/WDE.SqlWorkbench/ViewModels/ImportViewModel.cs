using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.Services.SqlDump;
using WDE.SqlWorkbench.Services.SqlImport;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class ImportViewModel : ObservableBase, IWindowViewModel, IClosableDialog
{
    private readonly IMySqlImportService mySqlImportService;
    private readonly IMySqlConnector mySqlConnector;
    private readonly DatabaseCredentials credentials;
    private readonly IMessageBoxService messageBoxService;

    [Notify] private bool eachFileInNewSession;

    [Notify] private bool saveTempFile;

    [Notify] private bool autoDeleteSaveTempFile = true;

    [Notify] private bool isLoading;

    [Notify] private bool importInProgress;

    [Notify] private double totalSize;

    [Notify] private double progressValue;

    [Notify] private string databaseVersion = "";

    [Notify] [AlsoNotify(nameof(HasAnyConsoleOutput))] private string consoleOutput = "";

    [Notify] private string? lastTempFilePath;

    private CancellationTokenSource? pendingImport;

    public bool HasAnyConsoleOutput => !string.IsNullOrEmpty(consoleOutput);

    public IAsyncCommand ImportCommand { get; }

    public ICommand CloseCommand { get; }

    public string SchemaName => credentials.SchemaName;

    public ObservableCollection<ImportFileViewModel> Files { get; } = new();

    public ICommand OpenTempFile { get; }

    public void OnClose()
    {
        async Task AskToCancel()
        {
            if (await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Question")
                    .SetMainInstruction("Import in progress")
                    .SetContent("Do you want to cancel the import?")
                    .WithYesButton(true)
                    .WithCancelButton(false)
                    .Build()))
            {
                pendingImport?.Cancel();
                Close?.Invoke();
            }
        }

        if (pendingImport != null)
        {
            AskToCancel().ListenErrors();
        }
        else
            Close?.Invoke();
    }

    public ImportViewModel(IMySqlImportService mySqlImportService,
        IMySqlConnector mySqlConnector,
        IWindowManager windowManager,
        DatabaseCredentials credentials,
        IMessageBoxService messageBoxService)
    {
        this.mySqlImportService = mySqlImportService;
        this.mySqlConnector = mySqlConnector;
        this.credentials = credentials;
        this.messageBoxService = messageBoxService;

        CloseCommand = new DelegateCommand(OnClose);

        ClearAllCommand = new DelegateCommand(() =>
        {
            Files.Clear();
        });

        OpenTempFile = new DelegateCommand(() =>
        {
            if (LastTempFilePath != null)
                windowManager.RevealFile(LastTempFilePath);
        }, () => LastTempFilePath != null).ObservesProperty(() => LastTempFilePath);

        AddFileCommand = new AsyncAutoCommand(async () =>
        {
            if (await windowManager.ShowOpenFileDialog("SQL file|sql|Text file|txt|All files|*") is { } file)
            {
                AddFile(file);
            }
        });

        AddFolderCommand = new AsyncAutoCommand(async () =>
        {
            if (await windowManager.ShowFolderPickerDialog(Environment.CurrentDirectory) is { } folder)
            {
                AddDirectory(folder);
            }
        });


        ImportCommand = new AsyncAutoCommand(DumpAsync, () => true)
            .WrapMessageBox<Exception>(messageBoxService);

        GetDatabseVersionAsync().ListenErrors();
    }

    public void AddDirectory(string folder)
    {
        var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
            .Where(x => x.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) ||
                        x.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)).ToList();

        files.Sort(Comparer<string>.Create((a, b) => String.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.Ordinal)));

        foreach (var file in files)
        {
            Files.Add(new ImportFileViewModel(new FileInfo(file)));
        }
    }

    public void AddFile(string file)
    {
        Files.Add(new ImportFileViewModel(new FileInfo(file)));
    }

    private async Task GetDatabseVersionAsync()
    {
        try
        {
            IsLoading = true;
            var session = await mySqlConnector.ConnectAsync(credentials, QueryExecutionSafety.AskUnlessSelect);

            var versions = await session.ExecuteSqlAsync("SHOW VARIABLES LIKE \"%version%\"");
            var variableNames = (StringColumnData)versions.Columns[0]!;
            var variableValues = (StringColumnData)versions.Columns[1]!;

            var indexOfVersion = Enumerable
                .Range(0, versions.AffectedRows)
                .FirstOrDefault(x => variableNames[x] == "version");

            var isMaria = Enumerable
                .Range(0, versions.AffectedRows)
                .Any(x => variableValues[x]!.Contains("maria", StringComparison.OrdinalIgnoreCase));

            var version = variableValues[indexOfVersion]!;

            DatabaseVersion = isMaria ?
                (version.Contains("maria", StringComparison.OrdinalIgnoreCase) ? version : "Maria DB " + version)
                : "MySQL " + version;
        }
        catch (Exception e)
        {
            await messageBoxService.SimpleDialog("Error", "Can't fetch database version", e.Message);
            LOG.LogError(e, "Can't fetch database version");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DumpAsync()
    {
        try
        {
            IsLoading = true;
            ImportInProgress = true;

            LastTempFilePath = !eachFileInNewSession && saveTempFile ? Path.GetTempFileName() : null;
            HashSet<FileInfo> alreadyLoggedFiles = new();

            ProgressValue = 0;
            TotalSize = Files.Where(x => x.IsChecked).Sum(x => x.SizeBytes);

            Files.Each(x => x.State = MySqlFileImportState.NotStarted);

            pendingImport = new();
            ConsoleOutput = "";
            await mySqlImportService.ImportToDatabaseAsync(credentials,
                DatabaseVersion.Contains("maria", StringComparison.OrdinalIgnoreCase)
                    ? MySqlToolsVersion.MariaDb
                    : MySqlToolsVersion.MySql,
                Files.Where(x => x.IsChecked).Select(x => x.Info).ToArray(),
                eachFileInNewSession,
                LastTempFilePath,
                fileImported =>
                {
                    if (fileImported >= 0 && fileImported < Files.Count)
                        Files[fileImported].State = MySqlFileImportState.Finished;
                },
                count => ProgressValue += count,
                (err, currentFile) =>
                {
                    if (err.Contains("[Warning] Using a password on the command line interface can be insecure."))
                        return;
                    currentFile ??= lastTempFilePath == null ? null : new FileInfo(lastTempFilePath);
                    if (currentFile != null)
                    {
                        if (alreadyLoggedFiles.Add(currentFile))
                        {
                            if (Files.FirstOrDefault(f => f.Info == currentFile) is { } fileVm)
                                fileVm.State = MySqlFileImportState.Failed;
                            LOG.LogError("In file: " + currentFile.FullName);
                            ConsoleOutput += "In file: " + currentFile.FullName + "\n";
                        }
                    }
                    else
                    {
                        ConsoleOutput += "Enable 'Save a temporary file' option to find what file failed to import.\n";
                    }

                    LOG.LogError(err);
                    ConsoleOutput += err + "\n";
                },
                pendingImport.Token);

            if (autoDeleteSaveTempFile && File.Exists(lastTempFilePath))
                File.Delete(lastTempFilePath);
        }
        catch (Exception)
        {
            if (!eachFileInNewSession)
            {
                // this property might not reflect the reality when importer was writing faster than the mysql was reading, so better clear it completely
                Files.Each(x => x.State = MySqlFileImportState.NotStarted);
            }
            throw;
        }
        finally
        {
            pendingImport = null;
            ImportInProgress = false;
            IsLoading = false;
        }
    }

    public int DesiredWidth => 800;
    public int DesiredHeight => 650;
    public string Title => "Import to database";
    public bool Resizeable => true;
    public ImageUri? Icon => new ImageUri("Icons/icon_accept_transaction.png");
    public ICommand AddFileCommand { get; }
    public ICommand AddFolderCommand { get; }
    public ICommand ClearAllCommand { get; }

    public event Action? Close;
}

internal partial class ImportFileViewModel : ObservableBase
{
    [Notify] private bool isChecked;
    [Notify] private MySqlFileImportState state;
    public long SizeBytes { get; }
    public double SizeMegaBytes { get; }
    public FileInfo Info { get; }

    public ImportFileViewModel(FileInfo file)
    {
        Info = file;
        Name = file.Name;
        SizeBytes = file.Length;
        SizeMegaBytes = file.Length / 1024.0 / 1024.0;
        IsChecked = true;
    }

    public string Name { get; }
}

internal enum MySqlFileImportState
{
    NotStarted,
    Finished,
    Failed
}