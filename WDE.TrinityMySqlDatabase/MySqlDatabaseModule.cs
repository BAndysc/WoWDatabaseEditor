using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

using Newtonsoft.Json;
using Prism.Modularity;
using WDE.Common;
using WDE.Common.Database;
using WDE.TrinityMySqlDatabase.Data;
using WDE.TrinityMySqlDatabase.ViewModels;
using WDE.TrinityMySqlDatabase.Views;
using Prism.Ioc;
using WDE.Common.Attributes;
using WDE.TrinityMySqlDatabase.Providers;

namespace WDE.TrinityMySqlDatabase
{
    [AutoRegister, SingleInstance]
    public class TrinityMySqlDatabaseModule : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }
    }
}