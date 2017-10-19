using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Solutions.Manager;

namespace WDE.Solutions
{
    public class SolutionsModule : IModule
    {
        private readonly IUnityContainer _container;

        public SolutionsModule(IUnityContainer container)
        {
            this._container = container;
        }

        public void Initialize()
        {
            _container.RegisterType<ISolutionItemProvider, SolutionFolderItemProvider>("Solution Folder");

            _container.RegisterType<ISolutionManager, SolutionManager>(new ContainerControlledLifetimeManager());

        }
    }
}
