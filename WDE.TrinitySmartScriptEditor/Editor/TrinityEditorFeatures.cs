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
    [RequiresCore("TrinityMaster")]
    public class TrinityMasterEditorFeatures : IEditorFeatures
    {
        public string Name => "TC";
        public bool SupportsSource => false;
        public bool SupportsEventCooldown => false;
        public bool SupportsTargetCondition => false;
        public bool SupportsEventTimerId => false;
        public bool SourceHasPosition => false;
        public ParametersCount ConditionParametersCount { get; } = new ParametersCount(3, 0, 1);
        public ParametersCount EventParametersCount { get; } = new ParametersCount(4, 0, 0);
        public ParametersCount ActionParametersCount { get; } = new ParametersCount(7, 0, 0);
        public ParametersCount TargetParametersCount { get; } = new ParametersCount(3, 4, 0);
        public IParameter<long> ConditionTargetParameter { get; }
        public IParameter<long> EventFlagsParameter => SmartEventFlagParameter.Instance;
        public int TargetConditionId => -1;
        public int? NonBreakableLinkFlag => null;

        public TrinityMasterEditorFeatures(ICurrentCoreVersion coreVersion)
        {
            var conditionTargetParam = new Parameter();
            conditionTargetParam.Items = new Dictionary<long, SelectOption>() {[0] = new("Action invoker"), [1] = new("Object")};
            if (coreVersion.Current.SmartScriptFeatures.SupportsConditionTargetVictim)
                conditionTargetParam.Items.Add(2, new SelectOption("Victim"));
            ConditionTargetParameter = conditionTargetParam;
        }
    }

    [AutoRegister]
    [SingleInstance]
    [RequiresCore("TrinityWrath", "TrinityCata")]
    public class TrinityWrathEditorFeatures : IEditorFeatures
    {
        public string Name => "TC";
        public bool SupportsSource => false;
        public bool SupportsEventCooldown => false;
        public bool SupportsTargetCondition => false;
        public bool SupportsEventTimerId => false;
        public bool SourceHasPosition => false;
        public ParametersCount ConditionParametersCount { get; } = new ParametersCount(3, 0, 1);
        public ParametersCount EventParametersCount { get; } = new ParametersCount(4, 0, 0);
        public ParametersCount ActionParametersCount { get; } = new ParametersCount(6, 0, 0);
        public ParametersCount TargetParametersCount { get; } = new ParametersCount(3, 4, 0);
        public IParameter<long> ConditionTargetParameter { get; }
        public IParameter<long> EventFlagsParameter => SmartEventFlagParameter.Instance;
        public int TargetConditionId => -1;
        public int? NonBreakableLinkFlag => null;

        public TrinityWrathEditorFeatures(ICurrentCoreVersion coreVersion)
        {
            var conditionTargetParam = new Parameter();
            conditionTargetParam.Items = new Dictionary<long, SelectOption>() {[0] = new("Action invoker"), [1] = new("Object")};
            if (coreVersion.Current.SmartScriptFeatures.SupportsConditionTargetVictim)
                conditionTargetParam.Items.Add(2, new SelectOption("Victim"));
            ConditionTargetParameter = conditionTargetParam;
        }
    }

    [AutoRegister]
    [SingleInstance]
    [RequiresCore("Azeroth")]
    public class AzerothEditorFeatures : IEditorFeatures
    {
        public string Name => "Azeroth";
        public bool SupportsSource => false;
        public bool SupportsEventCooldown => false;
        public bool SupportsTargetCondition => false;
        public bool SupportsEventTimerId => false;
        public bool SourceHasPosition => false;
        public ParametersCount ConditionParametersCount { get; } = new ParametersCount(3, 0, 0);
        public ParametersCount EventParametersCount { get; } = new ParametersCount(6, 0, 0);
        public ParametersCount ActionParametersCount { get; } = new ParametersCount(6, 0, 0);
        public ParametersCount TargetParametersCount { get; } = new ParametersCount(3, 4, 0);
        public IParameter<long> ConditionTargetParameter { get; }
        public IParameter<long> EventFlagsParameter => SmartEventFlagParameter.Instance;
        public int TargetConditionId => -1;
        public int? NonBreakableLinkFlag => null;

        public AzerothEditorFeatures(ICurrentCoreVersion coreVersion)
        {
            var conditionTargetParam = new Parameter();
            conditionTargetParam.Items = new Dictionary<long, SelectOption>() {[0] = new("Action invoker"), [1] = new("Object")};
            if (coreVersion.Current.SmartScriptFeatures.SupportsConditionTargetVictim)
                conditionTargetParam.Items.Add(2, new SelectOption("Victim"));
            ConditionTargetParameter = conditionTargetParam;
        }
    }
}