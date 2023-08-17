using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaStyles.Controls.FastTableView;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor;

public class DemoItemGroup : ObservableBase, ITableRowGroup, ITableRow
{
    private readonly DefinitionViewModel table;
    public IReadOnlyList<ITableRow> Rows => new ITableRow[] { this };
    public event Action<ITableRowGroup, ITableRow>? RowChanged;
    public event Action<ITableRowGroup>? RowsChanged;
    public IReadOnlyList<ITableCell> CellsList => table.CellsPreview;

    public string GroupHeader => (string.IsNullOrEmpty(table.TablePrimaryKeyColumnName)
                                     ? null
                                     : table.Groups.SelectMany(g => g.Columns).FirstOrDefault(c =>
                                         c.DatabaseColumnName?.ColumnName == table.TablePrimaryKeyColumnName)?.Preview)
                                 ?? "!! you need to specify Primary Key Column Name";

    public event Action<ITableRow>? Changed;

    public DemoItemGroup(DefinitionViewModel table)
    {
        this.table = table;
        table.ToObservable(x => x.Columns)
            .SubscribeAction(columns =>
            {
                Changed?.Invoke(this);
                RowChanged?.Invoke(this, this);
                RaisePropertyChanged(nameof(GroupHeader));
            });

        table.ToObservable(x => x.TablePrimaryKeyColumnName)
            .SubscribeAction(_ => RaisePropertyChanged(nameof(GroupHeader)));
    }
}