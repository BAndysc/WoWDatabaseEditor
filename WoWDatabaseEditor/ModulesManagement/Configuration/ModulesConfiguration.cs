using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WDE.Common;
using WDE.Module.Attributes;
using WoWDatabaseEditor.ModulesManagement.Configuration.Views;

namespace WoWDatabaseEditor.ModulesManagement.Configuration
{
    [AutoRegister, SingleInstance]
    public class ModulesConfiguration : IConfigurable
    {
        public ModulesConfiguration()
        {
        }

        public KeyValuePair<ContentControl, Action> GetConfigurationView()
        {
            var view = new ModulesConfigView();
            return new KeyValuePair<ContentControl, Action>(view, () => { });
        }

        public string GetName()
        {
            return "Modules";
        }
    }
}
