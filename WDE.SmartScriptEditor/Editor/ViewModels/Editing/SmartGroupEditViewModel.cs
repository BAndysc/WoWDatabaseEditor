using System;
using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Managers;
using WDE.MVVM;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels.Editing;

public partial class SmartGroupEditViewModel : ObservableBase, IDialog
{
    [Notify] private string header;
    [Notify] private string? description;
    
    private readonly SmartGroup group;

    public int DesiredWidth => 400;
    public int DesiredHeight => 250;
    public string Title => "Edit group";
    public bool Resizeable => true;
    public ICommand Accept { get; }
    public ICommand Cancel { get; }
    public event Action? CloseCancel;
    public event Action? CloseOk;

    public SmartGroupEditViewModel(SmartGroup group)
    {
        this.group = group;
        header = group.Header;
        description = group.Description;

        var accept = new DelegateCommand(() =>
        {
            group.Header = header;
            group.Description = description;
            CloseOk?.Invoke();
        }, () => !string.IsNullOrWhiteSpace(header));
        Accept = accept;
        Cancel = new DelegateCommand(() =>
        {
            CloseCancel?.Invoke();
        });
        On(() => Header, _ => accept.RaiseCanExecuteChanged());
    }
}