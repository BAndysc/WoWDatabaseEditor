using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AsyncAwaitBestPractices.MVVM;
using PropertyChanged.SourceGenerator;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.SqlDump;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class DumpViewModel : ObservableBase, IWindowViewModel
{
    private readonly IMySqlDumpService mySqlDumpService;
    private readonly DatabaseCredentials credentials;
    private readonly string tableName;

    [Notify] private string? outputFile;

    [Notify] private bool dumpInProgress;
    
    public ObservableCollection<DumpBoolOptionViewModel> BoolOptions { get; } = new();
    
    public IAsyncCommand DumpCommand { get; }

    public IAsyncCommand PickFileCommand { get; }
    
    public string SchemaName => credentials.SchemaName;
    public string TableName => tableName;
    
    public DumpViewModel(IMySqlDumpService mySqlDumpService,
        IWindowManager windowManager,
        DatabaseCredentials credentials, 
        string tableName)
    {
        this.mySqlDumpService = mySqlDumpService;
        this.credentials = credentials;
        this.tableName = tableName;
        
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

        DumpCommand = new AsyncAutoCommand(DumpAsync, () => OutputFile != null);

        On(() => OutputFile, _ => DumpCommand.RaiseCanExecuteChanged());
    }

    private async Task DumpAsync()
    {
        try
        {
            DumpInProgress = true;
            MySqlDumpOptions options = default;
            foreach (var option in BoolOptions)
                option.Apply(ref options);

            await mySqlDumpService.DumpDatabaseAsync(credentials, options, new[] { tableName }, outputFile!, default);
        }
        finally
        {
            DumpInProgress = false;
        }
    }

    public int DesiredWidth => 500;
    public int DesiredHeight => 850;
    public string Title => "Dump table";
    public bool Resizeable => true;
    public ImageUri? Icon => new ImageUri("Icons/icon_accept_transaction.png");
}