using System;
using NSubstitute;
using NUnit.Framework;
using WDE.Common;
using WDE.Common.Parameters;
using WDE.Common.QuickAccess;
using WDE.Common.Services;
using WDE.Parameters.QuickAccess;

namespace WDE.Parameters.Test
{
    public class FactoryTests
    {
        private IParameterFactory parameterFactory;
        
        [SetUp]
        public void SetUp()
        {
            parameterFactory = new ParameterFactory(Substitute.For<IQuickAccessRegisteredParameters>(),
                new Lazy<ITableEditorPickerService>());
        }

        private IParameter<long> Register(string name)
        {
            var param = Substitute.For<IParameter<long>>();
            parameterFactory.Register(name, param);
            return param;
        }
        
        [Test]
        public void TestRegisterCompoundCAB()
        {
            parameterFactory.RegisterCombined("c", "a", "b", (a, b) =>
            {
                Assert.True(parameterFactory.IsRegisteredLong("a"));
                Assert.True(parameterFactory.IsRegisteredLong("b"));
                return Substitute.For<IParameter<long>>();
            });
            
            Assert.False(parameterFactory.IsRegisteredLong("c"));
            Register("a");
            Assert.False(parameterFactory.IsRegisteredLong("c"));
            Register("b");
            
            Assert.True(parameterFactory.IsRegisteredLong("c"));
        }
        
        [Test]
        public void TestRegisterCompoundCBA()
        {
            parameterFactory.RegisterCombined("c", "a", "b", (a, b) =>
            {
                Assert.True(parameterFactory.IsRegisteredLong("a"));
                Assert.True(parameterFactory.IsRegisteredLong("b"));
                return Substitute.For<IParameter<long>>();
            });
            
            Assert.False(parameterFactory.IsRegisteredLong("c"));
            Register("b");
            Assert.False(parameterFactory.IsRegisteredLong("c"));
            Register("a");
            
            Assert.True(parameterFactory.IsRegisteredLong("c"));
        }
        
        [Test]
        public void TestRegisterCompoundABC()
        {
            Register("a");
            Register("b");
            
            parameterFactory.RegisterCombined("c", "a", "b", (a, b) =>
            {
                Assert.True(parameterFactory.IsRegisteredLong("a"));
                Assert.True(parameterFactory.IsRegisteredLong("b"));
                return Substitute.For<IParameter<long>>();
            });

            Assert.True(parameterFactory.IsRegisteredLong("c"));
        }
        
        [Test]
        public void TestRegisterCompoundACB()
        {
            Register("a");
            parameterFactory.RegisterCombined("c", "a", "b", (a, b) =>
            {
                Assert.True(parameterFactory.IsRegisteredLong("a"));
                Assert.True(parameterFactory.IsRegisteredLong("b"));
                return Substitute.For<IParameter<long>>();
            });
            
            Assert.False(parameterFactory.IsRegisteredLong("c"));
            Register("b");
            
            Assert.True(parameterFactory.IsRegisteredLong("c"));
        }
        
        [Test]
        public void TestRegisterCompoundBCA()
        {
            Register("b");
            parameterFactory.RegisterCombined("c", "a", "b", (a, b) =>
            {
                Assert.True(parameterFactory.IsRegisteredLong("a"));
                Assert.True(parameterFactory.IsRegisteredLong("b"));
                return Substitute.For<IParameter<long>>();
            });
            
            Assert.False(parameterFactory.IsRegisteredLong("c"));
            Register("a");
            
            Assert.True(parameterFactory.IsRegisteredLong("c"));
        }
    }
}