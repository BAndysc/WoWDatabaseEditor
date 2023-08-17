using System;
using System.Reflection.Metadata;
using System.Windows.Input;
using Avalonia.Input;
using DynamicData.Binding;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Utils.DragDrop;
using WDE.DatabaseEditors.Data.Structs;
using WDE.MVVM;
using DragDropEffects = WDE.Common.Utils.DragDrop.DragDropEffects;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor;

public partial class CommandViewModel : ObservableBase, IDropTarget
{
    public DefinitionViewModel Parent { get; }

    [Notify] private string commandId;
    [Notify] private bool showInToolbar;
    [Notify] private bool showInContextMenu;
    [Notify] private ParameterViewModel? selectedParameter;
    [Notify] [AlsoNotify(nameof(HasGesture))] private KeyGesture? keyGesture;
    public ObservableCollectionExtended<ParameterViewModel> Parameters { get; } =
        new ObservableCollectionExtended<ParameterViewModel>();

    public bool HasGesture
    {
        get => keyGesture != null;
        set
        {
            if (value == false)
                KeyGesture = null;
            else
                KeyGesture = new KeyGesture(Key.None);
        }
    }
    
    public ICommand AddParameterCommand { get; }
    public ICommand DeleteSelectedParameterCommand { get; }
    public DatabaseCommandUsage Usage => (showInToolbar ? DatabaseCommandUsage.Toolbar : 0) | (showInContextMenu ? DatabaseCommandUsage.ContextMenu : 0);

    public CommandViewModel(DefinitionViewModel parent, DatabaseCommandDefinitionJson json)
    {
        Parent = parent;
        commandId = json.CommandId;
        keyGesture = json.KeyBinding == null ? null : KeyGesture.Parse(json.KeyBinding);
        showInToolbar = json.Usage.HasFlagFast(DatabaseCommandUsage.Toolbar);
        showInContextMenu = json.Usage.HasFlagFast(DatabaseCommandUsage.ContextMenu);

        if (json.Parameters != null)
        {
            foreach (var param in json.Parameters)
                Parameters.Add(new ParameterViewModel(this, param));
        }
        
        if ((json.Usage & ~(DatabaseCommandUsage.Toolbar | DatabaseCommandUsage.ContextMenu)) != 0)
            throw new Exception("Unknown usage flags: " + json.Usage);

        AddParameterCommand = new DelegateCommand(() =>
        {
            Parameters.Add(new ParameterViewModel(this, "-"));
        });
        DeleteSelectedParameterCommand = new DelegateCommand(() =>
        {
            if (SelectedParameter != null)
                Parameters.Remove(SelectedParameter);
        });
    }

    public partial class ParameterViewModel : ObservableBase
    {
        public CommandViewModel Parent { get; }
        [Notify] private string columnName;

        public ParameterViewModel(CommandViewModel parent, string columnName)
        {
            Parent = parent;
            this.columnName = columnName;
        }
    }
    
    public void DragOver(IDropInfo dropInfo)
    {
        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        dropInfo.Effects = DragDropEffects.Move;
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is not ParameterViewModel data)
            return;

        int indexOf = Parameters.IndexOf(data);
        int dropIndex = dropInfo.InsertIndex;
        if (indexOf < dropIndex)
            dropIndex--;

        Parameters.RemoveAt(indexOf);
        Parameters.Insert(dropIndex, data);
        SelectedParameter = data;
    }
}