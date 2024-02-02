using Prism.Ioc;
using SmartFormat;
using WDE.Common.Services.QueryParser;
using WDE.Common.Windows;
using WDE.Module;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Services;
using WDE.TrinitySmartScriptEditor.Providers;

namespace WDE.TrinitySmartScriptEditor
{
    public class SmartScriptModule : ScopedModuleBase
    {
        public SmartScriptModule(IScopedContainer scopedContainer) : base(scopedContainer)
        {
            Smart.Default.Parser.UseAlternativeEscapeChar();
        }

        public override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterTypes(containerRegistry);
            AutoRegisterByConvention(typeof(SmartFactory).Assembly, containerRegistry);
            AutoRegisterByConvention(typeof(SmartScriptModule).Assembly, containerRegistry);
            
            var t = new ToolSmartEditorViewModel();
            containerRegistry.RegisterInstance(typeof(ITool), t);
            containerRegistry.RegisterInstance(typeof(IToolSmartEditorViewModel), t);
            containerRegistry.Register(typeof(IQueryParserProvider), typeof(SmartScriptQueryParser), nameof(SmartScriptQueryParser));
        }

        public override void RegisterFallbackTypes(IContainerRegistry container)
        {
            base.RegisterFallbackTypes(container);
            AutoRegisterFallbackTypesByConvention(typeof(SmartFactory).Assembly, container);
        }

        public override void FinalizeRegistration(IContainerRegistry container)
        {
            base.FinalizeRegistration(container);
            if (!container.IsRegistered(typeof(IFavouriteSmartsService)))
                container.RegisterSingleton<IFavouriteSmartsService, FavouriteSmartsService>();
        }
    }
}