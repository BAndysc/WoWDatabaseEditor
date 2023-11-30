using System;
using System.Collections;
using System.Collections.Generic;
using WDE.Common.Collections;
using WDE.Common.DBC;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.Spells
{
    [AutoRegister]
    [SingleInstance]
    public class SpellStore : ISpellStore
    {
        private MergedSpellService merged;

        public SpellStore(IDbcSpellService dbcSpellService, IDatabaseSpellService databaseSpellService)
        {
            merged = new MergedSpellService(dbcSpellService, databaseSpellService);
        }

        public IReadOnlyList<ISpellEntry> Spells => merged;
        
        public bool HasSpell(uint entry) => merged.ContainsSpell(entry);

        public string? GetName(uint entry)
        {
            if (!merged.ContainsSpell(entry))
                return null;

            var spell = merged.GetBySpellId(entry);
            return spell.Name;
        }
    }
    
    internal class SpellEntry : ISpellEntry
    {
        public uint Id { get; }
        public string Name { get; set; }
        public string Aura { get; set; } = "";
        public string Targets { get; set; } = "";

        public SpellEntry(uint id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    internal class MergedSpellService : IIndexedCollection<ISpellEntry>, IReadOnlyList<ISpellEntry>
    {
        private readonly ISpellService[] services;
        private List<SpellEntry> cachedSpells = new();
        private Dictionary<uint, int> spellIdToIndex = new();

        public MergedSpellService(params ISpellService[] services)
        {
            this.services = services;
            foreach (var service in services)
            {
                service.Changed += _ =>
                {
                    Cache();
                    OnCountChanged?.Invoke();
                };
            }
            Cache();
        }

        private void Cache()
        {
            cachedSpells.Clear();
            spellIdToIndex.Clear();
            HashSet<SpellTarget> distinctTargets = new();
            foreach (var service in services)
            {
                for (int i = 0, count = service.SpellCount; i < count; ++i)
                {
                    var spellId = service.GetSpellId(i);
                    var entry = new SpellEntry(spellId, service.GetName(spellId));
                    var effectsCount = service.GetSpellEffectsCount(spellId);
                    distinctTargets.Clear();
                    
                    for (int j = 0; j < effectsCount; ++j)
                    {
                        var effectType = service.GetSpellEffectType(spellId, j);
                        var aura = service.GetSpellAuraType(spellId, j);
                        var targets = service.GetSpellEffectTargetType(spellId, j);
                        if (targets.a != SpellTarget.NoTarget)
                            distinctTargets.Add(targets.a);
    
                        if (targets.b != SpellTarget.NoTarget)
                            distinctTargets.Add(targets.b);
                        
                        if (aura != SpellAuraType.None)
                        {
                            entry.Aura += $"{aura} ";
                        }
                    }
                    entry.Targets = string.Join(", ", distinctTargets);
                    cachedSpells.Add(entry);
                }
            }
            cachedSpells.Sort((a, b) => a.Id.CompareTo(b.Id));
            for (int i = 0; i < cachedSpells.Count; ++i)
                spellIdToIndex[cachedSpells[i].Id] = i;
        }

        public bool ContainsSpell(uint spellId) => spellIdToIndex.ContainsKey(spellId);
        
        public ISpellEntry GetBySpellId(uint spellId)
        {
            if (!spellIdToIndex.TryGetValue(spellId, out var index))
                throw new Exception("Spell not found");
            return cachedSpells[index];
        }
        
        public ISpellEntry this[int index] => cachedSpells[index];

        public int Count => cachedSpells.Count;
        
        public event Action? OnCountChanged;
        
        public IEnumerator<ISpellEntry> GetEnumerator()
        {
            return cachedSpells.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}