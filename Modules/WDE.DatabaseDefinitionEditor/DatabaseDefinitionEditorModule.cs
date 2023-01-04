using System;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using WDE.Common.Windows;
using WDE.DatabaseDefinitionEditor.ViewModels;
using WDE.DatabaseDefinitionEditor.Views;
using WDE.Module;

namespace WDE.DatabaseDefinitionEditor;

public class DatabaseDefinitionEditorModule : ModuleBase
{
    public override void RegisterViews(IViewLocator viewLocator)
    {
        base.RegisterViews(viewLocator);
        // table editor
        viewLocator.Bind<ToolsViewModel, DefinitionToolView>();
        // table editor
        //viewLocator.Bind<DefinitionEditorViewModel, DefinitionEditorView>();

        Application.Current!.Styles.Add(new StyleInclude(new Uri("resm:Styles?assembly=WDE.DatabaseDefinitionEditor")){Source = new Uri("avares://WDE.DatabaseDefinitionEditor/Themes/Generic.axaml")});
    }
}