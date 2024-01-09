using System;
using Avalonia.Data;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Utils;

namespace WDE.SqlWorkbench.ViewModels;

internal class TimeSpanCellEditorViewModel : BaseCellEditorViewModel
{
    private readonly TimeSpanSparseColumnData overrideData;
    
    private TimeSpan time;
    public string TimeAsString
    {
        get => time.ToPrettyString();
        set
        {
            if (TimeSpanColumnData.TryParseTime(value, out var time))
            {
                if (this.time == time)
                    return;

                this.time = time;
                isModified = true;
                RaisePropertyChanged();
            }
            else
                throw new DataValidationException("Invalid time format: (-)HH:mm:ss");
        }
    }

    public TimeSpanCellEditorViewModel(string? mySqlType, TimeSpanSparseColumnData overrideData, TimeSpanColumnData data, int rowIndex, bool nullable, bool readOnly) 
        : base(mySqlType, overrideData, data, rowIndex, nullable, readOnly)
    {
        this.overrideData = overrideData;
        time = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }

    public override void ApplyChanges()
    {
        if (!isModified)
            return;

        overrideData.Override(rowIndex, IsNull ? null : time);
    }
}