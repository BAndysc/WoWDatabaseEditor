using System;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using WDE.Common.Windows;
using WDE.Module;
using WDE.Module.Attributes;

[assembly: ModuleRequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]

namespace WDE.DbScriptsEditor.Avalonia
{
    public class DbScriptsAvaloniaModule : ModuleBase
    {
        public override void RegisterViews(IViewLocator viewLocator)
        {
            base.RegisterViews(viewLocator);
            // The dbscript action list is rendered with EventAI/SmartScript-style templated rows;
            // register the row theme (this module is loaded as a plugin, so the core App.xaml can't
            // StyleInclude it directly).
            Application.Current!.Styles.Add(new StyleInclude(new Uri("resm:Styles?assembly=WDE.DbScriptsEditor.Avalonia"))
            {
                Source = new Uri("avares://WDE.DbScriptsEditor.Avalonia/Themes/Generic.axaml")
            });
        }
    }
}
