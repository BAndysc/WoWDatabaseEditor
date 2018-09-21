using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Prism.Commands;
using Prism.Modularity;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.SQLEditor.ViewModels;
using WDE.SQLEditor.Views;
using Prism.Ioc;

namespace WDE.SQLEditor
{
    public class SqlEditorModule : IModule
    {        
        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}
