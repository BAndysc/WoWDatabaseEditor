using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module;
using Prism.Modularity;
using WDE.Module.Attributes;
using Prism.Ioc;
using WDE.Common.Events;
using Prism.Events;
using WDE.ThemeChanger.Providers;
using WDE.ThemeChanger.ViewModels;
using WDE.Common.Managers;

namespace WDE.ThemeChanger
{
    [AutoRegister, SingleInstance]
    public class ThemeChangerModule : ModuleBase
    {
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.Resolve<IThemeManager>();
        }
    }
}
