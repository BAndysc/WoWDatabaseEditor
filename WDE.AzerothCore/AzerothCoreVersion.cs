using System;
using System.Collections.Generic;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.AzerothCore
{
    [AutoRegister]
    [SingleInstance]
    public class AzerothCoreVersion : ICoreVersion, IDatabaseFeatures, ISmartScriptFeatures
    {
        public string Tag => "Azeroth";
        public string FriendlyName => "AzerothCore Wrath of the Lich King";
        public ISmartScriptFeatures SmartScriptFeatures => this;
        public IDatabaseFeatures DatabaseFeatures => this;

        public ISet<Type> UnsupportedTables { get; } = new HashSet<Type>{typeof(IAreaTriggerTemplate), typeof(IConversationTemplate)};
        public ISet<SmartScriptType> SupportedTypes { get; } = new HashSet<SmartScriptType>
        {
            SmartScriptType.Creature,
            SmartScriptType.GameObject,
            SmartScriptType.AreaTrigger,
            SmartScriptType.TimedActionList,
        };
    }
}