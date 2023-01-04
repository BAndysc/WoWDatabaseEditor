using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Parameters;

namespace WDE.Spells.Parameters
{
    internal class DatabaseSpellParameter : ParameterNumbered
    {
        public DatabaseSpellParameter(IReadOnlyList<IDatabaseSpellDbc>? items)
        {
            if (items == null)
                return;
            Items = new();
            foreach (var i in items)
                Items.Add(i.Id, new SelectOption(i.Name ?? "Database spell " + i.Id));
        }
    }
}