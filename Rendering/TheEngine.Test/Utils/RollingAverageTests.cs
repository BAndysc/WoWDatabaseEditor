using System;
using NUnit.Framework;
using TheEngine.Utils;

namespace TheEngine.Test.Utils
{
    public class RollingAverageTests
    {
        [Test]
        public void Test1()
        {
            RollingAverage avg = new();
            for (int i = 0; i < 32; ++i)
                avg.Add(32);
            Assert.AreEqual(32, avg.Average, float.Epsilon);
            
            avg.Add(0);
            
            Assert.AreEqual(31, avg.Average, float.Epsilon);
        }
    }
}