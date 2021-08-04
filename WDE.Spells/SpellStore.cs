using System.Collections.Generic;
using System.Linq;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.Module.Attributes;

namespace WDE.Spells
{
    [AutoRegister]
    [SingleInstance]
    public class SpellStore : ISpellStore
    {
        private readonly IParameterFactory parameterFactory;

        public SpellStore(IParameterFactory parameterFactory)
        {
            this.parameterFactory = parameterFactory;
        }

        public IEnumerable<uint> Spells => parameterFactory.Factory("SpellParameter").Items?.Select(i => (uint)i.Key) ??
                                           Enumerable.Empty<uint>();

        public IEnumerable<(uint, string)> SpellsWithName => parameterFactory.Factory("SpellParameter").Items?.Select(i => ((uint)i.Key, i.Value.Name)) ??
                                           Enumerable.Empty<(uint, string)>();
        
        public bool HasSpell(uint entry)
        {
            if (!parameterFactory.IsRegisteredLong("SpellParameter"))
                return false;
            var param = parameterFactory.Factory("SpellParameter");
            return param.Items != null && param.Items.ContainsKey(entry);
        }

        public string? GetName(uint entry)
        {
            if (!HasSpell(entry))
                return null;
            
            var param = parameterFactory.Factory("SpellParameter");
            return param.Items![entry].Name;
        }
    }
}