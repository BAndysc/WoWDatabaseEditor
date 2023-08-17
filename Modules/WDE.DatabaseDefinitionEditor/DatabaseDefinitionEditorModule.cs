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
    }
}