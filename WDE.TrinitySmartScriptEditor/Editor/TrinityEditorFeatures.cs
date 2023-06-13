using System.Collections.Generic;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Models;

namespace WDE.TrinitySmartScriptEditor.Editor
{
    [AutoRegister]
    [SingleInstance]
    public class TrinityEditorFeatures : IEditorFeatures
    {
        public string Name => "TC";
        public bool SupportsSource => false;
        public bool SupportsEventCooldown => false;
        public bool SupportsTargetCondition => false;
        public bool SupportsEventTimerId => false;
        public bool SourceHasPosition => false;
        public ParametersCount ConditionParametersCount { get; } = new ParametersCount(3, 0, 0);
        public ParametersCount EventParametersCount { get; } = new ParametersCount(4, 0, 0);
        public ParametersCount ActionParametersCount { get; } = new ParametersCount(6, 0, 0);
        public ParametersCount TargetParametersCount { get; } = new ParametersCount(3, 4, 0);
        public IParameter<long> ConditionTargetParameter { get; }
        public IParameter<long> EventFlagsParameter => SmartEventFlagParameter.Instance;
        public ISmartScriptSolutionItem CreateSolutionItem(SmartScriptType type, int entry) => new SmartScriptSolutionItem(entry, type);

        public TrinityEditorFeatures(ICurrentCoreVersion coreVersion)
        {
            var conditionTargetParam = new Parameter();
            conditionTargetParam.Items = new Dictionary<long, SelectOption>() {[0] = new("Action invoker"), [1] = new("Object")};
            if (coreVersion.Current.SmartScriptFeatures.SupportsConditionTargetVictim)
                conditionTargetParam.Items.Add(2, new SelectOption("Victim"));
            ConditionTargetParameter = conditionTargetParam;
        }
    }
}