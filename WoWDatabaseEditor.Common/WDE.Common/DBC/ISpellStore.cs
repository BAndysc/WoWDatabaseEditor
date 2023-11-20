using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Common.DBC
{
    [UniqueProvider]
    public interface ISpellStore
    {
        IReadOnlyList<ISpellEntry> Spells { get; }
        bool HasSpell(uint entry);
        string? GetName(uint entry);
    }

    public interface ISpellEntry
    {
        uint Id { get; }
        string Name { get; }
        string Aura { get; }
        string Targets { get; }
    }

    public class AbstractSpellEntry : ISpellEntry
    {
        public uint Id { get; set; }
        public string Name { get; set; } = "";
        public string Aura { get; set; } = "";
        public string Targets { get; set; } = "";
    }
}