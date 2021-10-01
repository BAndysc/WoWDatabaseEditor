using NUnit.Framework;
using WDE.Common.Utils;

namespace WDE.Common.Test.Utils
{
    public class CoordsParserTests
    {
        private static float? O => null;
    
        [Test]
        public void TestSpaceXyz()
        {
            Assert.AreEqual((1f, 2f, 3f, O), CoordsParser.ExtractCoords("1 2 3"));
            Assert.AreEqual((1.5f, 2.5f, 3.5f, O), CoordsParser.ExtractCoords("1.5 2.5 3.5"));
            Assert.AreEqual((-1.5f, -2.5f, -3.5f, O), CoordsParser.ExtractCoords("-1.5 -2.5 -3.5"));
        }
    
        [Test]
        public void TestCommaXyz()
        {
            Assert.AreEqual((1f, 2f, 3f, O), CoordsParser.ExtractCoords("1, 2, 3"));
        }
    
        [Test]
        public void TestSemicolonXyz()
        {
            Assert.AreEqual((1f, 2f, 3f, O), CoordsParser.ExtractCoords("1; 2; 3"));
        }
    
        [Test]
        public void TestLettersXyz()
        {
            Assert.AreEqual((1f, 2f, 3f, O), CoordsParser.ExtractCoords("X 1 Y 2 Z 3"));
        }
    
        [Test]
        public void TestLettersXyzo()
        {
            Assert.AreEqual((1f, 2f, 3f, 4f), CoordsParser.ExtractCoords("X 1 Y 2 Z 3 O 4"));
        }
    
        [Test]
        public void TestLettersColonXyz()
        {
            Assert.AreEqual((1f, 2f, 3f, O), CoordsParser.ExtractCoords("X: 1 Y: 2 Z: 3"));
        }
    
        [Test]
        public void TestLettersColonXyzo()
        {
            Assert.AreEqual((1f, 2f, 3f, 4f), CoordsParser.ExtractCoords("X: 1 Y: 2 Z: 3 O: 4"));
        }
    
        [Test]
        public void TestMixedXyz()
        {
            Assert.AreEqual((1f, 2f, 3f, 4f), CoordsParser.ExtractCoords("1 2, 3; 4"));
        }
    
        [Test]
        public void TestRandomPrefix()
        {
            Assert.AreEqual((1f, 2f, 3f, 4f), CoordsParser.ExtractCoords("[1] position: 1, 2, 3, 4"));
        }
    
        [Test]
        public void TestRandomSuffix()
        {
            Assert.AreEqual((1f, 2f, 3f, 4f), CoordsParser.ExtractCoords("1, 2, 3, 4[random]"));
        }
    }
}