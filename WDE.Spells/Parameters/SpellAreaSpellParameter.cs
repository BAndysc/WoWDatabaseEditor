using System.Collections.Generic;
using WDE.Common.Parameters;

namespace WDE.Spells.Parameters
{
    internal class SpellAreaSpellParameter : Parameter
    {
        public SpellAreaSpellParameter(IParameter<long> storage)
        {
            Items = new Dictionary<long, SelectOption>();
            if (storage.Items != null)
            {
                foreach (var item in storage.Items)
                {
                    Items.Add(-item.Key, new SelectOption(item.Value.Name, "If the player HAS aura, then the spell will not be activated"));
                    Items.Add(item.Key, new SelectOption(item.Value.Name, "If the player has NO aura, then the spell will not be activated"));
                }   
            }
        }
    }
    
    internal class SpellOrRankedSpellParameter : Parameter
    {
        public SpellOrRankedSpellParameter(IParameter<long> storage)
        {
            Items = new Dictionary<long, SelectOption>();
            if (storage.Items != null)
            {
                foreach (var item in storage.Items)
                {
                    Items.Add(-item.Key, new SelectOption(item.Value.Name, "Ranked spell"));
                    Items.Add(item.Key, new SelectOption(item.Value.Name));
                }   
            }
        }
    }
}