using Avalonia.Data;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.ViewModels;

internal class FloatCellEditorViewModel : BaseCellEditorViewModel
{
    private readonly FloatSparseColumnData overrideData;
    
    private float value;
    public float Value
    {
        get => value;
        set
        {
            if (float.IsInfinity(value))
                throw new DataValidationException("MySql can't hold infinity values");
            if (float.IsNaN(value))
                throw new DataValidationException("MySql can't hold NaN values");
            
            if (this.value == value)
                return;
            
            isModified = true;
            this.value = value;
            RaisePropertyChanged();
        }
    }

    public FloatCellEditorViewModel(string? mySqlType, FloatSparseColumnData overrideData, FloatColumnData data, int rowIndex, bool nullable, bool readOnly) 
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