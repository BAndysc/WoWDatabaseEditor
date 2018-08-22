using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Prism.Modularity;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Solutions.Manager;
using Prism.Ioc;
using WDE.Common.Solution;
using WDE.Solutions.Providers;

namespace WDE.Solutions
{
    public class SolutionsModule : IModule
    {
        public SolutionsModule()
        {
        }
        

        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.Resolve<ISolutionItemNameRegistry>().Register(containerProvider.Resolve<FolderNameProvider>());
            containerProvider.Resolve<ISolutionItemSqlGeneratorRegistry>().Register(containerProvider.Resolve<FolderSqlProvider>());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<ISolutionItemProvider, SolutionFolderItemProvider>("Solution Folder");
            containerRegistry.RegisterSingleton<ISolutionManager, SolutionManager>();
            containerRegistry.Register<ISolutionExplorer, SolutionExplorer>();
        }
    }
}
