using System.Collections.Generic;
using NUnit.Framework;
using WDE.Common.Utils;

namespace WDE.Common.Test.Utils;

public class HierarchicalIndexContainerTests
{
    public VerticalCursor C(int group, int row)
    {
        return new VerticalCursor(group, row);
    }
    
    [Test]
    public void Test_TableMultiSelection_Add_Remove_All()
    {
        TableMultiSelection sel = new TableMultiSelection();
        
        CollectionAssert.IsEmpty(sel.All());
        CollectionAssert.IsEmpty(sel.AllReversed());
        Assert.IsTrue(sel.Empty);
        Assert.IsFalse(sel.MoreThanOne);
        
        sel.Add(C(2, 3));
        
        CollectionAssert.AreEqual(new[]{C(2, 3)}, sel.All());
        CollectionAssert.AreEqual(new[]{C(2, 3)}, sel.AllReversed());
        Assert.IsFalse(sel.Empty);
        Assert.IsFalse(sel.MoreThanOne);
        
        sel.Add(C(2, 1));
        
        CollectionAssert.AreEqual(new[]{C(2, 1), C(2, 3)}, sel.All());
        CollectionAssert.AreEqual(new[]{C(2, 3), C(2, 1)}, sel.AllReversed());
        Assert.IsFalse(sel.Empty);
        Assert.IsTrue(sel.MoreThanOne);

        sel.Add(C(1, 2));
        
        CollectionAssert.AreEqual(new[]{C(1, 2), C(2, 1), C(2, 3)}, sel.All());
        CollectionAssert.AreEqual(new[]{C(2, 3), C(2, 1), C(1, 2)}, sel.AllReversed());
        Assert.IsFalse(sel.Empty);
        Assert.IsTrue(sel.MoreThanOne);
        
        sel.Remove(C(2, 1));
        
        CollectionAssert.AreEqual(new[]{C(1, 2), C(2, 3)}, sel.All());
        CollectionAssert.AreEqual(new[]{C(2, 3), C(1, 2)}, sel.AllReversed());
        Assert.IsFalse(sel.Empty);
        Assert.IsTrue(sel.MoreThanOne);
        
        sel.Remove(C(2, 3));
        
        CollectionAssert.AreEqual(new[]{C(1, 2)}, sel.All());
        CollectionAssert.AreEqual(new[]{C(1, 2)}, sel.AllReversed());
        Assert.IsFalse(sel.Empty);
        Assert.IsFalse(sel.MoreThanOne);
    }
    
    [Test]
    public void Test_TableMultiSelection_Efficient_Contains()
    {
        TableMultiSelection sel = new TableMultiSelection();

        List<VerticalCursor> selected = new List<VerticalCursor>()
        {
            C(3, 1),
            C(2, 2),
            C(3, 3),
            C(2, 0),
            C(1, 2),
            C(1, 3),
            C(0, 4),
        };
        
        foreach (var s in selected)
            sel.Add(s);

        using var containsIterator = sel.ContainsIterator;
        
        for (int group = 0; group < 4; group++)
        {
            for (int row = 0; row < 5; row++)
            {
                var shouldContain = selected.Contains(C(group, row));
                var contains = containsIterator.Contains(C(group, row));
                
                Assert.AreEqual(shouldContain, contains);
            }
        }
    }
}