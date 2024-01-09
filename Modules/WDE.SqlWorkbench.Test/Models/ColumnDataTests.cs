using System.Diagnostics.CodeAnalysis;
using MySqlConnector;
using NSubstitute;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Test.Models;

[SuppressMessage("Assertion", "NUnit2005:Consider using Assert.That(actual, Is.EqualTo(expected)) instead of Assert.AreEqual(expected, actual)")]
public class ColumnDataTests
{
    private IMySqlDataReader reader;
    private List<object?> data;
    
    [SetUp]
    public void Setup()
    {
        reader = Substitute.For<IMySqlDataReader>();
        reader.IsDBNull(default).ReturnsForAnyArgs(x => data[x.Arg<int>()] == null);
        data = new List<object?>();
    }
        
    [Test]
    public void TestObject()
    {
        reader.GetValue(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, "a", null, 5 });
        var column = new ObjectColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("a", column.GetToString(1));
        Assert.AreEqual("5", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.Unknown, column.Category);
    }
    
    [Test]
    public void TestString()
    {
        reader.GetString(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, "a", null, "b" });
        var column = new StringColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("a", column.GetToString(1));
        Assert.AreEqual("b", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.String, column.Category);
    }
    
    [Test]
    public void TestBoolean()
    {
        reader.GetBoolean(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, true, null, false });
        var column = new BooleanColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("True", column.GetToString(1));
        Assert.AreEqual("False", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.Number, column.Category);
    }
    
    [Test]
    public void TestByte()
    {
        reader.GetByte(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, (byte)5, null, (byte)6 });
        var column = new ByteColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("5", column.GetToString(1));
        Assert.AreEqual("6", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.Number, column.Category);
    }
    
    [Test]
    public void TestSByte()
    {
        reader.GetSByte(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, (sbyte)5, null, (sbyte)-5 });
        var column = new SByteColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("5", column.GetToString(1));
        Assert.AreEqual("-5", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.Number, column.Category);
    }
    
    [Test]
    public void TestInt16()
    {
        reader.GetInt16(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, (short)5, null, (short)short.MinValue });
        var column = new Int16ColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("5", column.GetToString(1));
        Assert.AreEqual("-32768", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.Number, column.Category);
    }
    
    [Test]
    public void TestUInt16()
    {
        reader.GetUInt16(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, ushort.MaxValue, null, (ushort)5 });
        var column = new UInt16ColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("65535", column.GetToString(1));
        Assert.AreEqual("5", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.Number, column.Category);
    }
    
    [Test]
    public void TestInt32()
    {
        reader.GetInt32(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, int.MaxValue, null, int.MinValue });
        var column = new Int32ColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("2147483647", column.GetToString(1));
        Assert.AreEqual("-2147483648", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.Number, column.Category);
    }
    
    [Test]
    public void TestUInt32()
    {
        reader.GetUInt32(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, uint.MaxValue, null, uint.MinValue });
        var column = new UInt32ColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("4294967295", column.GetToString(1));
        Assert.AreEqual("0", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.Number, column.Category);
    }
    
    [Test]
    public void TestInt64()
    {
        reader.GetInt64(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, long.MaxValue, null, long.MinValue });
        var column = new Int64ColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("9223372036854775807", column.GetToString(1));
        Assert.AreEqual("-9223372036854775808", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.Number, column.Category);
    }
    
    [Test]
    public void TestUInt64()
    {
        reader.GetUInt64(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, ulong.MaxValue, null, ulong.MinValue });
        var column = new UInt64ColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("18446744073709551615", column.GetToString(1));
        Assert.AreEqual("0", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.Number, column.Category);
    }
    
    [Test]
    public void TestDecimal()
    {
        var d = PublicMySqlDecimal.Parse("0000.1");
        var d2 = PublicMySqlDecimal.Parse("-99999.9900");
        var d3 = PublicMySqlDecimal.Parse("1000");
        reader.GetDecimal(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, d2, null, d, d3 });
        var column = new DecimalColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.IsFalse(column.IsNull(4));
        Assert.AreEqual("1000", column.GetToString(4));
        Assert.AreEqual("-99999.99", column.GetToString(1));
        Assert.AreEqual("0.1", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.Number, column.Category);
    }
    
    [Test]
    public void TestDouble()
    {
        reader.GetDouble(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, 2.0, null, -1.5 });
        var column = new DoubleColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("2", column.GetToString(1));
        Assert.AreEqual("-1.5", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.Number, column.Category);
    }
    
    [Test]
    public void TestFloat()
    {
        reader.GetFloat(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, 2.0f, null, -1.5f });
        var column = new FloatColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("2", column.GetToString(1));
        Assert.AreEqual("-1.5", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.Number, column.Category);
    }
    
    [Test]
    public void TestDateTime()
    {
        reader.GetDateTime(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, new DateTime(2000, 5, 21, 13, 59, 59), null});
        var column = new DateTimeColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.AreEqual("2000-05-21 13:59:59", column.GetToString(1));
        Assert.AreEqual(ColumnTypeCategory.DateTime, column.Category);
    }
    
    [Test]
    public void TestTimeSpan()
    {
        reader.GetTimeSpan(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, new TimeSpan(1, 1, 0, 0), null, -new TimeSpan(12, 13, 14) });
        var column = new TimeSpanColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("25:00:00", column.GetToString(1));
        Assert.AreEqual("-12:13:14", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.DateTime, column.Category);
    }
    
    [Test]
    public void TestBinary()
    {
        reader.GetBytes(default, default, default, default, default)
            .ReturnsForAnyArgs(x =>
            {
                var bytes = (byte[])data[x.ArgAt<int>(0)]!;
                var dataOffset = x.ArgAt<long>(1);
                var buffer = x.ArgAt<byte[]?>(2);
                var bufferOffset = x.ArgAt<int>(3);
                var length = x.ArgAt<int>(4);
                
                if (buffer == null)
                    return (long)bytes.Length;
                
                bytes.AsSpan(new Index((int)dataOffset)).CopyTo(buffer.AsSpan(bufferOffset, length));
                return (long)bytes.Length;
            });
        data.AddRange(new object?[]{ null, new byte[]{0xDE, 0xAD, 0xBE, 0xEF}, null, new byte[]{0x37, 0x73} });
        var column = new BinaryColumnData();
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("DEADBEEF", column.GetToString(1));
        Assert.AreEqual("3773", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.Binary, column.Category);
    }
    
    [Test]
    public void TestMySqlDateTime()
    {
        reader.GetMySqlDateTime(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, 
            new MySqlDateTime(0, 0, 0, 0, 0, 0, 0), 
            null, 
            new MySqlDateTime(2020, 5, 12, 23, 59, 59, 1)
        });
        var column = new MySqlDateTimeColumnData("datetime");
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("0000-00-00 00:00:00", column.GetToString(1));
        Assert.AreEqual("2020-05-12 23:59:59.000001", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.DateTime, column.Category);
    }
    
    [Test]
    public void TestMySqlDate()
    {
        reader.GetMySqlDateTime(default).ReturnsForAnyArgs(x => data[x.Arg<int>()]);
        data.AddRange(new object?[]{ null, 
            new MySqlDateTime(0, 0, 0, 0, 0, 0, 0), 
            null, 
            new MySqlDateTime(2020, 5, 12, 23, 59, 59, 1)
        });
        var column = new MySqlDateTimeColumnData("date");
        for (int i = 0; i < data.Count; ++i)
            column.Append(reader, i);
        
        Assert.IsTrue(column.IsNull(0));
        Assert.IsFalse(column.IsNull(1));
        Assert.IsTrue(column.IsNull(2));
        Assert.IsFalse(column.IsNull(3));
        Assert.AreEqual("0000-00-00", column.GetToString(1));
        Assert.AreEqual("2020-05-12", column.GetToString(3));
        Assert.AreEqual(ColumnTypeCategory.DateTime, column.Category);
    }
}