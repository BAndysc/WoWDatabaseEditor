using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Prism.Modularity;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Editor.Views;
using Prism.Ioc;
using Prism.Events;
using WDE.Common.Providers;
using WDE.Common.DBC;
using WDE.Common.Solution;
using WDE.SmartScriptEditor.Providers;

namespace WDE.SmartScriptEditor
{
    public class SmartScriptModule : IModule
    {
        public SmartScriptModule()
        {
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            SmartDataLoader.Load(SmartDataManager.GetInstance(), new SmartDataFileLoader());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}
