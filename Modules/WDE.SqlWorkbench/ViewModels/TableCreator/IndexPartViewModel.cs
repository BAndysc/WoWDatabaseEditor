using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PropertyChanged.SourceGenerator;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class IndexPartViewModel : ObservableBase
{
    [Notify] private ColumnViewModel? column;
    [Notify] private bool descending;
    [Notify] [AlsoNotify(nameof(HasLength))] private long? length;

    private ShowIndexEntry? originalIndexInfo;
    
    private ColumnViewModel? originalColumn;

    public bool IsModified => originalIndexInfo is { } info &&
                              (info.SubPart != length ||
                               (info.Ascending == false) != descending ||
                               column != originalColumn);
    
    public IndexPartViewModel()
    {
        
    }
    
    public IndexPartViewModel(IReadOnlyList<ColumnViewModel> columns, ShowIndexEntry part)
    {
        originalIndexInfo = part;
        Column = originalColumn = columns.FirstOrDefault(x => x.ColumnName == part.ColumnName);
        Descending = part.Ascending == false;
        Length = part.SubPart;
    }

    public override string ToString()
    {
        if (column == null)
            return "????";
        
        string text = $"`{column.ColumnName}`";

        if (HasLength)
            text += $"({Length})";

        if (Descending)
            text += " DESC";

        return text;
    }

    public bool HasLength
    {
        get => length != null;
        set
        {
            if (value)
            {
                if (length == null)
                    Length = 1;
            }
            else
                Length = null;
        }
    }
    
    public IndexPartViewModel Clone()
    {
        return new IndexPartViewModel()
        {
            Column = Column,
            Descending = Descending,
            Length = Length
        };
    }
}