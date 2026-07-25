using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using WDE.Common.Database;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Test
{
    public class ConditionsStoreTests
    {
        private static AbstractMangosConditionLine Line(uint entry, int type, uint v1 = 0, uint v2 = 0) =>
            new() { ConditionEntry = entry, ConditionType = type, Value1 = v1, Value2 = v2 };

        [Test]
        public async Task LoadClosures_FollowsLogicalReferences()
        {
            var db = new Dictionary<uint, AbstractMangosConditionLine>
            {
                [6] = Line(6, -1, 4, 5), // AND(4, 5)
                [5] = Line(5, -3, 3),    // NOT(3)
                [4] = Line(4, 8, 100),
                [3] = Line(3, 8, 200),
            };
            var store = new DbScriptConditionsStore();
            await store.LoadClosuresAsync(new uint[] { 6 },
                entries => Task.FromResult<IReadOnlyList<IMangosConditionLine>>(
                    entries.Where(db.ContainsKey).Select(e => (IMangosConditionLine)db[e]).ToList()));

            var closure = store.Closure(6);
            CollectionAssert.AreEquivalent(new uint[] { 3, 4, 5, 6 }, closure.Select(l => l.ConditionEntry));
            Assert.AreEqual(6u, store.MaxEntry);
            Assert.IsFalse(store.HasChanges);
        }

        [Test]
        public void ApplyEdit_TracksAffectedAndReplacesRows()
        {
            var store = new DbScriptConditionsStore();
            store.AddLoaded(new IMangosConditionLine[] { Line(3, 8, 100), Line(5, -3, 3) });

            // edit replaced entry 3 with new content and added a new node 10
            store.ApplyEdit(new uint[] { 3, 5 }, new IMangosConditionLine[]
            {
                Line(3, 8, 999),
                Line(10, -3, 3),
            });

            Assert.IsTrue(store.HasChanges);
            // 5 was dropped by the edit: deleted, not reinserted
            CollectionAssert.AreEquivalent(new uint[] { 3, 5, 10 }, store.AffectedEntries);
            CollectionAssert.AreEquivalent(new uint[] { 3, 10 }, store.AffectedLines.Select(l => l.ConditionEntry));
            Assert.AreEqual(999u, store.Closure(3).Single().Value1);
        }

        [Test]
        public void Closure_UnknownOrZero_IsEmpty()
        {
            var store = new DbScriptConditionsStore();
            Assert.IsEmpty(store.Closure(0));
            Assert.IsEmpty(store.Closure(42));
        }
    }
}
