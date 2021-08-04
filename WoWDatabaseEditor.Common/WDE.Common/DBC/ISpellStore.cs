using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Common.DBC
{
    [UniqueProvider]
    public interface ISpellStore
    {
        IEnumerable<uint> Spells { get; }
        IEnumerable<(uint key, string name)> SpellsWithName { get; }
        bool HasSpell(uint entry);
        string? GetName(uint entry);
    }
}