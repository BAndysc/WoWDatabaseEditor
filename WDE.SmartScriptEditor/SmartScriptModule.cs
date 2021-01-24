using Prism.Ioc;
using SmartFormat;
using WDE.Common.Windows;
using WDE.Module;
using WDE.SmartScriptEditor.Editor.ViewModels;

namespace WDE.SmartScriptEditor
{
    public class SmartScriptModule : ModuleBase
    {
        public SmartScriptModule()
        {
            Smart.Default.Parser.UseAlternativeEscapeChar();
        }

        public override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterTypes(containerRegistry);
            var t = new ToolSmartEditorViewModel();
            containerRegistry.RegisterInstance(typeof(ITool), t);
            containerRegistry.RegisterInstance(typeof(IToolSmartEditorViewModel), t);
        }
    }
}