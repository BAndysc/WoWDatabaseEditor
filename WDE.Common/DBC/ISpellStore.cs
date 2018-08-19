using System.Collections.Generic;

namespace WDE.Common.DBC
{
    public interface ISpellStore
    {
        bool HasSpell(uint entry);
        string GetName(uint entry);
        IEnumerable<uint> Spells { get; }
    }
}
