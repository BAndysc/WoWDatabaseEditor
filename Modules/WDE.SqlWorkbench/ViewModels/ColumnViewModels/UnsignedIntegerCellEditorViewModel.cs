using System;
using Avalonia.Data;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Models.DataTypes;

namespace WDE.SqlWorkbench.ViewModels;

internal class UnsignedIntegerCellEditorViewModel : BaseCellEditorViewModel
{
    private readonly ByteSparseColumnData? overrideByte;
    private readonly UInt16SparseColumnData? overrideInt16;
    private readonly UInt32SparseColumnData? overrideInt32;
    private readonly UInt64SparseColumnData? overrideInt64;
    private readonly ulong maxValue;

    private ulong value;
    public ulong Value
    {
        get => value;
        set
        {
            if (value == this.value)
                return;
            
            if (value > maxValue)
                throw new DataValidationException($"Value cannot be greater than {maxValue}");
            
            isModified = true;
            this.value = value;
            
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HexValue));
        }
    }

    public string HexValue
    {
        get => $"0x{value:X}";
        set
        {
            if (string.IsNullOrEmpty(value))
                return;
            
            var startParseIndex = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
            
            if (!ulong.TryParse(value.AsSpan(startParseIndex), System.Globalization.NumberStyles.HexNumber, null, out var v))
                throw new DataValidationException("Invalid hex number");    
            Value = v;
        }
    }
    
    public ulong MaxValue => maxValue;
    public ulong MinValue => 0;
    
    public UnsignedIntegerCellEditorViewModel(string? mySqlType, ByteSparseColumnData overrideData, ByteColumnData data, int rowIndex, bool nullable, bool readOnly) 
        : base(mySqlType, overrideData, data, rowIndex, nullable, readOnly)
    {
        this.overrideByte = overrideData;
        maxValue = byte.MaxValue;
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }
    
    public UnsignedIntegerCellEditorViewModel(string? mySqlType, UInt16SparseColumnData overrideData, UInt16ColumnData data, int rowIndex, bool nullable, bool readOnly) 
        : base(mySqlType, overrideData, data, rowIndex, nullable, readOnly)
    {
        overrideInt16 = overrideData;
        maxValue = ushort.MaxValue;
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }
    
    public UnsignedIntegerCellEditorViewModel(string? mySqlType, UInt32SparseColumnData overrideData, UInt32ColumnData data, int rowIndex, bool nullable, bool readOnly) 
        : base(mySqlType, overrideData, data, rowIndex, nullable, readOnly)
    {
        overrideInt32 = overrideData;
        maxValue = uint.MaxValue;
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }
    
    public UnsignedIntegerCellEditorViewModel(string? mySqlType, UInt64SparseColumnData overrideData, UInt64ColumnData data, int rowIndex, bool nullable, bool readOnly) 
        : base(mySqlType, overrideData, data, rowIndex, nullable, readOnly)
    {
        maxValue = ulong.MaxValue;
        // MySqlConnector returns bit(x) as ulong, so we need to handle it here
        if (MySqlType.TryParse(mySqlType, out var type)
            && type.Kind == MySqlTypeKind.Numeric &&
            type.AsNumeric()!.Value.Kind == NumericDataTypeKind.Bit &&
            type.AsNumeric()!.Value.M is { } m)
        {
            maxValue = (1UL << m) - 1;
        }
        overrideInt64 = overrideData;
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }

    public override void ApplyChanges()
    {
        if (!isModified)
            return;

        if (overrideByte != null)
            overrideByte.Override(rowIndex, IsNull ? null : (byte)value);
        else if (overrideInt16 != null)
            overrideInt16.Override(rowIndex, IsNull ? null : (ushort)value);
        else if (overrideInt32 != null)
            overrideInt32.Override(rowIndex, IsNull ? null : (uint)value);
        else if (overrideInt64 != null)
            overrideInt64.Override(rowIndex, IsNull ? null : value);
    }
}