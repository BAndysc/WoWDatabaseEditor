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
using WDE.Common.Attributes;
using WDE.DbcStore.Data;
using Newtonsoft.Json;
using System.IO;
using WDE.DbcStore.Views;
using WDE.DbcStore.ViewModels;

namespace WDE.DbcStore
{
    [AutoRegister]
    public class DbcStoreModule : IModule
    {        
        public DbcStoreModule(IParameterFactory parameterFactory)
        {
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.Resolve<DbcStore>().Load();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}
