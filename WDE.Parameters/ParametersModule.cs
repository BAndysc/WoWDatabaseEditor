using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

using Prism.Modularity;
using WDE.Common;
using WDE.Common.Parameters;
using WDE.Parameters.ViewModels;
using WDE.Parameters.Views;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.Module;
using WDE.Common.Events;

namespace WDE.Parameters
{
    [AutoRegister]
    public class ParametersModule : ModuleBase
    {
        public ParametersModule()
        {
        }

        public override void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.Resolve<AllModulesLoaded>().Subscribe(() =>
            {
                new ParameterLoader(containerProvider.Resolve<IDatabaseProvider>()).Load(containerProvider.Resolve<ParameterFactory>());
            });
        }
    }
}