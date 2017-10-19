using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using WDE.Common;
using WDE.Common.DBC;

namespace WDE.DbcStore
{
    public class DbcStoreModule : IModule
    {
        private readonly IUnityContainer _container;

        public DbcStoreModule(IUnityContainer container)
        {
            _container = container;
        }

        public void Initialize()
        {
            Store = new DbcStore(_container);
            _container.RegisterInstance<IDbcStore>(Store, new ContainerControlledLifetimeManager());
        }

        public static DbcStore Store;
    }
}
