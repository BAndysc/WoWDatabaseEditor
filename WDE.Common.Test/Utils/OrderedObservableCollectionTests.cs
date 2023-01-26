using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WDE.Common.Utils;

namespace WDE.Common.Test.Utils;

public class OrderedObservableCollectionTests
{
    static void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }
    
    static void DoPermute<T>(T[] nums, int start, int end, Action<T[]> action)
    {
        if (start == end)
        {
            action(nums);
        }
        else
        {
            for (var i = start; i <= end; i++)
            {
                Swap(ref nums[start], ref nums[i]);
                DoPermute(nums, start + 1, end, action);
                Swap(ref nums[start], ref nums[i]); // reset for next pass
            }
        }
    }
    
    [Test]
    public void TestAdd()
    {
        int[] nums = new int[] { 1, 2, 3, 4, 5, 6 };

        OrderedObservableCollection<int> collection = new OrderedObservableCollection<int>(Comparer<int>.Default);
        DoPermute(nums.ToArray(), 0, nums.Length - 1, perm =>
        {
            collection.Clear();
            foreach (var i in nums)
                collection.Add(i);
            CollectionAssert.AreEqual(nums, collection);
        });
    }
}