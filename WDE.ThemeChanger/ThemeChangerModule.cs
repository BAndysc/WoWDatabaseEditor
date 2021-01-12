using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Module;
using WDE.Module.Attributes;

namespace WDE.ThemeChanger
{
    [AutoRegister]
    [SingleInstance]
    public class ThemeChangerModule : ModuleBase
    {
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.Resolve<IThemeManager>();
        }
    }
}