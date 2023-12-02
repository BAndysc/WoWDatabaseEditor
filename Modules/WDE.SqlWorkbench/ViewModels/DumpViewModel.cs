using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.Services.SqlDump;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class DumpViewModel : ObservableBase, IWindowViewModel
{
    private readonly IMySqlDumpService mySqlDumpService;
    private readonly IMySqlConnector mySqlConnector;
    private readonly DatabaseCredentials credentials;
    private readonly IMessageBoxService messageBoxService;

    [Notify] private bool isLoading;
    
    [Notify] private string? outputFile;

    [Notify] private bool dumpInProgress;
    
    [Notify] private double estimatedSize;

    [Notify] private double progressValue;
    
    public ObservableCollection<DumpBoolOptionViewModel> BoolOptions { get; } = new();
    
    public IAsyncCommand DumpCommand { get; }

    public IAsyncCommand PickFileCommand { get; }
    
    public ICommand SelectAllTablesCommand { get; }
    
    public ICommand SelectNoTableCommand { get; }
    
    public string SchemaName => credentials.SchemaName;

    public ObservableCollection<DumpTableViewModel> Tables { get; } = new();
    
    public DumpViewModel(IMySqlDumpService mySqlDumpService,
        IMySqlConnector mySqlConnector,
        IWindowManager windowManager,
        DatabaseCredentials credentials, 
        IMessageBoxService messageBoxService,
        string? tableName)
    {
        this.mySqlDumpService = mySqlDumpService;
        this.mySqlConnector = mySqlConnector;
        this.credentials = credentials;
        this.messageBoxService = messageBoxService;
        
        var fields = typeof(MySqlDumpOptions).GetFields();
        MySqlDumpOptions defaultOptions = new MySqlDumpOptions();
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(bool))
            {
                var defaultValue = (bool)field.GetValueDirect(__makeref(defaultOptions))!;
                var option = new DumpBoolOptionViewModel(field) { IsChecked = defaultValue };
                BoolOptions.Add(option);
            }
        }

        PickFileCommand = new AsyncAutoCommand(async () =>
        {
            if (await windowManager.ShowSaveFileDialog("SQL file|sql|Text file|txt|All files|*") is { } file)
            {
                OutputFile = file;
            }
        });

        SelectAllTablesCommand = new DelegateCommand(() =>
        {
            foreach (var table in Tables)
                table.IsChecked = true;
        });
        
        SelectNoTableCommand = new DelegateCommand(() =>
        {
            foreach (var table in Tables)
                table.IsChecked = false;
        });
        
        DumpCommand = new AsyncAutoCommand(DumpAsync, () => OutputFile != null);

        ListTablesAsync(tableName).ListenErrors();

        AutoDispose(Tables.ToStream(true)
            .SubscribeAction(x =>
            {
                if (x.Type == CollectionEventType.Add)
                    x.Item.PropertyChanged += OnTablePropertyValueChanged;
                else if (x.Type == CollectionEventType.Remove)
                    x.Item.PropertyChanged -= OnTablePropertyValueChanged;
            }));
        
        On(() => OutputFile, _ => DumpCommand.RaiseCanExecuteChanged());
    }

    private void OnTablePropertyValueChanged(object? sender, PropertyChangedEventArgs e)
    {
        // mysqldump doesn't provide any progress, the only way to simulate it (very roughly) is to estimate size of the dump
        // https://stackoverflow.com/questions/4852933/does-mysqldump-support-a-progress-bar
        // the 0.6 is a very rough estimate of the output file to data length ratio
        EstimatedSize = Tables.Where(x => x.IsChecked).Sum(x => x.DataLength ?? 0) * 0.6;
    }

    private async Task ListTablesAsync(string? tableToSelect)
    {
        try
        {
            IsLoading = true;
            var session = await mySqlConnector.ConnectAsync(credentials);
            var tables = await session.GetTablesAsync(credentials.SchemaName);
            
            foreach (var table in tables)
            {
                var vm = new DumpTableViewModel(table.Name,  table.DataLength / 1024.0 / 1024.0);
                if (tableToSelect == null || table.Name == tableToSelect)
                    vm.IsChecked = true;
                Tables.Add(vm);
            }

            OnTablePropertyValueChanged(default, default!);
        }
        catch (Exception e)
        {
            await messageBoxService.SimpleDialog("Error", "Can't fetch tables", e.Message);
            Console.WriteLine(e);
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
            DumpInProgress = true;
            MySqlDumpOptions options = default;
            foreach (var option in BoolOptions)
                option.Apply(ref options);

            var selectedTables = Tables.Where(x => x.IsChecked).Select(x => x.Name).ToArray();
            
            await mySqlDumpService.DumpDatabaseAsync(credentials,
                options,
                Tables.Select(x => x.Name).ToArray(),
                selectedTables, 
                outputFile!, 
                count => ProgressValue += count / 1024.0 / 1024.0,
                default);
        }
        finally
        {
            DumpInProgress = false;
        }
    }

    public int DesiredWidth => 800;
    public int DesiredHeight => 650;
    public string Title => "Dump database";
    public bool Resizeable => true;
    public ImageUri? Icon => new ImageUri("Icons/icon_accept_transaction.png");
}

internal partial class DumpTableViewModel : ObservableBase
{
    [Notify] private bool isChecked;
    public double? DataLength { get; }

    public DumpTableViewModel(string name, double? dataLength)
    {
        Name = name;
        DataLength = dataLength;
    }

    public string Name { get; }
}