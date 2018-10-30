using System.Collections.Generic;
using WDE.Common.Attributes;

namespace WDE.Common.DBC
{
    [UniqueProvider]
    public interface ISpellStore
    {
        bool HasSpell(uint entry);
        string GetName(uint entry);
        IEnumerable<uint> Spells { get; }
    }
}
