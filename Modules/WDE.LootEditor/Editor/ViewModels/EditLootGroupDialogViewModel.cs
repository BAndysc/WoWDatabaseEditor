using System;
using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Managers;
using WDE.MVVM;

namespace WDE.LootEditor.Editor.ViewModels;

public partial class EditLootGroupDialogViewModel : ObservableBase, IDialog
{
    private readonly ILootEditorFeatures features;
    [Notify] private string name;
    [Notify] private bool dontLoadRecursively;
    
    public bool HasFlags { get; }
    
    public EditLootGroupDialogViewModel(ILootEditorFeatures features,
        LootGroup group)
    {
        HasFlags = features.LootGroupHasFlags(group.LootSourceType);
        this.features = features;
        name = group.GroupName ?? "";
        dontLoadRecursively = group.DontLoadRecursively;
        Accept = new DelegateCommand(() => CloseOk?.Invoke());
        Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
    }
    
    public int DesiredWidth => 400;
    public int DesiredHeight => 180;
    public string Title => "Edit loot group";
    public bool Resizeable => true;
    public ICommand Accept { get; set; }
    public ICommand Cancel { get; set; }
    public event Action? CloseCancel;
    public event Action? CloseOk;
}