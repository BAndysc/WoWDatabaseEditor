using Avalonia.Data;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.ViewModels;

internal class DoubleCellEditorViewModel : BaseCellEditorViewModel
{
    private readonly DoubleSparseColumnData overrideData;
    
    private double value;
    public double Value
    {
        get => value;
        set
        {
            if (double.IsInfinity(value))
                throw new DataValidationException("MySql can't hold infinity values");
            if (double.IsNaN(value))
                throw new DataValidationException("MySql can't hold NaN values");
            
            if (this.value == value)
                return;
            
            isModified = true;
            this.value = value;
            RaisePropertyChanged();
        }
    }

    public DoubleCellEditorViewModel(string? mySqlType, DoubleSparseColumnData overrideData, DoubleColumnData data, int rowIndex, bool nullable, bool readOnly) 
        : base(mySqlType, overrideData, data, rowIndex, nullable, readOnly)
    {
        this.overrideData = overrideData;
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }

    public override void ApplyChanges()
    {
        if (!isModified)
            return;

        overrideData.Override(rowIndex, IsNull ? null : value);
    }
}