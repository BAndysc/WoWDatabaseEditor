using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Styling;
using AvaloniaStyles.Controls;
using WDE.Common.Avalonia;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Database;
using WDE.Common.Utils;

namespace WDE.DatabaseDefinitionEditor.Views.Controls;

public class DatabaseTableCompletionBox : CompletionComboBox
{
    protected override Type StyleKeyOverride => typeof(CompletionComboBox);

    public static readonly DirectProperty<DatabaseTableCompletionBox, string> TableNameProperty = AvaloniaProperty.RegisterDirect<DatabaseTableCompletionBox, string>(nameof(TableName), o => o.TableName, (o, v) => o.TableName = v, defaultBindingMode: BindingMode.TwoWay);

    public string TableName
    {
        get => (SelectedItem as DatabaseTableViewModel)?.Name ?? "";
        set
        {
            if ((SelectedItem as DatabaseTableViewModel)?.Name != value)
            {
                var item = TryFindTable(value);
                if (item != null)
                    SelectedItem = item;
                else
                {
                    SelectedItem = new DatabaseTableViewModel(value, true);
                    PopulateAndTryMatchTable(value).ListenErrors();
                }
            }
        }
    }
    public bool CanSelectEmpty
    {
        get => (bool)GetValue(CanSelectEmptyProperty);
        set => SetValue(CanSelectEmptyProperty, value);
    }

    public DataDatabaseType Database
    {
        get => GetValue(DatabaseProperty);
        set => SetValue(DatabaseProperty, value);
    }

    private DatabaseTableViewModel? TryFindTable(string tableName)
    {
        return Items?.Cast<DatabaseTableViewModel>().FirstOrDefault(x => x.Name == tableName);
    }

    static DatabaseTableCompletionBox()
    {
        IsDropDownOpenProperty.Changed.AddClassHandler<DatabaseTableCompletionBox>((cb, x) =>
        {
            if (cb.IsDropDownOpen)
                cb.Populate();
        });
        SelectedItemProperty.Changed.AddClassHandler<DatabaseTableCompletionBox>((cb, x) =>
        {
            var old = x.OldValue as DatabaseTableViewModel;
            var @new = x.NewValue as DatabaseTableViewModel;
            cb.RaisePropertyChanged(TableNameProperty, old?.Name ?? "",  @new?.Name ?? "");
        });
    }

    private CancellationTokenSource? cts;

    public static readonly StyledProperty<bool> CanSelectEmptyProperty =
        AvaloniaProperty.Register<DatabaseTableCompletionBox, bool>(nameof(CanSelectEmpty));

    public static readonly StyledProperty<DataDatabaseType> DatabaseProperty = AvaloniaProperty.Register<DatabaseTableCompletionBox, DataDatabaseType>("Database");

    private async Task PopulateAsync(CancellationToken token)
    {
        var columns = await GetTablesAsync();

        if (token.IsCancellationRequested)
            return;

        var viewModels = columns.Select(c => new DatabaseTableViewModel(c, true))
            .ToList();

        if (CanSelectEmpty)
            viewModels.Insert(0, new DatabaseTableViewModel("", true));

        Items = viewModels;
        cts = null;
    }

    private async Task<IList<string>> GetTablesAsync()
    {
        var worldMysqlExecutor = ViewBind.ResolveViewModel<IMySqlExecutor>();
        var hotfixMySqlExecutor = ViewBind.ResolveViewModel<IMySqlHotfixExecutor>();

        IList<string> tables;
        if (Database == DataDatabaseType.World)
            tables = await worldMysqlExecutor.GetTables();
        else if (Database == DataDatabaseType.Hotfix)
            tables = await hotfixMySqlExecutor.GetTables();
        else
            throw new ArgumentOutOfRangeException(nameof(Database));
        return tables;
    }

    private void Populate()
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        PopulateAsync(cts.Token).ListenErrors();
    }
    
    private async Task PopulateAndTryMatchTable(string value)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        await PopulateAsync(cts.Token);
        
        var item = TryFindTable(value);
        // if the user hasn't changed the column yet
        if ((SelectedItem as DatabaseTableViewModel)?.Name == value)
        {
            if (item != null)
                SelectedItem = item;
            else
                SelectedItem = new DatabaseTableViewModel(value, false);
        }
    }

    public DatabaseTableCompletionBox()
    {
        OnEnterPressed += (_, e) =>
        {
            if (e.SelectedItem == null)
            {
                TableName = e.SearchText;
                e.Handled = true;
            }
        };
        this.GetResource<IDataTemplate>("MySqlTableDataTemplate", null!, out var template);
        ItemTemplate = ButtonItemTemplate = template;
    }
}

public class DatabaseTableViewModel
{
    public DatabaseTableViewModel(string name, bool existInDatabase)
    {
        Name = name;
        ExistInDatabase = existInDatabase;
    }

    public string Name { get; }
    public bool ExistInDatabase { get; }

    public override string ToString() => Name;
}