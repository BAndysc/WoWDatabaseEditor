using System;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor
{
    public interface IEditorFeatures
    {
        string Name { get; }
        bool SupportsSource { get; }
        bool SupportsEventCooldown { get; }
        bool SupportsTargetCondition { get; }
        bool SupportsEventTimerId { get; }
        bool SourceHasPosition { get; }
        bool UseExternalConditionsEditor => false;
        bool CanReorderConditions => true;
        bool HasCreatureEntry => false;
        ParametersCount ConditionParametersCount { get; }
        ParametersCount EventParametersCount { get; }
        ParametersCount ActionParametersCount { get; }
        ParametersCount TargetParametersCount { get; }
        IParameter<long> ConditionTargetParameter { get; }
        IParameter<long> EventFlagsParameter { get; }
        int TargetConditionId { get; }
        int? NonBreakableLinkFlag { get; }
    }

    public readonly struct ParametersCount
    {
        public readonly int IntCount;
        public readonly int FloatCount;
        public readonly int StringCount;

        public ParametersCount(int intCount, int floatCount, int stringCount)
        {
            IntCount = intCount;
            FloatCount = floatCount;
            StringCount = stringCount;
        }
    }
}