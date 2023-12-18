using System;
using Avalonia.Data;
using AvaloniaEdit.Document;
using WDE.Common.Avalonia.Controls;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Models.DataTypes;

namespace WDE.SqlWorkbench.ViewModels;

internal abstract class BaseCellEditorViewModel : ObservableBase
{
    protected readonly int rowIndex;
    protected bool isModified;
    public bool CanBeNull { get; }

    private bool isNull;
    public bool IsNull
    {
        get => isNull;
        set
        {
            if (isNull == value)
                return;

            isModified = true;
            SetProperty(ref isNull, value);
            ApplyChanges();
        }
    }

    public string? Type { get; }

    public BaseCellEditorViewModel(string? mySqlType, ISparseColumnData overrideData, IColumnData columnData, int rowIndex, bool nullable)
    {
        this.rowIndex = rowIndex;
        Type = mySqlType;
        CanBeNull = nullable;
        // don't use the property here, because it will call ApplyChanges which is an abstract method and will not work in the base constructor!
        isNull = overrideData.HasRow(rowIndex) ? overrideData.IsNull(rowIndex) : columnData.IsNull(rowIndex);
    }

    public abstract void ApplyChanges();
}

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
    
    public UnsignedIntegerCellEditorViewModel(string? mySqlType, ByteSparseColumnData overrideData, ByteColumnData data, int rowIndex, bool nullable) : base(mySqlType, overrideData, data, rowIndex, nullable)
    {
        this.overrideByte = overrideData;
        maxValue = byte.MaxValue;
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }
    
    public UnsignedIntegerCellEditorViewModel(string? mySqlType, UInt16SparseColumnData overrideData, UInt16ColumnData data, int rowIndex, bool nullable) : base(mySqlType, overrideData, data, rowIndex, nullable)
    {
        overrideInt16 = overrideData;
        maxValue = ushort.MaxValue;
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }
    
    public UnsignedIntegerCellEditorViewModel(string? mySqlType, UInt32SparseColumnData overrideData, UInt32ColumnData data, int rowIndex, bool nullable) : base(mySqlType, overrideData, data, rowIndex, nullable)
    {
        overrideInt32 = overrideData;
        maxValue = uint.MaxValue;
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }
    
    public UnsignedIntegerCellEditorViewModel(string? mySqlType, UInt64SparseColumnData overrideData, UInt64ColumnData data, int rowIndex, bool nullable) : base(mySqlType, overrideData, data, rowIndex, nullable)
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

    public SignedIntegerCellEditorViewModel(string? mySqlType, SByteSparseColumnData overrideData, SByteColumnData data, int rowIndex, bool nullable) : base(mySqlType, overrideData, data, rowIndex, nullable)
    {
        overrideByte = overrideData;
        maxValue = sbyte.MaxValue;
        minValue = sbyte.MinValue;
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }
    
    public SignedIntegerCellEditorViewModel(string? mySqlType, Int16SparseColumnData overrideData, Int16ColumnData data, int rowIndex, bool nullable) : base(mySqlType, overrideData, data, rowIndex, nullable)
    {
        overrideInt16 = overrideData;
        maxValue = short.MaxValue;
        minValue = short.MinValue;
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }
    
    public SignedIntegerCellEditorViewModel(string? mySqlType, Int32SparseColumnData overrideData, Int32ColumnData data, int rowIndex, bool nullable) : base(mySqlType, overrideData, data, rowIndex, nullable)
    {
        overrideInt32 = overrideData;
        maxValue = int.MaxValue;
        minValue = int.MinValue;
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }
    
    public SignedIntegerCellEditorViewModel(string? mySqlType, Int64SparseColumnData overrideData, Int64ColumnData data, int rowIndex, bool nullable) : base(mySqlType, overrideData, data, rowIndex, nullable)
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

internal class StringCellEditorViewModel : BaseCellEditorViewModel
{
    private readonly StringSparseColumnData overrideData;
    private int? maxLength;
    
    public TextDocument Document { get; } = new();
    
    public bool IsTooLong => maxLength.HasValue && Document.TextLength > maxLength.Value;
    
    public int Length => Document.TextLength;
    
    public StringCellEditorViewModel(string? mySqlType, StringSparseColumnData overrideData, StringColumnData data, int rowIndex, bool nullable) : base(mySqlType, overrideData, data, rowIndex, nullable)
    {
        this.overrideData = overrideData;
        if (MySqlType.TryParse(mySqlType, out var type)
            && type.Kind == MySqlTypeKind.Text)
        {
            maxLength = type.AsText()!.Value.Length;
        }
        Document.Text = (overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex]) ?? "";
        Document.TextChanged += DocumentOnTextChanged;
    }

    private void DocumentOnTextChanged(object? sender, EventArgs e)
    {
        RaisePropertyChanged(nameof(Length));
        if (maxLength.HasValue)
            RaisePropertyChanged(nameof(IsTooLong));
        isModified = true;
    }

    public override void ApplyChanges()
    {
        if (!isModified)
            return;

        overrideData.Override(rowIndex, IsNull ? null : Document.Text);
    }
}

internal class BinaryCellEditorViewModel : BaseCellEditorViewModel
{
    private readonly BinarySparseColumnData overrideData;
    private int? maxLength;
    
    public CopyOnWriteMemory<byte> Bytes { get; private set; }

    public int Length
    {
        get => Bytes.Length;
        set
        {
            if (value == Bytes.Length)
                return;

            isModified = true;
            var newBytes = new byte[value];
            Bytes.Slice(0, Math.Min(value, Bytes.Length)).CopyTo(newBytes);
            Bytes = new CopyOnWriteMemory<byte>(newBytes);
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(Bytes));
            ApplyChanges();
        }
    }
    
    public BinaryCellEditorViewModel(string? mySqlType, BinarySparseColumnData overrideData, BinaryColumnData data, int rowIndex, bool nullable) : base(mySqlType, overrideData, data, rowIndex, nullable)
    {
        this.overrideData = overrideData;
        if (MySqlType.TryParse(mySqlType, out var type)
            && type.Kind == MySqlTypeKind.Text)
        {
            maxLength = type.AsText()!.Value.Length;
        }

        Bytes = overrideData.HasRow(rowIndex)
            ? new CopyOnWriteMemory<byte>(overrideData.AsWriteableMemory(rowIndex))
            : new CopyOnWriteMemory<byte>(data.AsReadOnlyMemory(rowIndex));
    }

    public override void ApplyChanges()
    {
        if (!isModified && !Bytes.IsModified)
            return;
        
        overrideData.OverrideNull(rowIndex, IsNull);
        if (!IsNull && Bytes.IsCopy)
            overrideData.Override(rowIndex, Bytes.ModifiedArray);
    }
}