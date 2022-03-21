using Microsoft.VisualBasic;
using NUnit.Framework;
using WDE.Common.Utils;

namespace WDE.Common.Test.Utils;

public class MultiIndexContainerTests
{
    [Test]
    public void TestAdd()
    {
        MultiIndexContainer c = new();
        c.Add(1);
        CollectionAssert.AreEqual(new[]{(1, 1)}, c.DebugRanges);
        c.Add(3);
        CollectionAssert.AreEqual(new[]{(1, 1), (3, 3)}, c.DebugRanges);
        c.Add(4);
        CollectionAssert.AreEqual(new[]{(1, 1), (3, 4)}, c.DebugRanges);
        c.Add(0);
        CollectionAssert.AreEqual(new[]{(0, 1), (3, 4)}, c.DebugRanges);
        c.Add(-2);
        CollectionAssert.AreEqual(new[]{(-2, -2), (0, 1), (3, 4)}, c.DebugRanges);
        c.Add(2);
        CollectionAssert.AreEqual(new[]{(-2, -2), (0, 4)}, c.DebugRanges);
        c.Add(7);
        CollectionAssert.AreEqual(new[]{(-2, -2), (0, 4), (7, 7)}, c.DebugRanges);
        c.Add(8);
        CollectionAssert.AreEqual(new[]{(-2, -2), (0, 4), (7, 8)}, c.DebugRanges);
        c.Add(9);
        CollectionAssert.AreEqual(new[]{(-2, -2), (0, 4), (7, 9)}, c.DebugRanges);
        c.Add(5);
        CollectionAssert.AreEqual(new[]{(-2, -2), (0, 5), (7, 9)}, c.DebugRanges);
        c.Add(6);
        CollectionAssert.AreEqual(new[]{(-2, -2), (0, 9)}, c.DebugRanges);
        c.Add(-1);
        CollectionAssert.AreEqual(new[]{(-2, 9)}, c.DebugRanges);
    }
    
    [Test]
    public void TestAdd2()
    {
        MultiIndexContainer c = new();
        c.Add(1);
        CollectionAssert.AreEqual(new[] { (1, 1) }, c.DebugRanges);
        c.Add(-1);
        CollectionAssert.AreEqual(new[] { (-1, -1), (1, 1) }, c.DebugRanges);
    }
    
    [Test]
    public void TestDoubleAdd()
    {
        MultiIndexContainer c = new();
        c.Add(1);
        CollectionAssert.AreEqual(new[] { (1, 1) }, c.DebugRanges);
        c.Add(1);
        CollectionAssert.AreEqual(new[] { (1, 1) }, c.DebugRanges);
        c.Add(3);
        CollectionAssert.AreEqual(new[] { (1, 1), (3, 3) }, c.DebugRanges);
        c.Add(3);
        CollectionAssert.AreEqual(new[] { (1, 1), (3, 3) }, c.DebugRanges);
        c.Add(3);
        CollectionAssert.AreEqual(new[] { (1, 1), (3, 3) }, c.DebugRanges);
        c.Add(3);
        CollectionAssert.AreEqual(new[] { (1, 1), (3, 3) }, c.DebugRanges);
        c.Add(2);
        CollectionAssert.AreEqual(new[] { (1, 3) }, c.DebugRanges);
        c.Add(2);
        CollectionAssert.AreEqual(new[] { (1, 3) }, c.DebugRanges);
        c.Add(1);
        c.Add(2);
        c.Add(3);
        CollectionAssert.AreEqual(new[] { (1, 3) }, c.DebugRanges);
        c.AddRange(5, 6, 7);
        CollectionAssert.AreEqual(new[] { (1, 3), (5, 7) }, c.DebugRanges);
        c.Add(1);
        c.Add(2);
        c.Add(3);
        c.Add(5);
        c.Add(6);
        c.Add(7);
        CollectionAssert.AreEqual(new[] { (1, 3), (5, 7) }, c.DebugRanges);
    }
    
    [Test]
    public void TestAll()
    {
        MultiIndexContainer c = new();
        c.AddRange(-1, 0, 2, 4, 3, 8, 6, 7);
        CollectionAssert.AreEqual(new[] { -1, 0, 2, 3, 4, 6, 7, 8 }, c.All());
    }
    
    [Test]
    public void TestContains()
    {
        MultiIndexContainer c = new();
        c.AddRange(-1, 0, 2, 4, 3, 8, 6, 7, 12, 13, 14);
        Assert.AreEqual(false, c.Contains(-2));
        Assert.AreEqual(true, c.Contains(-1));
        Assert.AreEqual(true, c.Contains(0));
        Assert.AreEqual(false, c.Contains(1));
        Assert.AreEqual(true, c.Contains(2));
        Assert.AreEqual(true, c.Contains(4));
        Assert.AreEqual(true, c.Contains(3));
        Assert.AreEqual(false, c.Contains(5));
        Assert.AreEqual(true, c.Contains(8));
        Assert.AreEqual(true, c.Contains(6));
        Assert.AreEqual(true, c.Contains(7));
        Assert.AreEqual(false, c.Contains(9));
        Assert.AreEqual(false, c.Contains(10));
        Assert.AreEqual(false, c.Contains(11));
        Assert.AreEqual(true, c.Contains(12));
        Assert.AreEqual(true, c.Contains(13));
        Assert.AreEqual(true, c.Contains(14));
        Assert.AreEqual(false, c.Contains(15));
    }

    [Test]
    public void TestEfficientContains()
    {
        MultiIndexContainer c = new();
        c.AddRange(-1, 0, 2, 4, 3, 8, 6, 7, 12, 13, 14);
        var itr = c.ContainsIterator;
        Assert.AreEqual(false, itr.Contains(-3));
        Assert.AreEqual(false, itr.Contains(-2));
        Assert.AreEqual(true, itr.Contains(-1));
        Assert.AreEqual(true, itr.Contains(-1));
        Assert.AreEqual(true, itr.Contains(0));
        Assert.AreEqual(true, itr.Contains(2));
        Assert.AreEqual(true, itr.Contains(4));
        Assert.AreEqual(true, itr.Contains(3));
        Assert.AreEqual(false, itr.Contains(-1));
        Assert.AreEqual(false, itr.Contains(5));
        Assert.AreEqual(true, itr.Contains(6));
        Assert.AreEqual(true, itr.Contains(13));
        Assert.AreEqual(false, itr.Contains(7));
        Assert.AreEqual(false, itr.Contains(8));
        Assert.AreEqual(false, itr.Contains(9));
        Assert.AreEqual(false, itr.Contains(10));
        Assert.AreEqual(false, itr.Contains(11));
        Assert.AreEqual(true, itr.Contains(12));
        Assert.AreEqual(true, itr.Contains(14));
        Assert.AreEqual(false, itr.Contains(15));
    }
}