using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using WDE.CMangosConditions.Data;
using WDE.CMangosConditions.Models;
using WDE.CMangosConditions.ViewModels;
using WDE.Common.Database;
using WDE.Common.Parameters;

namespace WDE.CMangosConditions.Test
{
    public class TreeCodecTests
    {
        private IMangosConditionsFactory factory = null!;

        [SetUp]
        public async Task Setup()
        {
            var parameterFactory = Substitute.For<IParameterFactory>();
            parameterFactory.Factory(Arg.Any<string>()).Returns(Parameter.Instance);
            var dataManager = new MangosConditionDataManager(new MangosConditionDataProvider(new TestRuntimeDataService()));
            await dataManager.Initialize();
            factory = new MangosConditionsFactory(dataManager, parameterFactory);
        }

        private static AbstractMangosConditionLine Line(uint entry, int type, uint v1 = 0, uint v2 = 0, uint v3 = 0, uint v4 = 0, uint flags = 0) =>
            new() { ConditionEntry = entry, ConditionType = type, Value1 = v1, Value2 = v2, Value3 = v3, Value4 = v4, Flags = flags };

        [Test]
        public void LeafOnly_Roundtrips()
        {
            // quest 123 rewarded, negated
            var roots = MangosConditionTreeCodec.BuildTree(new[] { Line(10, 8, 123, flags: 1) }, factory);
            Assert.AreEqual(1, roots.Count);
            Assert.AreEqual(8, roots[0].ConditionType);
            Assert.AreEqual(123, roots[0].Value1.Value);
            Assert.AreEqual(1, roots[0].Negate.Value);

            var result = MangosConditionTreeCodec.Serialize(roots, 11);
            Assert.AreEqual(1, result.Lines.Count);
            var line = result.Lines[0];
            Assert.AreEqual(10u, line.ConditionEntry);
            Assert.AreEqual(8, line.ConditionType);
            Assert.AreEqual(123u, line.Value1);
            Assert.AreEqual(1u, line.Flags);
            Assert.AreEqual(new List<uint> { 10 }, result.RootEntries);
        }

        [Test]
        public void NestedLogical_Roundtrips()
        {
            // AND(OR(quest 1, quest 2), NOT(aura 3))
            var lines = new[]
            {
                Line(1, 8, 100),
                Line(2, 8, 200),
                Line(3, 1, 300, 0),
                Line(4, -2, 1, 2),
                Line(5, -3, 3),
                Line(6, -1, 4, 5),
            };
            var roots = MangosConditionTreeCodec.BuildTree(lines, factory);
            Assert.AreEqual(1, roots.Count);
            var and = roots[0];
            Assert.AreEqual(-1, and.ConditionType);
            Assert.AreEqual(2, and.Children.Count);
            var or = and.Children[0];
            var not = and.Children[1];
            Assert.AreEqual(-2, or.ConditionType);
            Assert.AreEqual(2, or.Children.Count);
            Assert.AreEqual(-3, not.ConditionType);
            Assert.AreEqual(1, not.Children.Count);
            Assert.AreEqual(1, not.Children[0].ConditionType);

            var result = MangosConditionTreeCodec.Serialize(roots, 7);
            Assert.AreEqual(6, result.Lines.Count);
            // unchanged tree keeps all entries
            CollectionAssert.AreEquivalent(new uint[] { 1, 2, 3, 4, 5, 6 }, result.Lines.Select(l => l.ConditionEntry));
            var rootLine = result.Lines.Single(l => l.ConditionEntry == 6);
            Assert.AreEqual(4u, rootLine.Value1);
            Assert.AreEqual(5u, rootLine.Value2);
            // children serialized before parents
            foreach (var l in result.Lines)
                foreach (var r in MangosConditionTreeCodec.ChildRefs(l))
                    Assert.Less(r, l.ConditionEntry);
        }

        [Test]
        public void NewNodes_GetIdsAboveFirstFree_ChildrenBelowParents()
        {
            var and = factory.Create(-1);
            var leaf1 = factory.Create(8);
            leaf1.Value1.Value = 100;
            var leaf2 = factory.Create(8);
            leaf2.Value1.Value = 200;
            and.Children.Add(leaf1);
            and.Children.Add(leaf2);

            var result = MangosConditionTreeCodec.Serialize(new[] { and }, 50);
            Assert.AreEqual(3, result.Lines.Count);
            Assert.IsTrue(result.Lines.All(l => l.ConditionEntry >= 50));
            var parent = result.Lines.Single(l => l.ConditionType == -1);
            Assert.Greater(parent.ConditionEntry, parent.Value1);
            Assert.Greater(parent.ConditionEntry, parent.Value2);
        }

        [Test]
        public void OrderingViolation_ReassignsParent()
        {
            // an old NOT (entry 5) gets an old child with a HIGHER entry (20): parent must be reassigned
            var not = factory.Create(Line(5, -3));
            var leaf = factory.Create(Line(20, 8, 100));
            not.Children.Add(leaf);

            var result = MangosConditionTreeCodec.Serialize(new[] { not }, 21);
            var parent = result.Lines.Single(l => l.ConditionType == -3);
            Assert.AreEqual(20u, result.Lines.Single(l => l.ConditionType == 8).ConditionEntry);
            Assert.GreaterOrEqual(parent.ConditionEntry, 21);
            Assert.AreEqual(20u, parent.Value1);
            Assert.AreEqual(parent.ConditionEntry, result.RootEntries[0]);
        }

        [Test]
        public void IdenticalContent_DedupesToOneEntry()
        {
            var a = factory.Create(8);
            a.Value1.Value = 100;
            var b = factory.Create(8);
            b.Value1.Value = 100;
            var and = factory.Create(-1);
            and.Children.Add(a);
            and.Children.Add(b);

            var result = MangosConditionTreeCodec.Serialize(new[] { and }, 1);
            // both identical leaves collapse into one row (unique key on type+values+flags)
            Assert.AreEqual(2, result.Lines.Count);
            var parent = result.Lines.Single(l => l.ConditionType == -1);
            Assert.AreEqual(parent.Value1, parent.Value2);
        }

        [Test]
        public void Validation_ChecksChildCounts()
        {
            var not = factory.Create(-3); // 0 children, needs exactly 1
            var and = factory.Create(-1); // 0 children, needs 2-4
            var leaf = factory.Create(8);

            Assert.AreEqual(1, MangosConditionTreeCodec.Validate(new[] { not }).Count);
            Assert.AreEqual(1, MangosConditionTreeCodec.Validate(new[] { and }).Count);
            Assert.AreEqual(0, MangosConditionTreeCodec.Validate(new[] { leaf }).Count);

            var okNot = factory.Create(-3);
            okNot.Children.Add(factory.Create(8));
            Assert.AreEqual(0, MangosConditionTreeCodec.Validate(new[] { okNot }).Count);
        }

        [Test]
        public void MissingReference_BecomesPlaceholder()
        {
            var roots = MangosConditionTreeCodec.BuildTree(new[] { Line(5, -3, 3) }, factory);
            Assert.AreEqual(1, roots.Count);
            Assert.AreEqual(1, roots[0].Children.Count);
            Assert.AreEqual(3u, roots[0].Children[0].OriginalEntry);
        }

        [Test]
        public void MultipleRoots_AllReturned()
        {
            var roots = MangosConditionTreeCodec.BuildTree(new[] { Line(1, 8, 100), Line(2, 8, 200) }, factory);
            Assert.AreEqual(2, roots.Count);
            var result = MangosConditionTreeCodec.Serialize(roots, 3);
            Assert.AreEqual(new List<uint> { 1, 2 }, result.RootEntries);
        }

        [Test]
        public void LogicalTypes_ComeFromJson()
        {
            var and = factory.Create(-1);
            Assert.IsTrue(and.IsLogical);
            Assert.AreEqual(2, and.MinChildren);
            Assert.AreEqual(4, and.MaxChildren);
            var not = factory.Create(-3);
            Assert.AreEqual(1, not.MinChildren);
            Assert.AreEqual(1, not.MaxChildren);
            var leaf = factory.Create(8);
            Assert.IsFalse(leaf.IsLogical);
        }
    }
}
