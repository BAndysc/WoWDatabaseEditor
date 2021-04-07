using Prism.Ioc;
using SmartFormat;
using WDE.Common.Windows;
using WDE.Module;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.ViewModels;

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
            AutoRegisterByConvention(typeof(SmartFactory).Assembly, moduleScope);
            
            var t = new ToolSmartEditorViewModel();
            containerRegistry.RegisterInstance(typeof(ITool), t);
            containerRegistry.RegisterInstance(typeof(IToolSmartEditorViewModel), t);
        }
    }
}