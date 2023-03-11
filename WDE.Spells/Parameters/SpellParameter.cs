using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Parameters;

namespace WDE.Spells.Parameters
{
    internal class SpellParameter : ParameterNumbered, ICustomPickerParameter<long>
    {
        private readonly ISpellEntryProviderService picker;

        public SpellParameter(ISpellEntryProviderService picker, IParameter<long> dbc, IParameter<long> db)
        {
            this.picker = picker;
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
            var picked = await picker.GetEntryFromService();
            return (picked ?? 0, picked.HasValue);
        }
    }
}