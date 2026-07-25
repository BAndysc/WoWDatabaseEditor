using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using WDE.CMangosConditions.Data;

namespace WDE.CMangosConditions.Test
{
    public class UnitConditionDataTests
    {
        private static Task<IReadOnlyList<UnitConditionVariableJson>> LoadVariables() =>
            new UnitConditionDataProvider(new TestRuntimeDataService()).GetVariables();

        [Test]
        public async Task Data_Contains_Exactly_Ids_0_To_86()
        {
            var variables = await LoadVariables();
            var ids = variables.Select(v => v.Id).ToList();
            CollectionAssert.AllItemsAreUnique(ids);
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 87), ids);
        }

        [Test]
        public async Task Data_Every_Entry_Has_Names()
        {
            foreach (var variable in await LoadVariables())
            {
                Assert.IsNotEmpty(variable.Name, $"variable {variable.Id} has no name");
                Assert.IsNotEmpty(variable.NameReadable, $"variable {variable.Id} has no readable name");
            }
        }

        [Test]
        public async Task Data_Every_Implemented_Entry_Except_None_Has_Value()
        {
            foreach (var variable in await LoadVariables())
            {
                if (variable.Nyi || variable.Id == 0 || variable.Id == 85)
                    continue;
                Assert.IsNotNull(variable.Value, $"variable {variable.Id} ({variable.Name}) has no value parameter");
            }
        }
    }
}
