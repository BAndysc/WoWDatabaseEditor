using Avalonia.Data;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.ViewModels;

internal class SignedIntegerCellEditorViewModel : BaseCellEditorViewModel
{
    private readonly SByteSparseColumnData? overrideByte;
    private readonly Int16SparseColumnData? overrideInt16;
    private readonly Int32SparseColumnData? overrideInt32;
    private readonly Int64SparseColumnData? overrideInt64;
    private long maxValue;
    private long minValue;
    
    private long value;
    public long Value
    {
        get => value;
        set
        {
            if (this.value == value)
                return;
            
            if (value > maxValue)
                throw new DataValidationException($"Value cannot be greater than {maxValue}");
            
            if (value < minValue)
                throw new DataValidationException($"Value cannot be less than {minValue}");
            
            isModified = true;
            this.value = value;
            RaisePropertyChanged();
        }
    }

    public long MaxValue => maxValue;
    public long MinValue => minValue;

    public SignedIntegerCellEditorViewModel(string? mySqlType, SByteSparseColumnData overrideData, SByteColumnData data, int rowIndex, bool nullable, bool readOnly) 
        : base(mySqlType, overrideData, data, rowIndex, nullable, readOnly)
    {
        overrideByte = overrideData;
        maxValue = sbyte.MaxValue;
        minValue = sbyte.MinValue;
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }
    
    public SignedIntegerCellEditorViewModel(string? mySqlType, Int16SparseColumnData overrideData, Int16ColumnData data, int rowIndex, bool nullable, bool readOnly) 
        : base(mySqlType, overrideData, data, rowIndex, nullable, readOnly)
    {
        overrideInt16 = overrideData;
        maxValue = short.MaxValue;
        minValue = short.MinValue;
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }
    
    public SignedIntegerCellEditorViewModel(string? mySqlType, Int32SparseColumnData overrideData, Int32ColumnData data, int rowIndex, bool nullable, bool readOnly) 
        : base(mySqlType, overrideData, data, rowIndex, nullable, readOnly)
    {
        overrideInt32 = overrideData;
        maxValue = int.MaxValue;
        minValue = int.MinValue;
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }
    
    public SignedIntegerCellEditorViewModel(string? mySqlType, Int64SparseColumnData overrideData, Int64ColumnData data, int rowIndex, bool nullable, bool readOnly) 
        : base(mySqlType, overrideData, data, rowIndex, nullable, readOnly)
    {
        overrideInt64 = overrideData;
        maxValue = long.MaxValue;
        minValue = long.MinValue;
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }

    public override void ApplyChanges()
    {
        if (!isModified)
            return;

        if (overrideByte != null)
            overrideByte.Override(rowIndex, IsNull ? null : (sbyte)value);
        else if (overrideInt16 != null)
            overrideInt16.Override(rowIndex, IsNull ? null : (short)value);
        else if (overrideInt32 != null)
            overrideInt32.Override(rowIndex, IsNull ? null : (int)value);
        else if (overrideInt64 != null)
            overrideInt64.Override(rowIndex, IsNull ? null : value);
    }
}