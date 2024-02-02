using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using NUnit.Framework;
using WDE.Common.Utils;

namespace WDE.Common.Test.Utils;

public class FlatTreeListTests
{
    [Test]
    public void TestSimple()
    {
        List<ParentType> parents = new();
        var p = new ParentType(true, new ParentType(true, new Child(3)), new Child(2));
        parents.Add(p);
        FlatTreeList<ParentType, Child> flat = new FlatTreeList<ParentType, Child>(parents);
        
        CollectionAssert.AreEqual(new object[]
        {
            p,
            p.nested[0],
            p.nested[0].children[0],
            p.children[0]
        }, flat);

        p.nested[0].IsExpanded = false;
        CollectionAssert.AreEqual(new object[]
        {
            p,
            p.nested[0],
            p.children[0]
        }, flat);
        
        p.nested[0].IsExpanded = true;
        p.IsExpanded = false;
        CollectionAssert.AreEqual(new object[]
        {
            p
        }, flat);
        
        p.IsExpanded = true;
        CollectionAssert.AreEqual(new object[]
        {
            p,
            p.nested[0],
            p.nested[0].children[0],
            p.children[0]
        }, flat);
    }
    
    [Test]
    public void TestDynamic()
    {
        ObservableCollection<ParentType> parents = new();
        var p = new ParentType(true, new ParentType(true, new Child(3)), new Child(2));
        FlatTreeList<ParentType, Child> flat = new FlatTreeList<ParentType, Child>(parents);

        Assert.IsTrue(flat.Count == 0);
        
        parents.Add(p);

        CollectionAssert.AreEqual(new object[]
        {
            p,
            p.nested[0],
            p.nested[0].children[0],
            p.children[0]
        }, flat);

        var p2 = new ParentType(false, new Child(1));
        parents.Add(p2);
        CollectionAssert.AreEqual(new object[]
        {
            p,
            p.nested[0],
            p.nested[0].children[0],
            p.children[0],
            p2
        }, flat);

        p2.IsExpanded = true;
        CollectionAssert.AreEqual(new object[]
        {
            p,
            p.nested[0],
            p.nested[0].children[0],
            p.children[0],
            p2,
            p2.children[0]
        }, flat);
    }

    [Test]
    public void Test_NestedParentCanBeRemoved()
    {
        var p = new ParentType(true, new ParentType(true, new Child(3)), new Child(2));
        ObservableCollection<ParentType> parents = new() { p };
        FlatTreeList<ParentType, Child> flat = new FlatTreeList<ParentType, Child>(parents);

        p.nested.RemoveAt(0);
        
        CollectionAssert.AreEqual(new object[]
        {
            p,
            p.children[0]
        }, flat);
    }
    
    [Test]
    public void Test_NestedParentCanBeAdded()
    {
        var p = new ParentType(true,  new Child(2));
        ObservableCollection<ParentType> parents = new() { p };
        FlatTreeList<ParentType, Child> flat = new FlatTreeList<ParentType, Child>(parents);

        p.nested.Add(new ParentType(true, new Child(3)));
        
        CollectionAssert.AreEqual(new object[]
        {
            p,
            p.nested[0],
            p.nested[0].children[0],
            p.children[0]
        }, flat);
    }

    [Test]
    public void Test_DoubleNestedParentCanBeRemoved()
    {
        var p = new ParentType(true, new ParentType(true, new ParentType(true, new Child(4)), new Child(3)), new Child(2));
        ObservableCollection<ParentType> parents = new() { p };
        FlatTreeList<ParentType, Child> flat = new FlatTreeList<ParentType, Child>(parents);

        Assert.AreEqual(6, flat.Count);
        p.nested.RemoveAt(0);
        
        CollectionAssert.AreEqual(new object[]
        {
            p,
            p.children[0]
        }, flat);
    }
    
    [Test]
    public void Test_ChildCanBeRemoved()
    {
        var p = new ParentType(true, new ParentType(true, new Child(3)), new Child(2), new Child(1));
        ObservableCollection<ParentType> parents = new() { p };
        FlatTreeList<ParentType, Child> flat = new FlatTreeList<ParentType, Child>(parents);

        p.children.RemoveAt(1);
        
        CollectionAssert.AreEqual(new object[]
        {
            p,
            p.nested[0],
            p.nested[0].children[0],
            p.children[0]
        }, flat);
        Assert.AreEqual(2, p.children[0].Value);
    }
    
    [Test]
    public void Test_ChildCanBeAdded()
    {
        var p = new ParentType(true, new ParentType(true, new Child(3)), new Child(2));
        ObservableCollection<ParentType> parents = new() { p };
        FlatTreeList<ParentType, Child> flat = new FlatTreeList<ParentType, Child>(parents);

        p.children.Add( new Child(1));
        
        CollectionAssert.AreEqual(new object[]
        {
            p,
            p.nested[0],
            p.nested[0].children[0],
            p.children[0],
            p.children[1]
        }, flat);
        Assert.AreEqual(2, p.children[0].Value);
        Assert.AreEqual(1, p.children[1].Value);
    }

    [Test]
    public void Test_ParentCanBeRemoved()
    {
        var p = new ParentType(true, new ParentType(true, new Child(3)), new Child(2));
        var p2 = new ParentType(true, new ParentType(true, new Child(3)), new Child(2));
        ObservableCollection<ParentType> parents = new() { p, p2 };
        FlatTreeList<ParentType, Child> flat = new FlatTreeList<ParentType, Child>(parents);

        parents.RemoveAt(1);
        parents.RemoveAt(0);

        CollectionAssert.AreEqual(new object[]
        {
        }, flat);
    }

    private class ParentType : IParentType, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event NotifyCollectionChangedEventHandler? ChildrenChanged;
        public event NotifyCollectionChangedEventHandler? NestedParentsChanged;

        private bool isExpanded;

        public bool CanBeExpanded => true;
        public uint NestLevel { get; set; }
        public bool IsVisible { get; set; }
        public IParentType? Parent { get; set; }

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                isExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }
        public IReadOnlyList<IParentType> NestedParents => nested;
        public IReadOnlyList<IChildType> Children => children;

        public ObservableCollection<Child> children = new();
        public ObservableCollection<ParentType> nested = new();

        public ParentType(bool expanded, params object[] inner)
        {
            isExpanded = expanded;
            foreach (var i in inner)
            {
                if (i is ParentType p)
                    nested.Add(p);
                else if (i is Child c)
                    children.Add(c);
            }

            nested.CollectionChanged += (_, e) => NestedParentsChanged?.Invoke(this, e);
            children.CollectionChanged += (_, e) => ChildrenChanged?.Invoke(this, e);
        }
    }

    private class Child : IChildType
    {
        public Child(int value)
        {
            Value = value;
        }

        public readonly int Value;
        public bool CanBeExpanded => false;
        public uint NestLevel { get; set; }
        public bool IsVisible { get; set; }
        public IParentType? Parent { get; set; }
    }
}