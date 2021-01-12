using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Common.DBC
{
    [UniqueProvider]
    public interface ISpellStore
    {
        IEnumerable<uint> Spells { get; }
        bool HasSpell(uint entry);
        string GetName(uint entry);
    }
}