using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.CoreVersion
{
    [NonUniqueProvider]
    public interface ICoreVersion
    {
        string Tag { get; }
        string FriendlyName { get; }
        IDatabaseFeatures DatabaseFeatures { get; }
        ISmartScriptFeatures SmartScriptFeatures { get; }
        IConditionFeatures ConditionFeatures { get; }
        bool SupportsRbac => true;
    }

    public interface IConditionFeatures
    {
        string ConditionsFile { get; }
        string ConditionGroupsFile { get; }
        string ConditionSourcesFile { get; }
    }

    public interface IDatabaseFeatures
    {
        ISet<Type> UnsupportedTables { get; }
        bool AlternativeTrinityStrings => false;
        bool HasAiEntry => false;
    }

    public interface ISmartScriptFeatures
    {
        ISet<SmartScriptType> SupportedTypes { get; }
        bool SupportsCreatingTimedEventsInsideTimedEvents => false;
        bool ProposeSmartScriptOnMainPage => true;
        string CreatureSmartAiName => "SmartAI";
        string GameObjectSmartAiName => "SmartGameObjectAI";

        string? ForceLoadTag => null;
    }
}