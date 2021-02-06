﻿using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Trinity
{
    [AutoRegister]
    [SingleInstance]
    public class TrinityMasterVersion : ICoreVersion, IDatabaseFeatures, ISmartScriptFeatures
    {
        public string Tag => "TrinityMaster";
        public string FriendlyName => "TrinityCore Shadowlands";
        public ISmartScriptFeatures SmartScriptFeatures => this;
        public IDatabaseFeatures DatabaseFeatures => this;
        
        public ISet<Type> UnsupportedTables { get; } = new HashSet<Type>();
        public ISet<SmartScriptType> SupportedTypes { get; } = new HashSet<SmartScriptType>
        {
            SmartScriptType.Creature,
            SmartScriptType.GameObject,
            SmartScriptType.AreaTrigger,
            SmartScriptType.TimedActionList,
            SmartScriptType.AreaTriggerEntity,
            SmartScriptType.AreaTriggerEntityServerSide
        };
    }
}