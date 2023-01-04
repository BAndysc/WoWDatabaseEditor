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
using WDE.MVVM.Observable;

namespace WDE.DatabaseDefinitionEditor.Views.Controls;

public class DatabaseColumnCompletionBox : CompletionComboBox
{
    public static readonly StyledProperty<string?> TableNameProperty = AvaloniaProperty.Register<DatabaseColumnCompletionBox, string?>(nameof(TableName));
    protected override Type StyleKeyOverride => typeof(CompletionComboBox);

    public string? TableName
    {
        get => GetValue(TableNameProperty);
        set => SetValue(TableNameProperty, value);
    }

    public bool CanSelectEmpty
    {
        get => (bool)GetValue(CanSelectEmptyProperty);
        set => SetValue(CanSelectEmptyProperty, value);
    }

    public string ColumnName
    {
        get => (SelectedItem as MySqlDatabaseColumn?)?.ColumnName ?? "";
        set
        {
            if ((SelectedItem as MySqlDatabaseColumn?)?.ColumnName != value)
            {
                var item = TryFindColumnByName(value);
                if (item.HasValue)
                    SelectedItem = item;
                else
                {
                    SelectedItem = new MySqlDatabaseColumn() { ColumnName = value, ManagedType = typeof(Unit) };
                    PopulateAndTryMatchColumn(value).ListenErrors();
                }
            }
        }
    }

    public DataDatabaseType Database
    {
        get => GetValue(DatabaseProperty);
        set => SetValue(DatabaseProperty, value);
    }

    private MySqlDatabaseColumn? TryFindColumnByName(string columnName)
    {
        return Items?.Cast<MySqlDatabaseColumn?>()
            .FirstOrDefault(x => x.HasValue && x.Value.ColumnName == columnName);
    }
    
    static DatabaseColumnCompletionBox()
    {
        IsDropDownOpenProperty.Changed.AddClassHandler<DatabaseColumnCompletionBox>((cb, x) =>
        {
            if (cb.IsDropDownOpen)
                cb.Populate();
        });
        SelectedItemProperty.Changed.AddClassHandler<DatabaseColumnCompletionBox>((cb, x) =>
        {
            var old = x.OldValue as MySqlDatabaseColumn?;
            var @new = x.NewValue as MySqlDatabaseColumn?;
            cb.RaisePropertyChanged(ColumnNameProperty, old?.ColumnName ?? "",  @new?.ColumnName ?? "");
        });
    }

    private CancellationTokenSource? cts;
    public static readonly StyledProperty<bool> CanSelectEmptyProperty = AvaloniaProperty.Register<DatabaseColumnCompletionBox, bool>(nameof(CanSelectEmpty));
    public static readonly DirectProperty<DatabaseColumnCompletionBox, string> ColumnNameProperty = AvaloniaProperty.RegisterDirect<DatabaseColumnCompletionBox, string>(nameof(ColumnName), o => o.ColumnName, (o, v) => o.ColumnName = v, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<DataDatabaseType> DatabaseProperty = AvaloniaProperty.Register<DatabaseColumnCompletionBox, DataDatabaseType>(nameof(Database));
    
    private async Task<IList<MySqlDatabaseColumn>> GetColumnsAsync()
    {
        var worldMysqlExecutor = ViewBind.ResolveViewModel<IMySqlExecutor>();
        var hotfixMySqlExecutor = ViewBind.ResolveViewModel<IMySqlHotfixExecutor>();

        IList<MySqlDatabaseColumn> columns;
        if (Database == DataDatabaseType.World)
            columns = await worldMysqlExecutor.GetTableColumns(TableName!);
        else if (Database == DataDatabaseType.Hotfix)
            columns = await hotfixMySqlExecutor.GetTableColumns(TableName!);
        else
            throw new ArgumentOutOfRangeException(nameof(Database));
        return columns;
    }
    
    private async Task PopulateAsync(CancellationToken token)
    {
        var tableName = TableName;
        if (string.IsNullOrEmpty(tableName))
            return;

        var columns = await GetColumnsAsync();
        if (token.IsCancellationRequested)
            return;
        
        if (CanSelectEmpty)
            columns.Insert(0, new MySqlDatabaseColumn(){ ColumnName = "", DatabaseType = "", ManagedType = typeof(Unit)});
        
        Items = columns;

        cts = null;
    }
    
    private void Populate()
    {
        if (string.IsNullOrEmpty(TableName))
            return;

        cts?.Cancel();
        cts = new CancellationTokenSource();
        PopulateAsync(cts.Token).ListenErrors();
    }

    private async Task PopulateAndTryMatchColumn(string value)
    {
        if (string.IsNullOrEmpty(TableName))
            return;
        
        cts?.Cancel();
        cts = new CancellationTokenSource();
        await PopulateAsync(cts.Token);
        
        var item = TryFindColumnByName(value);
        // if the user hasn't changed the column yet
        if ((SelectedItem as MySqlDatabaseColumn?)?.ColumnName == value)
        {
            if (item.HasValue)
                SelectedItem = item;
            else
                SelectedItem = new MySqlDatabaseColumn() { ColumnName = value };
        }
    }

    public DatabaseColumnCompletionBox()
    {
        OnEnterPressed += (_, e) =>
        {
            if (e.SelectedItem == null)
            {
                ColumnName = e.SearchText;
                e.Handled = true;
            }
        };
        this.GetResource<IDataTemplate>("MySqlColumnDataTemplate", null!, out var template);
        ItemTemplate = ButtonItemTemplate = template;
    }
}