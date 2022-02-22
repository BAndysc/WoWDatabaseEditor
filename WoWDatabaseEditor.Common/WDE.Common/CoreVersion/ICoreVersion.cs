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
        IGameVersionFeatures GameVersionFeatures { get; }
        bool SupportsRbac => true;
        bool SupportsSpecialCommands => false;
        bool SupportsReverseCommands => false;
    }
    
    public interface IGameVersionFeatures
    {
        CharacterRaces AllRaces { get; }
        CharacterClasses AllClasses { get; }
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
        bool AlternativeTrinityDatabase { get; }
        bool HasAiEntry => false;
    }

    public interface ISmartScriptFeatures
    {
        ISet<SmartScriptType> SupportedTypes { get; }
        bool SupportsCreatingTimedEventsInsideTimedEvents => false;
        bool ProposeSmartScriptOnMainPage => true;
        bool SupportsConditionTargetVictim => false;
        string CreatureSmartAiName => "SmartAI";
        string GameObjectSmartAiName => "SmartGameObjectAI";

        string? ForceLoadTag => null;
        string TableName { get; }

        string? ActionsPath => null;
        string? EventsPath => null;
        string? TargetsPath => null;
        string? ActionGroupPath => null;
        string? EventGroupPath => null;
        string? TargetGroupPath => null;
    }
}