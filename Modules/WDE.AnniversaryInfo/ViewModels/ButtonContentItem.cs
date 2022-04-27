using System.Windows.Input;
using Prism.Mvvm;

namespace WDE.AnniversaryInfo.ViewModels;

public class ButtonContentItem : BindableBase, IContentItem
{
    public ButtonContentItem(string text, ICommand command)
    {
        Text = text;
        Command = command;
    }

    public string Text { get; }
    public ICommand Command { get; }
}