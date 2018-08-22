using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

using Prism.Modularity;
using WDE.Common;
using WDE.Common.DBC;
using Prism.Ioc;
using WDE.Common.Parameters;

namespace WDE.DbcStore
{
    public class DbcStoreModule : IModule
    {
        private readonly IParameterFactory parameterFactory;

        public DbcStoreModule(IParameterFactory parameterFactory)
        {
            this.parameterFactory = parameterFactory;
        }
        
        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            var store = new DbcStore(parameterFactory);
            containerRegistry.RegisterInstance<IDbcStore>(store);
            containerRegistry.RegisterInstance<ISpellStore>(store);
        }
    }
}
