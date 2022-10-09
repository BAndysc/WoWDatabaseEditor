using NUnit.Framework;
using WDE.DbcStore.FastReader;

namespace WDE.DbcStore.Test;

public class BitStreamUtilsTest
{
    [Test]
    public void Test_Whole_Byte()
    {
        byte[] array = new byte[] { 0, 0b11111111 };
        Assert.AreEqual(255, BitStreamUtils.ReadInt32(array, 8, 8));
    }
    
    [Test]
    public void Test_Byte_Skip_Left()
    {
        byte[] array = new byte[] { 0, 0b11111110 };
        Assert.AreEqual(0b1111111, BitStreamUtils.ReadInt32(array, 9, 7));
    }
    
    [Test]
    public void Test_Byte_Skip_Right()
    {
        byte[] array = new byte[] { 0, 0b11111110 };
        Assert.AreEqual(0b1111110, BitStreamUtils.ReadInt32(array, 8, 7));
    }
    
    [Test]
    public void Test_Multi_Byte()
    {
        byte[] array = new byte[] { 0, 0b11111111, 0b11111111 };
        Assert.AreEqual(0b11111111_11111111, BitStreamUtils.ReadInt32(array, 8, 16));
    }
    
    [Test]
    public void Test_Multi_Byte_Skip_Left()
    {
        byte[] array = new byte[] { 0, 0b11111100, 0b11111111 };
        Assert.AreEqual(0b111111_11111111, BitStreamUtils.ReadInt32(array, 10, 14));
    }
    
    [Test]
    public void Test_Multi_Byte_Skip_Right()
    {
        byte[] array = new byte[] { 0, 0b11111111, 0b00101111 };
        Assert.AreEqual(0b101111_11111111, BitStreamUtils.ReadInt32(array, 8, 14));
    }
}