using NUnit.Framework;
using WDE.SmartScriptEditor.Validation;
using WDE.SmartScriptEditor.Validation.Antlr;

namespace WDE.SmartScriptEditor.Test.Validation.Antlr
{
    public class ValidatorContextlessBoolTests
    {
        [Test]
        public void TestTrue()
        {
            Assert.True(new SmartValidator("true").Evaluate(null));
        }
        
        [Test]
        public void TestFalse()
        {
            Assert.False(new SmartValidator("false").Evaluate(null));
        }
        
        [Test]
        public void TestNegate()
        {
            Assert.True(new SmartValidator("!false").Evaluate(null));
        }
        
        [Test]
        public void TestEquals()
        {
            Assert.True(new SmartValidator("!false==true").Evaluate(null));
            Assert.True(new SmartValidator("true==true").Evaluate(null));
        }
        
        [Test]
        public void TestNotEquals()
        {
            Assert.True(new SmartValidator("false!=true").Evaluate(null));
            Assert.True(new SmartValidator("true!=false").Evaluate(null));
        }

        [Test]
        public void TestCompoundEquality()
        {
            Assert.True(new SmartValidator("true==true!=false").Evaluate(null));
        }
        
        [Test]
        public void TestOr()
        {
            Assert.True(new SmartValidator("true||true").Evaluate(null));
            Assert.False(new SmartValidator("false||false").Evaluate(null));
            Assert.True(new SmartValidator("true||false").Evaluate(null));
            Assert.True(new SmartValidator("false||true").Evaluate(null));
        }
        
        [Test]
        public void TestAnd()
        {
            Assert.True(new SmartValidator("true&&true").Evaluate(null));
            Assert.False(new SmartValidator("false&&false").Evaluate(null));
            Assert.False(new SmartValidator("true&&false").Evaluate(null));
            Assert.False(new SmartValidator("false&&true").Evaluate(null));
        }
        
        [Test]
        public void TestCompoundLogical()
        {
            Assert.True(new SmartValidator("true || false && true").Evaluate(null));
            Assert.True(new SmartValidator("true && false || true").Evaluate(null));
        }
        
        [Test]
        public void TestInvalid1()
        {
            Assert.Throws<ValidationParseException>(() => new SmartValidator("flse").Evaluate(null));
        }
        
        [Test]
        public void TestInvalid2()
        {
            Assert.Throws<ValidationParseException>(() => new SmartValidator("4").Evaluate(null));
        }
        
        [Test]
        public void Comparisons()
        {
            Assert.True(new SmartValidator("1==1").Evaluate(null));
            Assert.True(new SmartValidator("1<2").Evaluate(null));
            Assert.True(new SmartValidator("1>0").Evaluate(null));
            Assert.True(new SmartValidator("1>=1").Evaluate(null));
            Assert.True(new SmartValidator("1<=1").Evaluate(null));
            Assert.True(new SmartValidator("1!=2").Evaluate(null));
        }
        
        [Test]
        public void CompoundComparisons()
        {
            Assert.True(new SmartValidator("1-1==2-2").Evaluate(null));
            Assert.True(new SmartValidator("1-1<2+1").Evaluate(null));
            Assert.True(new SmartValidator("1*2>0-1").Evaluate(null));
            Assert.True(new SmartValidator("1*3+1>=1/1").Evaluate(null));
            Assert.True(new SmartValidator("1+2<=1+1*2").Evaluate(null));
            Assert.True(new SmartValidator("3!=3-1*2").Evaluate(null));
        }
    }
}