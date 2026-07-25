using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters
{
    /// <summary>
    /// A parameter whose actual meaning depends on a sibling parameter's value within the
    /// same action/event/target (e.g. SMART_ACTION_FOLLOW's "Credit" is a creature entry
    /// when "Credit Type" = 0, but a quest id when "Credit Type" = 1). The discriminator is
    /// read from the owning SmartBaseElement's int parameter list by index (0-based).
    /// </summary>
    public class SwitchedByParameter : BaseContextualParameter<long, SmartBaseElement>, ICustomPickerContextualParameter<long>
    {
        private readonly int discriminatorParamIndex;
        private readonly IReadOnlyDictionary<long, IParameter<long>> byDiscriminatorValue;
        private readonly IParameter<long> fallback;

        public SwitchedByParameter(int discriminatorParamIndex,
            IReadOnlyDictionary<long, IParameter<long>> byDiscriminatorValue,
            IParameter<long> fallback)
        {
            this.discriminatorParamIndex = discriminatorParamIndex;
            this.byDiscriminatorValue = byDiscriminatorValue;
            this.fallback = fallback;
        }

        private IParameter<long> Resolve(SmartBaseElement? element)
        {
            if (element != null && discriminatorParamIndex < element.ParametersCount)
            {
                var key = element.GetParameter(discriminatorParamIndex).Value;
                if (byDiscriminatorValue.TryGetValue(key, out var p))
                    return p;
            }
            return fallback;
        }

        public override string? Prefix => null;
        public override bool HasItems => true;
        public override Dictionary<long, SelectOption>? Items => null;
        public override string ToString(long value) => fallback.ToString(value);
        public override string ToString(long value, SmartBaseElement context) => Resolve(context).ToString(value);

        public async Task<(long, bool)> PickValue(long value, object context)
        {
            var resolved = Resolve(context as SmartBaseElement);
            if (resolved is ICustomPickerParameter<long> picker)
                return await picker.PickValue(value);
            if (resolved is ICustomPickerContextualParameter<long> contextualPicker)
                return await contextualPicker.PickValue(value, context!);
            return (value, false);
        }
    }
}
