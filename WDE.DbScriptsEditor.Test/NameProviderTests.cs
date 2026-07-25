using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Parameters;
using WDE.DbScriptsEditor;
using WDE.DbScriptsEditor.Models;
using WDE.DbScriptsEditor.Providers;

namespace WDE.DbScriptsEditor.Test
{
    public class NameProviderTests
    {
        private class NumberedParameter : ParameterNumbered
        {
            public NumberedParameter(params (long, string)[] items)
            {
                Items = new Dictionary<long, SelectOption>();
                foreach (var (key, name) in items)
                    Items[key] = new SelectOption(name);
            }
        }

        private class AsyncOnlyParameter : Parameter, IAsyncParameter<long>
        {
            public override string ToString(long key) => key + " (fetching)";

            public Task<string> ToStringAsync(long val, CancellationToken token) =>
                Task.FromResult($"{val} (Ancient Chest - 1234)");
        }

        private IParameterFactory FactoryWith(string key, IParameter<long> parameter)
        {
            var factory = Substitute.For<IParameterFactory>();
            factory.Factory(Arg.Any<string>()).Returns(Parameter.Instance);
            factory.Factory(key).Returns(parameter);
            return factory;
        }

        [Test]
        public void QuestScript_IncludesQuestName()
        {
            var factory = FactoryWith("QuestParameter", new NumberedParameter((5, "The Cursed Crystal")));
            var provider = new DbScriptNameProvider(factory);

            var name = provider.GetName(new DbScriptSolutionItem(DbScriptType.QuestStart, 5));
            Assert.AreEqual("On Quest Start: The Cursed Crystal (5)", name);
        }

        [Test]
        public void CreatureScript_IncludesCreatureName()
        {
            var factory = FactoryWith("CreatureParameter", new NumberedParameter((299, "Young Wolf")));
            var provider = new DbScriptNameProvider(factory);

            var name = provider.GetName(new DbScriptSolutionItem(DbScriptType.CreatureDeath, 299));
            Assert.AreEqual("On Creature Death: Young Wolf (299)", name);
        }

        [Test]
        public void UnknownId_FallsBackToNumericName()
        {
            var factory = FactoryWith("QuestParameter", new NumberedParameter());
            var provider = new DbScriptNameProvider(factory);

            var name = provider.GetName(new DbScriptSolutionItem(DbScriptType.QuestEnd, 123));
            Assert.AreEqual("On Quest End 123", name);
        }

        [Test]
        public void RelayScript_PlainParameter_StaysNumeric()
        {
            var factory = Substitute.For<IParameterFactory>();
            factory.Factory(Arg.Any<string>()).Returns(Parameter.Instance);
            var provider = new DbScriptNameProvider(factory);

            var name = provider.GetName(new DbScriptSolutionItem(DbScriptType.Relay, 777));
            Assert.AreEqual("On Relay 777", name);
        }

        [Test]
        public void GoGuidScript_SyncName_DoesNotShowFetchingPlaceholder()
        {
            var factory = FactoryWith("GameobjectGUIDParameter", new AsyncOnlyParameter());
            var provider = new DbScriptNameProvider(factory);

            var name = provider.GetName(new DbScriptSolutionItem(DbScriptType.GoUse, 5005));
            Assert.AreEqual("On GO Use (guid) 5005", name);
        }

        [Test]
        public async Task GoGuidScript_AsyncName_UsesAsyncParameter()
        {
            var factory = FactoryWith("GameobjectGUIDParameter", new AsyncOnlyParameter());
            var provider = new DbScriptNameProvider(factory);

            var name = await provider.GetNameAsync(new DbScriptSolutionItem(DbScriptType.GoUse, 5005));
            Assert.AreEqual("On GO Use (guid): 5005 (Ancient Chest - 1234)", name);
        }

        [Test]
        public async Task AsyncName_ForSyncParameter_MatchesSyncName()
        {
            var factory = FactoryWith("QuestParameter", new NumberedParameter((5, "The Cursed Crystal")));
            var provider = new DbScriptNameProvider(factory);

            var name = await provider.GetNameAsync(new DbScriptSolutionItem(DbScriptType.QuestStart, 5));
            Assert.AreEqual("On Quest Start: The Cursed Crystal (5)", name);
        }
    }
}
