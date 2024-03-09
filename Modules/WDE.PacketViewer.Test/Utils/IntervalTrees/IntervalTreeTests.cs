using System.Runtime.Versioning;
using NUnit.Framework;
using WDE.PacketViewer.Utils.IntervalTrees;

namespace WDE.PacketViewer.Test.Utils.IntervalTrees;

[RequiresPreviewFeatures]
public class IntervalTreeTests
{
    [Test]
    public void TestQuery()
    {
        IntervalTree<int, int> tree = new();
        tree.Add(0, 3, 0);
        tree.Add(5, 7, 1);
        tree.Add(9, 12, 2);
        
        Assert.AreEqual(null, tree.Query(-2));
        Assert.AreEqual(null, tree.Query(-1));
        Assert.AreEqual(0, tree.Query(0)!.Value.value);
        Assert.AreEqual(0, tree.Query(1)!.Value.value);
        Assert.AreEqual(0, tree.Query(2)!.Value.value);
        Assert.AreEqual(0, tree.Query(3)!.Value.value);
        Assert.AreEqual(null, tree.Query(4));
        Assert.AreEqual(1, tree.Query(5)!.Value.value);
        Assert.AreEqual(1, tree.Query(6)!.Value.value);
        Assert.AreEqual(1, tree.Query(7)!.Value.value);
        Assert.AreEqual(null, tree.Query(8));
        Assert.AreEqual(2, tree.Query(9)!.Value.value);
        Assert.AreEqual(2, tree.Query(10)!.Value.value);
        Assert.AreEqual(2, tree.Query(11)!.Value.value);
        Assert.AreEqual(2, tree.Query(12)!.Value.value);
        Assert.AreEqual(null, tree.Query(13));
    }
    
    [Test]
    public void TestQueryBefore()
    {
        IntervalTree<int, int> tree = new();
        tree.Add(0, 3, 0);
        tree.Add(5, 7, 1);
        tree.Add(9, 12, 2);
        
        Assert.AreEqual(null, tree.QueryBefore(-2));
        Assert.AreEqual(null, tree.QueryBefore(-1));
        Assert.AreEqual(null, tree.QueryBefore(0));
        Assert.AreEqual(null, tree.QueryBefore(1));
        Assert.AreEqual(null, tree.QueryBefore(2));
        Assert.AreEqual(null, tree.QueryBefore(3));
        Assert.AreEqual(0, tree.QueryBefore(4)!.Value.value);
        Assert.AreEqual(0, tree.QueryBefore(5)!.Value.value);
        Assert.AreEqual(0, tree.QueryBefore(6)!.Value.value);
        Assert.AreEqual(0, tree.QueryBefore(7)!.Value.value);
        Assert.AreEqual(1, tree.QueryBefore(8)!.Value.value);
        Assert.AreEqual(1, tree.QueryBefore(9)!.Value.value);
        Assert.AreEqual(1, tree.QueryBefore(10)!.Value.value);
        Assert.AreEqual(1, tree.QueryBefore(11)!.Value.value);
        Assert.AreEqual(1, tree.QueryBefore(12)!.Value.value);
        Assert.AreEqual(2, tree.QueryBefore(13)!.Value.value);
        Assert.AreEqual(2, tree.QueryBefore(14)!.Value.value);
    }
    
    [Test]
    public void TestQueryAfter()
    {
        IntervalTree<int, int> tree = new();
        tree.Add(0, 3, 0);
        tree.Add(5, 7, 1);
        tree.Add(9, 12, 2);
        
        Assert.AreEqual(0, tree.QueryAfter(-2)!.Value.value);
        Assert.AreEqual(0, tree.QueryAfter(-1)!.Value.value);
        Assert.AreEqual(1, tree.QueryAfter(0)!.Value.value);
        Assert.AreEqual(1, tree.QueryAfter(1)!.Value.value);
        Assert.AreEqual(1, tree.QueryAfter(2)!.Value.value);
        Assert.AreEqual(1, tree.QueryAfter(3)!.Value.value);
        Assert.AreEqual(1, tree.QueryAfter(4)!.Value.value);
        Assert.AreEqual(2, tree.QueryAfter(5)!.Value.value);
        Assert.AreEqual(2, tree.QueryAfter(6)!.Value.value);
        Assert.AreEqual(2, tree.QueryAfter(7)!.Value.value);
        Assert.AreEqual(2, tree.QueryAfter(8)!.Value.value);
        Assert.AreEqual(null, tree.QueryAfter(9));
        Assert.AreEqual(null, tree.QueryAfter(10));
        Assert.AreEqual(null, tree.QueryAfter(11));
    }
}