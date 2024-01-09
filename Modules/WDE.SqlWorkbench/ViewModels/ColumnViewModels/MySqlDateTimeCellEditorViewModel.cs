using System.Text.RegularExpressions;
using Avalonia.Data;
using MySqlConnector;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Models.DataTypes;
using WDE.SqlWorkbench.Utils;

namespace WDE.SqlWorkbench.ViewModels;

internal class MySqlDateTimeCellEditorViewModel : BaseCellEditorViewModel
{
    private readonly MySqlDateTimeSparseColumnData overrideData;
    
    private bool dateOnly;
    
    private MySqlDateTime dateTime;
    public string DateTimeAsString
    {
        get => dateTime.ToPrettyString(dateOnly);
        set
        {
            if (MySqlDateTimeColumnData.TryParse(value, out var dateTime))
            {
                if (this.dateTime == dateTime)
                    return;

                this.dateTime = dateTime;
                isModified = true;
                RaisePropertyChanged();
            }
            else
                throw new DataValidationException("Invalid date format: yyyy-MM-dd HH:mm:ss");
        }
    }

    public MySqlDateTimeCellEditorViewModel(string? mySqlType, MySqlDateTimeSparseColumnData overrideData, MySqlDateTimeColumnData data, int rowIndex, bool nullable, bool readOnly) 
        : base(mySqlType, overrideData, data, rowIndex, nullable, readOnly)
    {
        this.overrideData = overrideData;
        if (MySqlType.TryParse(mySqlType, out var type)
            && type.Kind == MySqlTypeKind.Date)
        {
            dateOnly = type.AsDate()!.Value.Kind == DateTimeDataTypeKind.Date;
        }
        dateTime = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }

    public override void ApplyChanges()
    {
        if (!isModified)
            return;

        overrideData.Override(rowIndex, IsNull ? null : dateTime);
    }
}