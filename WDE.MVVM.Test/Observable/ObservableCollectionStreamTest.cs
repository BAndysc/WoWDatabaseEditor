using System.Collections.Generic;
using System.Collections.ObjectModel;
using NUnit.Framework;
using WDE.MVVM.Observable;

namespace WDE.MVVM.Test.Observable
{
    public class ObservableCollectionStreamTest
    {
        [Test]
        public void AddTest()
        {
            ObservableCollection<int> ints = new() {1, 2, 3};

            List<int> receivedAdded = new();
            List<int> indices = new();
            using var disp = ints.ToStream(false)
                .SubscribeAction(e =>
                {
                    Assert.AreEqual(CollectionEventType.Add, e.Type);
                    receivedAdded.Add(e.Item);
                    indices.Add(e.Index);
                });
            
            CollectionAssert.AreEqual(new []{1, 2, 3}, receivedAdded);
            CollectionAssert.AreEqual(new []{0, 1, 2}, indices);
            
            indices.Clear();
            receivedAdded.Clear();

            ints.Add(4);
            
            CollectionAssert.AreEqual(new []{4}, receivedAdded);
            CollectionAssert.AreEqual(new []{3}, indices);
        }
        
        [Test]
        public void RemoveTest()
        {
            ObservableCollection<int> ints = new() {0, 1, 2, 3, 4, 5};
            ints.Remove(5);

            List<int> receivedRemoved = new();
            List<int> indices = new();
            int callTimes = 0;
            using var disp = ints.ToStream(false)
                .SubscribeAction(e =>
                {
                    if (callTimes++ < 5)
                        return;
                    Assert.AreEqual(CollectionEventType.Remove, e.Type);
                    receivedRemoved.Add(e.Item);
                    indices.Add(e.Index);
                });
            
            CollectionAssert.AreEqual(new int[]{}, receivedRemoved);
            CollectionAssert.AreEqual(new int[]{}, indices);
            
            ints.Remove(0);
            ints.Remove(2);
            ints.Remove(7);
            ints.Remove(2);
            
            CollectionAssert.AreEqual(new []{0, 2}, receivedRemoved);
            CollectionAssert.AreEqual(new []{0, 1}, indices);
        }

        [Test]
        public void ReplaceTest()
        {
            ObservableCollection<int> ints = new() {0, 1, 2, 3, 4, 5};

            List<int> receivedAdded = new();
            List<int> receivedRemoved = new();
            List<int> indicesAdded = new();
            List<int> indicesRemoved = new();
            using var disp = ints.ToStream(false)
                .SubscribeAction(e =>
                {
                    if (e.Type == CollectionEventType.Add)
                    {
                        receivedAdded.Add(e.Item);
                        indicesAdded.Add(e.Index);
                    }
                    else
                    {
                        Assert.AreEqual(CollectionEventType.Remove, e.Type);
                        receivedRemoved.Add(e.Item);
                        indicesRemoved.Add(e.Index);
                    }
                });
            
            receivedAdded.Clear();
            indicesAdded.Clear();

            ints[3] = 5;
            
            CollectionAssert.AreEqual(new []{3}, receivedRemoved);
            CollectionAssert.AreEqual(new []{3}, indicesRemoved);
            CollectionAssert.AreEqual(new []{5}, receivedAdded);
            CollectionAssert.AreEqual(new []{3}, indicesAdded);
        }
        
        [Test]
        public void MoveTest()
        {
            ObservableCollection<int> ints = new() {0, 1, 2, 3, 4, 5};

            List<int> receivedAdded = new();
            List<int> receivedRemoved = new();
            List<int> indicesAdded = new();
            List<int> indicesRemoved = new();
            using var disp = ints.ToStream(false)
                .SubscribeAction(e =>
                {
                    if (e.Type == CollectionEventType.Add)
                    {
                        receivedAdded.Add(e.Item);
                        indicesAdded.Add(e.Index);
                    }
                    else
                    {
                        Assert.AreEqual(CollectionEventType.Remove, e.Type);
                        receivedRemoved.Add(e.Item);
                        indicesRemoved.Add(e.Index);
                    }
                });
            
            receivedAdded.Clear();
            indicesAdded.Clear();

            ints.Move(2, 4);
            
            CollectionAssert.AreEqual(new []{2}, receivedRemoved);
            CollectionAssert.AreEqual(new []{2}, indicesRemoved);
            CollectionAssert.AreEqual(new []{2}, receivedAdded);
            CollectionAssert.AreEqual(new []{4}, indicesAdded);
        }
    }
}