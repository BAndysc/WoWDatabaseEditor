using System.Linq;
using WDE.Common.Parameters;

namespace WDE.Spells.Parameters
{
    internal class MultiSpellParameter : MultiSwitchStringParameter
    {
        public MultiSpellParameter(IParameter<long> spells)
            : base(spells.Items?.ToDictionary(t => t.Key.ToString(), t => t.Value) ?? new())
        {
        }
    }
}