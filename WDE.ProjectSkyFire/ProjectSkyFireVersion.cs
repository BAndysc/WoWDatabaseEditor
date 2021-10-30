using System;
using System.Collections.Generic;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.ProjectSkyFire
{
    [AutoRegister]
    [SingleInstance]
    public class ProjectSkyFireVersion : ICoreVersion, IDatabaseFeatures, ISmartScriptFeatures, IConditionFeatures
    {
        public string Tag => "SkyFire";
        public string FriendlyName => "ProjectSkyFire Mists of Pandaria";
        public ISmartScriptFeatures SmartScriptFeatures => this;
        public IConditionFeatures ConditionFeatures => this;
        public IDatabaseFeatures DatabaseFeatures => this;

        public ISet<Type> UnsupportedTables { get; } = new HashSet<Type>{typeof(IAreaTriggerTemplate),
            typeof(IConversationTemplate),
            typeof(IAuthRbacPermission),
            typeof(IAuthRbacLinkedPermission)
        };
        public bool AlternativeTrinityDatabase => false;
        
        public ISet<SmartScriptType> SupportedTypes { get; } = new HashSet<SmartScriptType>
        {
            SmartScriptType.Creature,
            SmartScriptType.GameObject,
            SmartScriptType.AreaTrigger,
            SmartScriptType.TimedActionList,
        };

        public string TableName => "smart_scripts";
        public string ConditionsFile => "SmartData/conditions.json";
        public string ConditionGroupsFile => "SmartData/conditions_groups.json";
        public string ConditionSourcesFile => "SmartData/condition_sources.json";
    }
}