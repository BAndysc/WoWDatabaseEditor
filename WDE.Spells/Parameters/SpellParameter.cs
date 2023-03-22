using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Parameters;

namespace WDE.Spells.Parameters
{
    internal class SpellParameter : ParameterNumbered, ICustomPickerParameter<long>, ISpellParameter
    {
        private readonly ISpellEntryProviderService picker;
        private readonly string? customCounterTable;

        public SpellParameter(ISpellEntryProviderService picker, IParameter<long> dbc, IParameter<long> db, string? customCounterTable = null) 
        {
            this.picker = picker;
            this.customCounterTable = customCounterTable;
            Items = new();
            if (dbc.Items != null)
            {
                foreach (var i in dbc.Items)
                    Items[i.Key] = i.Value;
            }
            
            if (db.Items != null)
            {
                foreach (var i in db.Items)
                    Items[i.Key] = i.Value;
            }
        }

        public async Task<(long, bool)> PickValue(long value)
        {
            var picked = await picker.GetEntryFromService((uint)value, customCounterTable);
            return (picked ?? 0, picked.HasValue);
        }
        
        public async Task<IReadOnlyCollection<long>> PickMultipleValues()
        {
            return (await picker.GetEntriesFromService()).Select(x => (long)x).ToList();
        }
    }
}