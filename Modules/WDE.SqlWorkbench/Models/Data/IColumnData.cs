using System;
using System.Collections.Generic;
using System.Diagnostics;
using MySqlConnector;

namespace WDE.SqlWorkbench.Models;

internal interface ISparseColumnData : IReadOnlyColumnData
{
    bool TryOverride(int rowIndex, string? str, out string? error);
    void Clear();
    event Action<int>? OnRowOverriden;
    
    static ISparseColumnData Create(IColumnData impl)
    {
        if (impl is BooleanColumnData boolean)
            return new BooleanSparseColumnData(boolean);
        else if (impl is BinaryColumnData binary)
            return new BinarySparseColumnData(binary);
        else if (impl is ByteColumnData @byte)
            return new ByteSparseColumnData(@byte);
        else if (impl is DateTimeColumnData dateTime)
            return new DateTimeSparseColumnData(dateTime);
        else if (impl is DecimalColumnData @decimal)
            return new DecimalSparseColumnData(@decimal);
        else if (impl is DoubleColumnData @double)
            return new DoubleSparseColumnData(@double);
        else if (impl is FloatColumnData @float)
            return new FloatSparseColumnData(@float);
        else if (impl is Int16ColumnData int16)
            return new Int16SparseColumnData(int16);
        else if (impl is Int32ColumnData int32)
            return new Int32SparseColumnData(int32);
        else if (impl is Int64ColumnData int64)
            return new Int64SparseColumnData(int64);
        else if (impl is MySqlDateTimeColumnData mySqlDateTime)
            return new MySqlDateTimeSparseColumnData(mySqlDateTime);
        else if (impl is ObjectColumnData @object)
            return new ObjectSparseColumnData(@object);
        else if (impl is SByteColumnData sByte)
            return new SByteSparseColumnData(sByte);
        else if (impl is StringColumnData @string)
            return new StringSparseColumnData(@string);
        else if (impl is TimeSpanColumnData timeSpan)
            return new TimeSpanSparseColumnData(timeSpan);
        else if (impl is UInt16ColumnData uInt16)
            return new UInt16SparseColumnData(uInt16);
        else if (impl is UInt32ColumnData uInt32)
            return new UInt32SparseColumnData(uInt32);
        else if (impl is UInt64ColumnData uInt64)
            return new UInt64SparseColumnData(uInt64);
        else
            throw new InvalidOperationException($"Unknown column type {impl.GetType()}");
    }
}

internal class BooleanSparseColumnData(BooleanColumnData impl) : SparseColumnData(impl)
{
    public bool this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, bool? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class BinarySparseColumnData(BinaryColumnData impl) : SparseColumnData(impl)
{
    public ReadOnlySpan<byte> this[int index] => impl[GetActualIndex(index)];
    public void OverrideNull(int rowIndex, bool isNull)
    {
        impl.OverrideNull(GetOrCreateActualIndex(rowIndex), isNull);
        RaiseOnRowOverriden(rowIndex);
    }

    public void Override(int rowIndex, ReadOnlySpan<byte> value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }

    public Memory<byte> AsWriteableMemory(int rowIndex) => impl.AsWriteableMemory(GetActualIndex(rowIndex));
}

internal class ByteSparseColumnData(ByteColumnData impl) : SparseColumnData(impl)
{
    public byte this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, byte? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class DateTimeSparseColumnData(DateTimeColumnData impl) : SparseColumnData(impl)
{
    public DateTime this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, DateTime? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class DecimalSparseColumnData(DecimalColumnData impl) : SparseColumnData(impl)
{
    public PublicMySqlDecimal this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, PublicMySqlDecimal? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class DoubleSparseColumnData(DoubleColumnData impl) : SparseColumnData(impl)
{
    public double this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, double? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class FloatSparseColumnData(FloatColumnData impl) : SparseColumnData(impl)
{
    public float this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, float? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class Int16SparseColumnData(Int16ColumnData impl) : SparseColumnData(impl)
{
    public short this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, short? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class Int32SparseColumnData(Int32ColumnData impl) : SparseColumnData(impl)
{
    public int this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, int? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class Int64SparseColumnData(Int64ColumnData impl) : SparseColumnData(impl)
{
    public long this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, long? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class MySqlDateTimeSparseColumnData(MySqlDateTimeColumnData impl) : SparseColumnData(impl)
{
    public MySqlDateTime this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, MySqlDateTime? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class ObjectSparseColumnData(ObjectColumnData impl) : SparseColumnData(impl)
{
    public object? this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, object? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class SByteSparseColumnData(SByteColumnData impl) : SparseColumnData(impl)
{
    public sbyte this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, sbyte? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class StringSparseColumnData(StringColumnData impl) : SparseColumnData(impl)
{
    public string? this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, string? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class TimeSpanSparseColumnData(TimeSpanColumnData impl) : SparseColumnData(impl)
{
    public TimeSpan this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, TimeSpan? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class UInt16SparseColumnData(UInt16ColumnData impl) : SparseColumnData(impl)
{
    public ushort this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, ushort? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class UInt32SparseColumnData(UInt32ColumnData impl) : SparseColumnData(impl)
{
    public uint this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, uint? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal class UInt64SparseColumnData(UInt64ColumnData impl) : SparseColumnData(impl)
{
    public ulong this[int index] => impl[GetActualIndex(index)];
    public void Override(int rowIndex, ulong? value)
    {
        impl.Override(GetOrCreateActualIndex(rowIndex), value);
        RaiseOnRowOverriden(rowIndex);
    }
}

internal abstract class SparseColumnData : ISparseColumnData
{
    private Dictionary<int, int> rowIndexToIndex = new ();
    private List<int>? freeActualIndices;
    private IColumnData impl;
    
    public SparseColumnData(IColumnData impl)
    {
        this.impl = impl;
    }

    protected int GetActualIndex(int rowIndex)
    {
        if (!rowIndexToIndex.TryGetValue(rowIndex, out var i)) 
            throw new ArgumentOutOfRangeException($"Row {rowIndex} not found");
        return i;
    }

    protected int GetOrCreateActualIndex(int rowIndex)
    {
        if (!rowIndexToIndex.TryGetValue(rowIndex, out var i))
        {
            int newIndex;
            if (freeActualIndices != null && freeActualIndices.Count > 0)
            {
                newIndex = freeActualIndices[^1];
                freeActualIndices.RemoveAt(freeActualIndices.Count - 1);
            }
            else
                newIndex = impl.AppendNull();
            rowIndexToIndex[rowIndex] = i = newIndex;
        }
        return i;
    }

    private void RemoveIndex(int rowIndex)
    {
        if (rowIndexToIndex.TryGetValue(rowIndex, out var i))
        {
            rowIndexToIndex.Remove(rowIndex);
            freeActualIndices ??= new List<int>();
            freeActualIndices.Add(i);
        }
    }
    
    public string? GetToString(int rowIndex)
    {
        return impl.GetToString(GetActualIndex(rowIndex));
    }

    public bool IsNull(int rowIndex)
    {
        return impl.IsNull(GetActualIndex(rowIndex));
    }

    public bool HasRow(int rowIndex)
    {
        return rowIndexToIndex.ContainsKey(rowIndex);
    }

    public ColumnTypeCategory Category => impl.Category;

    public IColumnData CloneEmpty()
    {
        throw new NotImplementedException();
    }

    public bool TryOverride(int rowIndex, string? str, out string? error)
    {
        var hasRow = HasRow(rowIndex);
        if (!impl.TryOverride(GetOrCreateActualIndex(rowIndex), str, out error))
        {
            if (!hasRow)
            {
                // is hasRow is False, then we have just created the index in GetOrCreateActualIndex
                // but since the override failed, we need to remove it
                RemoveIndex(rowIndex);
            }

            return false;
        }

        return true;
    }

    public void Clear()
    {
        rowIndexToIndex.Clear();
        impl.Clear();
        freeActualIndices?.Clear();
    }

    protected void RaiseOnRowOverriden(int rowIndex) => OnRowOverriden?.Invoke(rowIndex);

    public event Action<int>? OnRowOverriden;
}

internal interface IReadOnlyColumnData
{
    bool HasRow(int rowIndex);
    string? GetToString(int rowIndex);
    bool IsNull(int rowIndex);
    ColumnTypeCategory Category { get; }
    IColumnData CloneEmpty();
}

internal interface IColumnData : IReadOnlyColumnData
{
    void Append(IMySqlDataReader reader, int ordinal);
    bool TryOverride(int rowIndex, string? str, out string? error);
    int AppendNull();
    void Clear();
    
    static IColumnData CreateTypedColumn(Type? dataType, string? mySqlDataType)
    {
        if (dataType == typeof(string))
            return new StringColumnData();
        else if (dataType == typeof(bool))
            return new BooleanColumnData();
        else if (dataType == typeof(byte))
            return new ByteColumnData();
        else if (dataType == typeof(sbyte))
            return new SByteColumnData();
        else if (dataType == typeof(short))
            return new Int16ColumnData();
        else if (dataType == typeof(ushort))
            return new UInt16ColumnData();
        else if (dataType == typeof(int))
            return new Int32ColumnData();
        else if (dataType == typeof(uint))
            return new UInt32ColumnData();
        else if (dataType == typeof(long))
            return new Int64ColumnData();
        else if (dataType == typeof(ulong))
            return new UInt64ColumnData();
        else if (dataType == typeof(decimal) || dataType == typeof(PublicMySqlDecimal) || dataType == typeof(MySqlDecimal))
            return new DecimalColumnData();
        else if (dataType == typeof(double))
            return new DoubleColumnData();
        else if (dataType == typeof(float))
            return new FloatColumnData();
        else if (dataType == typeof(MySqlDateTime))
            return new MySqlDateTimeColumnData(mySqlDataType);
        else if (dataType == typeof(DateTime))
            return new DateTimeColumnData();
        else if (dataType == typeof(TimeSpan))
            return new TimeSpanColumnData();
        else if (dataType == typeof(byte[]))
            return new BinaryColumnData();
        else
            return new ObjectColumnData();
    }
}