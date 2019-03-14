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
using WDE.Module.Attributes;
using WDE.DbcStore.Data;
using Newtonsoft.Json;
using System.IO;
using WDE.DbcStore.Views;
using WDE.DbcStore.ViewModels;
using WDE.Module;
using Prism.Events;
using WDE.Common.Events;

namespace WDE.DbcStore
{
    [AutoRegister]
    public class DbcStoreModule : ModuleBase
    {        
        public DbcStoreModule()
        {
        }

        public override void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.Resolve<IEventAggregator>().GetEvent<AllModulesLoaded>().Subscribe(() => {
                containerProvider.Resolve<DbcStore>().Load();
            });
        }
    }
}
