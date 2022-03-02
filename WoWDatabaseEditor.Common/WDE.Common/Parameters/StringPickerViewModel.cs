using System;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Managers;

namespace WDE.Common.Parameters;

public class StringPickerViewModel : BindableBase, IDialog
{
    private readonly bool acceptEmpty;
    private string content;

    public StringPickerViewModel(string existing, bool acceptEmpty, bool multiline)
    {
        this.acceptEmpty = acceptEmpty;
        MultiLine = multiline;
        content = existing;
        Accept = new DelegateCommand(() =>
        {
            CloseOk?.Invoke();
        }, () => acceptEmpty || !string.IsNullOrWhiteSpace(Content)).ObservesProperty(() => Content);
        Cancel = new DelegateCommand(() =>
        {
            CloseCancel?.Invoke();
        });
    }
    
    public bool MultiLine { get; }

    public string Content
    {
        get => content;
        set => SetProperty(ref content, value);
    }

    public int DesiredWidth => 650;
    public int DesiredHeight => 350;
    public string Title => "Edit string";
    public bool Resizeable => true;
    public ICommand Accept { get; set; }
    public ICommand Cancel { get; set; }
    public event Action? CloseCancel;
    public event Action? CloseOk;
}