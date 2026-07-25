using WDE.Parameters.Models;

namespace WDE.DbScriptsEditor.Models
{
    // A single editable named parameter shown in the step editor: wraps whichever holder
    // (long or float) the command mapped to a physical column, so the view can pick/display it.
    public class DbScriptEditableParameter
    {
        // Well-known parameter type keys the editor navigates to instead of opening a value picker.
        public const string RelayType = "DbScriptRelayParameter";
        public const string StringRandomTemplateType = "DbScriptStringRandomTemplateParameter";
        public const string RelayRandomTemplateType = "DbScriptRelayRandomTemplateParameter";

        public string Name { get; }
        public DbScriptDestination? Destination { get; }
        public bool IsFloat { get; }
        public ParameterValueHolder<long>? LongHolder { get; }
        public ParameterValueHolder<float>? FloatHolder { get; }

        // The commands.json parameter type key (e.g. "DbScriptRelayParameter"); null for the
        // structural/timing scalars. Drives link navigation from the readable line.
        public string? TypeKey { get; }

        // The commands.json defaultVal: the value considered "unset" for this parameter.
        public long DefaultVal { get; }

        public DbScriptEditableParameter(string name, DbScriptDestination? destination, bool isFloat,
            ParameterValueHolder<long>? longHolder, ParameterValueHolder<float>? floatHolder, string? typeKey = null,
            long defaultVal = 0)
        {
            Name = name;
            Destination = destination;
            IsFloat = isFloat;
            LongHolder = longHolder;
            FloatHolder = floatHolder;
            TypeKey = typeKey;
            DefaultVal = defaultVal;
        }

        public IParameterValueHolder Holder => (IParameterValueHolder?)LongHolder ?? FloatHolder!;
        public string StringValue => Holder.String;

        public bool IsRelayLink => TypeKey == RelayType;
        public bool IsStringRandomTemplateLink => TypeKey == StringRandomTemplateType;
        public bool IsRelayRandomTemplateLink => TypeKey == RelayRandomTemplateType;
        public bool IsRandomTemplateLink => IsStringRandomTemplateLink || IsRelayRandomTemplateLink;
        public bool IsNavigable => IsRelayLink || IsRandomTemplateLink;
    }
}
