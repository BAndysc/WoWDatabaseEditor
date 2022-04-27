using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using NUnit.Framework;
using WDE.Common.Utils;

namespace WDE.Common.Test.Utils;

public class LinqExtensionsTests
{
    [Test]
    public void RemoveAllTest()
    {
        ObservableCollection<int> ints = new() {0, 1, 2, 3, 4, 5};

        List<int> receivedRemoved = new();
        List<int> indices = new();
        ints.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                int i = 0;
                foreach (int old in e.OldItems!)
                {
                    
                    receivedRemoved.Add(old);
                    indices.Add(e.OldStartingIndex + i++);
                }
            }
        };
        ints.RemoveAll();
            
        CollectionAssert.AreEqual(new []{5, 4, 3, 2, 1, 0}, receivedRemoved);
        CollectionAssert.AreEqual(new []{5, 4, 3, 2, 1, 0}, indices);
    }
}