using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using WDE.Common.Database;
using WDE.SmartScriptEditor.Validation;
using WDE.SmartScriptEditor.Validation.Antlr;

namespace WDE.SmartScriptEditor.Test.Validation.Antlr
{
    public class ValidatorContextTests
    {
        private ISmartValidationContext context;
        
        [SetUp]
        public void Setup()
        {
            context = new TestContext();
        }

        [Test]
        public void TestScriptType()
        {
            Assert.True(new SmartValidator($"script.source_type == 4").Evaluate(context));
        }
        
        [Test]
        public void TestEventParams()
        {
            Assert.True(new SmartValidator($"event.param(1) == 2").Evaluate(context));
            Assert.True(new SmartValidator($"event.param(2) == 4").Evaluate(context));
            Assert.True(new SmartValidator($"event.param(3) == 6").Evaluate(context));
            Assert.True(new SmartValidator($"event.param(4) == 8").Evaluate(context));
        }
        
        [Test]
        public void TestEventInvalidParams()
        {
            Assert.Throws<ValidationParseException>(() => new SmartValidator($"event.param(0) == 2").Evaluate(context));
            Assert.Throws<ValidationParseException>(() => new SmartValidator($"event.param(5) == 2").Evaluate(context));
        }
        
        [Test]
        public void TestActionParams()
        {
            Assert.True(new SmartValidator($"action.param(1) == 3").Evaluate(context));
            Assert.True(new SmartValidator($"action . param(2) == 6").Evaluate(context));
            Assert.True(new SmartValidator($"action .param(3) == 9").Evaluate(context));
            Assert.True(new SmartValidator($"action. param(4) == 12").Evaluate(context));
            Assert.True(new SmartValidator($"action  .  param(5) == 15").Evaluate(context));
            Assert.True(new SmartValidator($"action.param(6) == 18").Evaluate(context));
            Assert.Throws<ValidationParseException>(() => new SmartValidator($"action.param(7) == 21").Evaluate(context));
        }

        [Test]
        public void TestSourceParams()
        {
            Assert.True(new SmartValidator($"source.param(1) == 4").Evaluate(context));
            Assert.True(new SmartValidator($"source.param(2) == 8").Evaluate(context));
            Assert.True(new SmartValidator($"source.param(3) == 12").Evaluate(context));
            Assert.Throws<ValidationParseException>(() => new SmartValidator($"source.param(4) == 16").Evaluate(context));
        }
        
        [Test]
        public void TestTargetParams()
        {
            Assert.True(new SmartValidator($"target.param(1) == 5").Evaluate(context));
            Assert.True(new SmartValidator($"target.param(2) == 10").Evaluate(context));
            Assert.True(new SmartValidator($"target.param(3) == 15").Evaluate(context));
            Assert.Throws<ValidationParseException>(() => new SmartValidator($"target.param(4) == 20").Evaluate(context));
        }
        
        [Test]
        public void TestTarget()
        {
            Assert.True(new SmartValidator($"target.type == 1").Evaluate(context));
        }
        
        [Test]
        public void TestActionWithoutAction()
        {
            var context = new TestContext();
            context.HasAction = false;
            Assert.Throws<ValidationParseException>(() => new SmartValidator($"action.param(4) == 20").Evaluate(context));
        }
        
        [ExcludeFromCodeCoverage]
        private class TestContext : ISmartValidationContext
        {
            public SmartScriptType ScriptType => SmartScriptType.Gossip;
            public bool HasAction { get; set; } = true;
            public int EventParametersCount => 4;
            public int ActionParametersCount => 6;
            public int ActionSourceParametersCount => 3;
            public int ActionTargetParametersCount => 3;
            
            public long GetEventParameter(int index)
            {
                if (index < 0 || index >= EventParametersCount)
                    throw new Exception();

                return (1 + index) * 2;
            }

            public long GetActionParameter(int index)
            {
                if (index < 0 || index >= ActionParametersCount)
                    throw new Exception();

                return (1 + index) * 3;
            }

            public long GetActionSourceParameter(int index)
            {
                if (index < 0 || index >= ActionSourceParametersCount)
                    throw new Exception();

                return (1 + index) * 4;
            }

            public long GetActionTargetParameter(int index)
            {
                if (index < 0 || index >= ActionTargetParametersCount)
                    throw new Exception();

                return (1 + index) * 5;
            }

            public long GetTargetType()
            {
                return 1;
            }
        }
    }
}