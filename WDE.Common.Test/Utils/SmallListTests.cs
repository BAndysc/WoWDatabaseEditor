using System;
using System.Collections.Generic;
using NUnit.Framework;
using WDE.Common.Utils;

namespace WDE.Common.Test.Utils;

[TestFixture]
public class SmallListTests
{
    [Test]
    public void Test_Add()
    {
        var list = new SmallList<int>();
        
        // Adding first 3 items to list
        for (int i = 1; i <= 3; i++)
        {
            list.Add(i);
            Assert.AreEqual(i, list.Count);
            Assert.AreEqual(i, list[i - 1]);
        }
        
        // Adding more items to list
        list.Add(4);
        list.Add(5);
        
        Assert.AreEqual(5, list.Count);
        Assert.AreEqual(4, list[3]);
        Assert.AreEqual(5, list[4]);
    }

    [Test]
    public void Test_Contains()
    {
        var list = new SmallList<int>(1, 2, 3, 4, 5);
        Assert.IsTrue(list.Contains(1));
        Assert.IsTrue(list.Contains(2));
        Assert.IsTrue(list.Contains(3));
        Assert.IsTrue(list.Contains(4));
        Assert.IsTrue(list.Contains(5));
        Assert.IsFalse(list.Contains(6));
    }

    [Test]
    public void Test_CopyTo()
    {
        var list = new SmallList<int>(1, 2, 3, 4, 5);
        int[] arr = new int[5];
        list.CopyTo(arr, 0);
        
        Assert.AreEqual(new[] {1, 2, 3, 4, 5}, arr);
    }

    [TestCase(0, ExpectedResult = -1)]
    [TestCase(1, ExpectedResult = 0)]
    [TestCase(2, ExpectedResult = 1)]
    [TestCase(3, ExpectedResult = 2)]
    [TestCase(4, ExpectedResult = 3)]
    [TestCase(5, ExpectedResult = 4)]
    public int Test_IndexOf(int item)
    {
        var list = new SmallList<int>(1, 2, 3, 4, 5);
        return list.IndexOf(item);
    }

    [Test]
    public void Test_Insert()
    {
        var list = new SmallList<int>();
        list.Insert(0, 1);
        Assert.AreEqual(1, list[0]);

        list.Insert(1, 2);
        Assert.AreEqual(2, list[1]);

        list.Insert(1, 3);
        Assert.AreEqual(3, list[1]);

        list.Insert(3, 4);
        Assert.AreEqual(4, list[3]);

        list.Insert(4, 5);
        Assert.AreEqual(5, list[4]);
    }

    [Test]
    public void Test_Remove()
    {
        var list = new SmallList<int>(1, 2, 3, 4, 5);
        Assert.IsTrue(list.Remove(1));
        Assert.AreEqual(4, list.Count);
        Assert.IsFalse(list.Contains(1));
        
        Assert.IsTrue(list.Remove(2));
        Assert.AreEqual(3, list.Count);
        Assert.IsFalse(list.Contains(2));
        
        Assert.IsTrue(list.Remove(5));
        Assert.AreEqual(2, list.Count);
        Assert.IsFalse(list.Contains(5));

        Assert.IsFalse(list.Remove(6));
    }

    [Test]
    public void Test_RemoveAt()
    {
        var list = new SmallList<int>(1, 2, 3, 4, 5);
        
        list.RemoveAt(0);
        Assert.AreEqual(4, list.Count);
        
        list.RemoveAt(0);
        Assert.AreEqual(3, list.Count);
        
        list.RemoveAt(2);
        Assert.AreEqual(2, list.Count);
        
        Assert.Throws<IndexOutOfRangeException>(() => list.RemoveAt(2));
    }

    [TestCase(0, ExpectedResult = 1)]
    [TestCase(1, ExpectedResult = 2)]
    [TestCase(2, ExpectedResult = 3)]
    [TestCase(3, ExpectedResult = 4)]
    [TestCase(4, ExpectedResult = 5)]
    public int Test_Indexer_Get(int index)
    {
        var list = new SmallList<int>(1, 2, 3, 4, 5);
        return list[index];
    }

    [Test]
    public void Test_Indexer_Set()
    {
        var list = new SmallList<int>(1, 2, 3, 4, 5);
        list[0] = 6;
        Assert.AreEqual(6, list[0]);
        
        list[1] = 7;
        Assert.AreEqual(7, list[1]);

        list[4] = 8;
        Assert.AreEqual(8, list[4]);
        
        Assert.Throws<ArgumentOutOfRangeException>(() => list[5] = 9);
    }

    [Test]
    public void Test_Clear()
    {
        var list = new SmallList<int>(1, 2, 3, 4, 5);
        list.Clear();
        Assert.AreEqual(0, list.Count);
    }

    [Test]
    public void Test_Constructor_FromIEnumerable()
    {
        var source = new[] {1, 2, 3, 4, 5};
        var list = new SmallList<int>(source);
        
        Assert.AreEqual(5, list.Count);
        Assert.AreEqual(1, list[0]);
        Assert.AreEqual(2, list[1]);
        Assert.AreEqual(3, list[2]);
        Assert.AreEqual(4, list[3]);
        Assert.AreEqual(5, list[4]);
    }

    [Test]
    public void Test_GetEnumerator()
    {
        var list = new SmallList<int>(1, 2, 3, 4, 5);
        int expectedValue = 1;

        foreach (int value in list)
        {
            Assert.AreEqual(expectedValue, value);
            expectedValue++;
        }
    }

    [Test]
    public void Test_GetEnumerator_NonGeneric()
    {
        var list = new SmallList<int>(1, 2, 3, 4, 5);
        int expectedValue = 1;

        foreach (int value in (System.Collections.IEnumerable)list)
        {
            Assert.AreEqual(expectedValue, value);
            expectedValue++;
        }
    }

    [Test]
    public void Test_IsReadOnly()
    {
        var list = new SmallList<int>();
        Assert.IsFalse(list.IsReadOnly);
    }

    [Test]
    public void Test_Insert_OutOfRange()
    {
        var list = new SmallList<int>();
        Assert.Throws<IndexOutOfRangeException>(() => list.Insert(-1, 1));
        Assert.Throws<IndexOutOfRangeException>(() => list.Insert(2, 1));

        list.Add(1);
        list.Add(2);
        list.Add(3);

        Assert.Throws<IndexOutOfRangeException>(() => list.Insert(5, 1));
    }

    [Test]
    public void Test_Insert_AtIndexTwo()
    {
        var list = new SmallList<int>(1, 2);
        list.Insert(2, 3);

        Assert.AreEqual(3, list.Count);
        Assert.AreEqual(3, list[2]);
    }

    [Test]
    public void Test_RemoveAt_IndexGreaterThanTwo()
    {
        var list = new SmallList<int>(1, 2, 3, 4, 5);

        list.RemoveAt(3);
        Assert.AreEqual(4, list.Count);
        Assert.AreEqual(5, list[3]);

        list.RemoveAt(3);
        Assert.AreEqual(3, list.Count);

        Assert.Throws<IndexOutOfRangeException>(() => list.RemoveAt(3));
    }

    [Test]
    public void Test_Indexer_Get_OutOfRange()
    {
        var list = new SmallList<int>(1, 2, 3, 4, 5);
    
        Assert.Throws<ArgumentOutOfRangeException>(() => { var temp = list[-1]; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { var temp = list[5]; });
    }

    [Test]
    public void Test_Indexer_Set_OutOfRange()
    {
        var list = new SmallList<int>(1, 2, 3, 4, 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => list[-1] = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => list[5] = 6);
    }
    
    [Test]
    public void SmokeTest_SmallListVersusList()
    {
        var smallList = new SmallList<int>();
        var standardList = new List<int>();

        var random = new Random();

        for (int i = 0; i < 2000; i++)
        {
            int operation = random.Next(5);

            switch (operation)
            {
                case 0: // Add
                    int valueToAdd = random.Next(1000);
                    smallList.Add(valueToAdd);
                    standardList.Add(valueToAdd);
                    break;

                case 1: // Remove
                    if (standardList.Count > 0)
                    {
                        int valueToRemove = standardList[random.Next(standardList.Count)];
                        smallList.Remove(valueToRemove);
                        standardList.Remove(valueToRemove);
                    }
                    break;

                case 2: // IndexOf
                    if (standardList.Count > 0)
                    {
                        int valueToFind = standardList[random.Next(standardList.Count)];
                        int indexFromSmallList = smallList.IndexOf(valueToFind);
                        int indexFromStandardList = standardList.IndexOf(valueToFind);
                        Assert.AreEqual(indexFromStandardList, indexFromSmallList);
                    }
                    break;

                case 3: // Clear
                    if (smallList.Count > 6)
                    {
                        smallList.Clear();
                        standardList.Clear();
                    }
                    break;

                case 4: // Insert
                    int indexToInsertAt = random.Next(standardList.Count + 1);
                    int valueToInsert = random.Next(1000);
                    smallList.Insert(indexToInsertAt, valueToInsert);
                    standardList.Insert(indexToInsertAt, valueToInsert);
                    break;
            }

            // Verify that both lists are equivalent after each operation.
            CollectionAssert.AreEqual(standardList, smallList);
        }
    }
}