using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.Spells;

[AutoRegister]
[SingleInstance]
public class DatabaseSpellService : IDatabaseSpellService
{
    private readonly IDatabaseProvider databaseProvider;

    private Dictionary<uint, int> spellIdToIndex = new();
    private List<Spell> spells = new();

    private class Spell
    {
        public uint SpellId;
        public string Name = "";
        public IDatabaseSpellEffectDbc[]? Effects;
    }

    public DatabaseSpellService(IDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
        LoadDataAsync().ListenErrors();
    }

    private async Task LoadDataAsync()
    {
        var effects = await databaseProvider.GetSpellEffectDbcAsync();
        var dbSpells = await databaseProvider.GetSpellDbcAsync();
        spellIdToIndex.Clear();
        spells.Clear();

        var effectsBySpellId = effects.GroupBy(e => e.SpellId)
            .ToDictionary(e => e.Key, e =>
            {
                var array = new IDatabaseSpellEffectDbc[e.Max(x => x.EffectIndex) + 1];
                foreach (var effect in e)
                    array[effect.EffectIndex] = effect;
                return array;
            });
        
        foreach (var spell in dbSpells)
        {
            spellIdToIndex[spell.Id] = spells.Count;
            var spellEntry = new Spell()
            {
                SpellId = spell.Id,
                Name = spell.Name ?? ""
            };
            if (effectsBySpellId.TryGetValue(spell.Id, out var effectsArray))
                spellEntry.Effects = effectsArray;
            spells.Add(spellEntry);
        }
        
        Changed?.Invoke(this);
    }

    public bool Exists(uint spellId) => spellIdToIndex.ContainsKey(spellId);

    public int SpellCount => spells.Count;

    public uint GetSpellId(int index) => spells[index].SpellId;

    public string GetName(uint spellId) => spells[spellIdToIndex[spellId]].Name;

    public int GetSpellEffectsCount(uint spellId) => spells[spellIdToIndex[spellId]].Effects?.Length ?? 0;

    public SpellAuraType GetSpellAuraType(uint spellId, int effectIndex) => (SpellAuraType)spells[spellIdToIndex[spellId]].Effects![effectIndex].Aura;
        
    public SpellEffectType GetSpellEffectType(uint spellId, int effectIndex) => (SpellEffectType)spells[spellIdToIndex[spellId]].Effects![effectIndex].Aura;

    public SpellTargetFlags GetSpellTargetFlags(uint spellId) => default;

    public (SpellTarget a, SpellTarget b) GetSpellEffectTargetType(uint spellId, int effectIndex)
    {
        var effect = spells[spellIdToIndex[spellId]].Effects![effectIndex];
        return ((SpellTarget)effect.ImplicitTarget1, (SpellTarget)effect.ImplicitTarget2);
    }

    public event Action<ISpellService>? Changed;
}