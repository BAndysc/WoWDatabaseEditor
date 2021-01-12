using System;
using System.Collections.Generic;
using System.Windows.Controls;
using WDE.Common;
using WDE.Module.Attributes;
using WoWDatabaseEditor.ModulesManagement.Configuration.Views;

namespace WoWDatabaseEditor.ModulesManagement.Configuration
{
    [AutoRegister]
    [SingleInstance]
    public class ModulesConfiguration : IConfigurable
    {
        public KeyValuePair<ContentControl, Action> GetConfigurationView()
        {
            ModulesConfigView? view = new();
            return new KeyValuePair<ContentControl, Action>(view, () => { });
        }

        public string GetName()
        {
            return "Modules";
        }
    }
}