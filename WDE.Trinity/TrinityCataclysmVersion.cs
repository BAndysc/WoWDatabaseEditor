﻿using System;
using System.Collections.Generic;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Trinity
{
    [AutoRegister]
    [SingleInstance]
    public class TrinityCataclysmVersion : ICoreVersion, IDatabaseFeatures, ISmartScriptFeatures
    {
        public string Tag => "TrinityCata";
        public string FriendlyName => "The Cataclysm Preservation Project";

        public IDatabaseFeatures DatabaseFeatures => this;
        public ISmartScriptFeatures SmartScriptFeatures => this;
        
        public ISet<Type> UnsupportedTables { get; } = new HashSet<Type>{typeof(IAreaTriggerTemplate)};
        public ISet<SmartScriptType> SupportedTypes { get; } = new HashSet<SmartScriptType>
        {
            SmartScriptType.Creature,
            SmartScriptType.GameObject,
            SmartScriptType.AreaTrigger,
            SmartScriptType.TimedActionList,
        };
    }
}