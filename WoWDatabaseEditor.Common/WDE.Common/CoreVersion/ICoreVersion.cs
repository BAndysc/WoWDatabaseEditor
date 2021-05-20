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
    }

    public interface IDatabaseFeatures
    {
        ISet<Type> UnsupportedTables { get; }
    }

    public interface ISmartScriptFeatures
    {
        ISet<SmartScriptType> SupportedTypes { get; }
        bool SupportsCreatingTimedEventsInsideTimedEvents => false;
        string CreatureSmartAiName => "SmartAI";
        string GameObjectSmartAiName => "SmartGameObjectAI";

        string? ForceLoadTag => null;
    }
}