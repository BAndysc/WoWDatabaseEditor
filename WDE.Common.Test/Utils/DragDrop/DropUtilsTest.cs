using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WDE.Common.Utils.DragDrop;

namespace WDE.Common.Test.Utils.DragDrop
{
    public class DropUtilsTest
    {
        private List<int> l = null!;

        [SetUp]
        public void Setup()
        {
            l = new List<int>() { 1, 2, 3, 4, 5 };
        }

        private void PerformTest(int dropIndex, int[] expected, params int[] itemsToDrop)
        {
            var drop = new DropUtils<int>(l, itemsToDrop.ToList(), dropIndex);
            drop.DoDrop(l);
            CollectionAssert.AreEqual(expected, l);
            drop.Undo(l);
            CollectionAssert.AreEqual(new int[]{ 1, 2, 3, 4, 5 }, l);
            drop.DoDrop(l);
            CollectionAssert.AreEqual(expected, l);
        }
        
        [Test]
        public void TestMove1To0() => PerformTest(0, new []{1, 2, 3, 4, 5}, 1);
        
        [Test]
        public void TestMove1To1() => PerformTest(1, new []{1, 2, 3, 4, 5}, 1);
        
        [Test]
        public void TestMove1To2() => PerformTest(2, new []{2, 1, 3, 4, 5}, 1);
        
        [Test]
        public void TestMove1To3() => PerformTest(3, new []{2, 3, 1, 4, 5}, 1);
        
        [Test]
        public void TestMove1To4() => PerformTest(4, new []{2, 3, 4, 1, 5}, 1);
        
        [Test]
        public void TestMove1To5() => PerformTest(5, new []{2, 3, 4, 5, 1}, 1);

        [Test]
        public void TestMove2To0() => PerformTest(0, new []{2, 1, 3, 4, 5}, 2);
        
        [Test]
        public void TestMove2To1() => PerformTest(1, new []{1, 2, 3, 4, 5}, 2);
        
        [Test]
        public void TestMove2To2() => PerformTest(2, new []{1, 2, 3, 4, 5}, 2);
        
        [Test]
        public void TestMove2To3() => PerformTest(3, new []{1, 3, 2, 4, 5}, 2);
        
        [Test]
        public void TestMove2To4() => PerformTest(4, new []{1, 3, 4, 2, 5}, 2);
        
        [Test]
        public void TestMove2To5() => PerformTest(5, new []{1, 3, 4, 5, 2}, 2);
        
        [Test]
        public void TestMove5To0() => PerformTest(0, new []{5, 1, 2, 3, 4}, 5);
        
        [Test]
        public void TestMove5To1() => PerformTest(1, new []{1, 5, 2, 3, 4}, 5);
        
        [Test]
        public void TestMove5To2() => PerformTest(2, new []{1, 2, 5, 3, 4}, 5);
        
        [Test]
        public void TestMove5To3() => PerformTest(3, new []{1, 2, 3, 5, 4}, 5);
        
        [Test]
        public void TestMove5To4() => PerformTest(4, new []{1, 2, 3, 4, 5}, 5);
        
        [Test]
        public void TestMove5To5() => PerformTest(5, new []{1, 2, 3, 4, 5}, 5);
        
        
        [Test]
        public void TestMove12To0() => PerformTest(0, new []{1, 2, 3, 4, 5}, 1, 2);
        
        [Test]
        public void TestMove12To1() => PerformTest(1, new []{1, 2, 3, 4, 5}, 1, 2);
        
        [Test]
        public void TestMove12To2() => PerformTest(2, new []{1, 2, 3, 4, 5}, 1, 2);
        
        [Test]
        public void TestMove12To3() => PerformTest(3, new []{3, 1, 2, 4, 5}, 1, 2);
        
        [Test]
        public void TestMove12To4() => PerformTest(4, new []{3, 4, 1, 2, 5}, 1, 2);
        
        [Test]
        public void TestMove12To5() => PerformTest(5, new []{3, 4, 5, 1, 2}, 1, 2);
        
        
        [Test]
        public void TestMove45To0() => PerformTest(0, new []{4, 5, 1, 2, 3}, 4, 5);
        
        [Test]
        public void TestMove45To1() => PerformTest(1, new []{1, 4, 5, 2, 3}, 4, 5);
        
        [Test]
        public void TestMove45To2() => PerformTest(2, new []{1, 2, 4, 5, 3}, 4, 5);
        
        [Test]
        public void TestMove45To3() => PerformTest(3, new []{1, 2, 3, 4, 5}, 4, 5);
        
        [Test]
        public void TestMove45To4() => PerformTest(4, new []{1, 2, 3, 4, 5}, 4, 5);
        
        [Test]
        public void TestMove45To5() => PerformTest(5, new []{1, 2, 3, 4, 5}, 4, 5);
        
        
        [Test]
        public void TestMove15To0() => PerformTest(0, new []{1, 5, 2, 3, 4}, 1, 5);
        
        [Test]
        public void TestMove15To1() => PerformTest(1, new []{1, 5, 2, 3, 4}, 1, 5);
        
        [Test]
        public void TestMove15To2() => PerformTest(2, new []{2, 1, 5, 3, 4}, 1, 5);
        
        [Test]
        public void TestMove15To3() => PerformTest(3, new []{2, 3, 1, 5, 4}, 1, 5);
        
        [Test]
        public void TestMove15To4() => PerformTest(4, new []{2, 3, 4, 1, 5}, 1, 5);
        
        [Test]
        public void TestMove15To5() => PerformTest(5, new []{2, 3, 4, 1, 5}, 1, 5);
        
        
        
        
        [Test]
        public void TestMove123To0() => PerformTest(0, new []{1, 2, 3, 4, 5}, 1, 2, 3);
        
        [Test]
        public void TestMove123To1() => PerformTest(1, new []{1, 2, 3, 4, 5}, 1, 2, 3);
        
        [Test]
        public void TestMove123To2() => PerformTest(2, new []{1, 2, 3, 4, 5}, 1, 2, 3);
        
        [Test]
        public void TestMove123To3() => PerformTest(3, new []{1, 2, 3, 4, 5}, 1, 2, 3);
        
        [Test]
        public void TestMove123To4() => PerformTest(4, new []{4, 1, 2, 3, 5}, 1, 2, 3);
        
        [Test]
        public void TestMove123To5() => PerformTest(5, new []{4, 5, 1, 2, 3}, 1, 2, 3);
        
        
        
        [Test]
        public void TestMove124To0() => PerformTest(0, new []{1, 2, 4, 3, 5}, 1, 2, 4);
        [Test]
        public void TestMove124To1() => PerformTest(1, new []{1, 2, 4, 3, 5}, 1, 2, 4);
        [Test]
        public void TestMove124To2() => PerformTest(2, new []{1, 2, 4, 3, 5}, 1, 2, 4);
        [Test]
        public void TestMove124To3() => PerformTest(3, new []{3, 1, 2, 4, 5}, 1, 2, 4);
        [Test]
        public void TestMove124To4() => PerformTest(4, new []{3, 1, 2, 4, 5}, 1, 2, 4);
        [Test]
        public void TestMove124To5() => PerformTest(5, new []{3, 5, 1, 2, 4}, 1, 2, 4);
        
        
        [Test]
        public void TestMove125To0() => PerformTest(0, new []{1, 2, 5, 3, 4}, 1, 2, 5);
        [Test]
        public void TestMove125To1() => PerformTest(1, new []{1, 2, 5, 3, 4}, 1, 2, 5);
        [Test]
        public void TestMove125To2() => PerformTest(2, new []{1, 2, 5, 3, 4}, 1, 2, 5);
        [Test]
        public void TestMove125To3() => PerformTest(3, new []{3, 1, 2, 5, 4}, 1, 2, 5);
        [Test]
        public void TestMove125To4() => PerformTest(4, new []{3, 4, 1, 2, 5}, 1, 2, 5);
        [Test]
        public void TestMove125To5() => PerformTest(5, new []{3, 4, 1, 2, 5}, 1, 2, 5);
        
        
        [Test]
        public void TestMove1234To0() => PerformTest(0, new []{1, 2, 3, 4, 5}, 1, 2, 3, 4);
        [Test]
        public void TestMove1234To1() => PerformTest(1, new []{1, 2, 3, 4, 5}, 1, 2, 3, 4);
        [Test]
        public void TestMove1234To2() => PerformTest(2, new []{1, 2, 3, 4, 5}, 1, 2, 3, 4);
        [Test]
        public void TestMove1234To3() => PerformTest(3, new []{1, 2, 3, 4, 5}, 1, 2, 3, 4);
        [Test]
        public void TestMove1234To4() => PerformTest(4, new []{1, 2, 3, 4, 5}, 1, 2, 3, 4);
        [Test]
        public void TestMove1234To5() => PerformTest(5, new []{5, 1, 2, 3, 4}, 1, 2, 3, 4);
        
        
        [Test]
        public void TestMove12345To0() => PerformTest(0, new []{1, 2, 3, 4, 5}, 1, 2, 3, 4, 5);
        [Test]
        public void TestMove12345To1() => PerformTest(1, new []{1, 2, 3, 4, 5}, 1, 2, 3, 4, 5);
        [Test]
        public void TestMove12345To2() => PerformTest(2, new []{1, 2, 3, 4, 5}, 1, 2, 3, 4, 5);
        [Test]
        public void TestMove12345To3() => PerformTest(3, new []{1, 2, 3, 4, 5}, 1, 2, 3, 4, 5);
        [Test]
        public void TestMove12345To4() => PerformTest(4, new []{1, 2, 3, 4, 5}, 1, 2, 3, 4, 5);
        [Test]
        public void TestMove12345To5() => PerformTest(5, new []{1, 2, 3, 4, 5}, 1, 2, 3, 4, 5);
    }
}