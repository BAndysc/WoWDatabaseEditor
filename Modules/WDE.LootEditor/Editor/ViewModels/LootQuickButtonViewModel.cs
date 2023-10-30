using System.Windows.Input;
using WDE.Common.Types;
using WDE.LootEditor.Services;
using WDE.MVVM;

namespace WDE.LootEditor.Editor.ViewModels;

public class LootQuickButtonViewModel : ObservableBase
{
    public string? Text { get; }
    public string? ToolTip { get; }
    public ImageUri? Icon { get; }
    public ICommand Command { get; }
    public bool HasIcon => Icon != null && !string.IsNullOrEmpty(Icon.Value.Uri);

    public LootQuickButtonViewModel(LootButtonDefinition def, ICommand command)
    {
        Text = def.ButtonText;
        ToolTip = def.ButtonToolTip;
        Icon = string.IsNullOrEmpty(def.Icon) ? null : new ImageUri(def.Icon);
        Command = command;
    }
}