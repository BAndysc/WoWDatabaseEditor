using NUnit.Framework;
using WDE.SmartScriptEditor.Validation.Antlr;

namespace WDE.SmartScriptEditor.Test.Validation.Antlr
{
    public class ValidatorContextlessIntTests
    {
        [Test]
        public void IntLiteral()
        {
            Assert.AreEqual(123, new SmartValidator("123").EvaluateInteger(null));
        }
        
        [Test]
        public void Negation()
        {
            Assert.AreEqual(-123, new SmartValidator("-123").EvaluateInteger(null));
        }
        
        [Test]
        public void Add()
        {
            Assert.AreEqual(6, new SmartValidator("2+4").EvaluateInteger(null));
            Assert.AreEqual(6, new SmartValidator("4+2").EvaluateInteger(null));
        }
        
        [Test]
        public void Subtract()
        {
            Assert.AreEqual(-2, new SmartValidator("2-4").EvaluateInteger(null));
            Assert.AreEqual(2, new SmartValidator("4-2").EvaluateInteger(null));
        }
        
        [Test]
        public void Multiply()
        {
            Assert.AreEqual(8, new SmartValidator("2*4").EvaluateInteger(null));
            Assert.AreEqual(8, new SmartValidator("4*2").EvaluateInteger(null));
        }
        
        [Test]
        public void Division()
        {
            Assert.AreEqual(8, new SmartValidator("16/2").EvaluateInteger(null));
            Assert.AreEqual(0, new SmartValidator("2/16").EvaluateInteger(null));
        }
        
        [Test]
        public void Modulo()
        {
            Assert.AreEqual(1, new SmartValidator("3%2").EvaluateInteger(null));
            Assert.AreEqual(2, new SmartValidator("2%3").EvaluateInteger(null));
        }
        
        [Test]
        public void OperatorPrecedence()
        {
            Assert.AreEqual(7, new SmartValidator("1+2*3").EvaluateInteger(null));
            Assert.AreEqual(9, new SmartValidator("(1+2)*3").EvaluateInteger(null));
            Assert.AreEqual(1, new SmartValidator("1+2-3+4-3").EvaluateInteger(null));
            Assert.AreEqual(-1, new SmartValidator("1+(2-((3+4)-3))").EvaluateInteger(null));
        }
    }
}