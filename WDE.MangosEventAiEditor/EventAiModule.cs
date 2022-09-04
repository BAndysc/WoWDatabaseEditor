using Prism.Ioc;
using SmartFormat;
using WDE.Common.Services.QueryParser;
using WDE.Common.Windows;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Editor.ViewModels;
using WDE.EventAiEditor.Services;
using WDE.MangosEventAiEditor.Providers;
using WDE.Module;
using WDE.Module.Attributes;

[assembly: ModuleRequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]

namespace WDE.MangosEventAiEditor
{
    public class EventAiModule : ScopedModuleBase
    {
        public EventAiModule(IScopedContainer scopedContainer) : base(scopedContainer)
        {
            Smart.Default.Parser.UseAlternativeEscapeChar();
        }

        public override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterTypes(containerRegistry);
            AutoRegisterByConvention(typeof(EventAiFactory).Assembly, moduleScope);
            
            var t = new ToolEventAiEditorViewModel();
            containerRegistry.RegisterInstance(typeof(ITool), t, "eai");
            containerRegistry.RegisterInstance(typeof(IToolEventAiEditorViewModel), t);
            containerRegistry.Register(typeof(IQueryParserProvider), typeof(EventAiQueryParser), nameof(EventAiQueryParser));
        }

        public override void FinalizeRegistration(IContainerRegistry container)
        {
            base.FinalizeRegistration(container);
            if (!container.IsRegistered(typeof(IFavouriteEventAiService)))
                container.RegisterSingleton<IFavouriteEventAiService, FavouriteEventAiService>();
        }
    }
}