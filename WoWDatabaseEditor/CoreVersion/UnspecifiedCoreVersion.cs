using System;
using System.Collections.Generic;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.CoreVersion
{
    [AutoRegister]
    [SingleInstance]
    public class UnspecifiedCoreVersion : ICoreVersion, IDatabaseFeatures, ISmartScriptFeatures
    {
        public string Tag => "unspecified";
        public string FriendlyName => "Unspecified";
        public IDatabaseFeatures DatabaseFeatures => this;
        public ISmartScriptFeatures SmartScriptFeatures => this;
        public ISet<Type> UnsupportedTables => new HashSet<Type>();
        public ISet<SmartScriptType> SupportedTypes => new HashSet<SmartScriptType>();
    }
}