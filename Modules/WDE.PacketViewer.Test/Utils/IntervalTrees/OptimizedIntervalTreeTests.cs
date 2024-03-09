using System.Runtime.Versioning;
using NUnit.Framework;
using WDE.PacketViewer.Utils.IntervalTrees;

namespace WDE.PacketViewer.Test.Utils.IntervalTrees;

[RequiresPreviewFeatures]
public class OptimizedIntervalTreeTests
{
    [Test]
    public void TestQuery()
    {
        OptimizedIntervalTree<int, int> tree = new(0, 3, 1);
        
        Assert.AreEqual(null, tree.Query(-2));
        Assert.AreEqual(null, tree.Query(-1));
        Assert.AreEqual(1, tree.Query(0)!.Value.value);
        Assert.AreEqual(1, tree.Query(1)!.Value.value);
        Assert.AreEqual(1, tree.Query(2)!.Value.value);
        Assert.AreEqual(1, tree.Query(3)!.Value.value);
        Assert.AreEqual(null, tree.Query(4));
    }
    
    [Test]
    public void TestQueryBefore()
    {
        OptimizedIntervalTree<int, int> tree = new(0, 3, 1);
        
        Assert.AreEqual(null, tree.QueryBefore(-2));
        Assert.AreEqual(null, tree.QueryBefore(-1));
        Assert.AreEqual(null, tree.QueryBefore(0));
        Assert.AreEqual(null, tree.QueryBefore(1));
        Assert.AreEqual(null, tree.QueryBefore(2));
        Assert.AreEqual(null, tree.QueryBefore(3));
        Assert.AreEqual(1, tree.QueryBefore(4)!.Value.value);
    }
    
    [Test]
    public void TestQueryAfter()
    {
        OptimizedIntervalTree<int, int> tree = new(0, 3, 1);
        
        Assert.AreEqual(1, tree.QueryAfter(-2)!.Value.value);
        Assert.AreEqual(1, tree.QueryAfter(-1)!.Value.value);
        Assert.AreEqual(null, tree.QueryAfter(0));
        Assert.AreEqual(null, tree.QueryAfter(1));
        Assert.AreEqual(null, tree.QueryAfter(2));
        Assert.AreEqual(null, tree.QueryAfter(3));
        Assert.AreEqual(null, tree.QueryAfter(4));
    }
}