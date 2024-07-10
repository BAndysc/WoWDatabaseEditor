using System;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Menu;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.DatabaseDefinitionEditor;

[AutoRegister]
public class DefinitionsEditorToolMenu : IToolMenuItem
{
    public string ItemName => "Table definitions editor";
    public ICommand ItemCommand { get; }
    public MenuShortcut? Shortcut => null;

    public DefinitionsEditorToolMenu(Lazy<ITableDefinitionEditorService> editorService)
    {
        ItemCommand = new DelegateCommand(() =>
        {
            editorService.Value.Open();
        });
    }
}