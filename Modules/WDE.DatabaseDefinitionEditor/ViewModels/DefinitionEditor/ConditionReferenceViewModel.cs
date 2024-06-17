using System;
using System.Linq;
using System.Windows.Input;
using DynamicData.Binding;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Utils.DragDrop;
using WDE.DatabaseEditors.Data.Structs;
using WDE.MVVM;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor;

public partial class ConditionReferenceViewModel : ObservableBase, IDropTarget
{
    public DefinitionViewModel Parent { get; }
    [Notify] private int sourceType;
    [Notify] private bool sourceGroupColumnAbs;
    [Notify] private string? sourceGroupColumn;
    [Notify] private string? sourceEntryColumn;
    [Notify] private string? sourceIdColumn;
    [Notify] private string? setColumn;

    [Notify] private ConditionTargetViewModel? selectedTarget;

    public ObservableCollectionExtended<ConditionTargetViewModel> Targets { get; } =
        new ObservableCollectionExtended<ConditionTargetViewModel>();

    public ICommand AddTarget { get; }
    public ICommand DeleteSelectedTarget { get; }

    public ConditionReferenceViewModel(DefinitionViewModel parent,
        DatabaseConditionReferenceJson json)
    {
        Parent = parent;
        sourceType = json.SourceType;
        if (json.SourceGroupColumn?.Name.ForeignTable != null)
            throw new Exception("Foreign Table in SourceGroupColumn is not supported (But could be)");
        if (json.SourceEntryColumn?.ForeignTable != null)
            throw new Exception("Foreign Table in SourceEntryColumn is not supported (But could be)");
        if (json.SourceIdColumn?.ForeignTable != null)
            throw new Exception("Foreign Table in SourceIdColumn is not supported (But could be)");

        sourceGroupColumn = json.SourceGroupColumn?.Name.ColumnName;
        sourceGroupColumnAbs = json.SourceGroupColumn?.IsAbs ?? false;
        sourceEntryColumn = json.SourceEntryColumn?.ColumnName;
        sourceIdColumn = json.SourceIdColumn?.ColumnName;
        setColumn = json.SetColumn?.ColumnName;
        if (json.Targets != null)
        {
            foreach (var target in json.Targets)
                Targets.Add(new ConditionTargetViewModel(this, target));
            if (Targets.Count > 0)
                SelectedTarget = Targets[0];
        }

        AddTarget = new DelegateCommand(() =>
        {
            var maxId = Targets.Count == 0 ? -1 : Targets.Max(t => (int)t.Id);
            Targets.Add(new ConditionTargetViewModel(this, new DatabaseConditionTargetJson(){Id = (uint)(maxId+1)}));
        });
        DeleteSelectedTarget = new DelegateCommand(() =>
        {
            if (SelectedTarget != null)
                Targets.Remove(SelectedTarget);
        });
    }

    public void DragOver(IDropInfo dropInfo)
    {
        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        dropInfo.Effects = DragDropEffects.Move;
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is not ConditionTargetViewModel data)
            return;

        int indexOf = Targets.IndexOf(data);
        int dropIndex = dropInfo.InsertIndex;
        if (indexOf < dropIndex)
            dropIndex--;

        Targets.RemoveAt(indexOf);
        Targets.Insert(dropIndex, data);
        SelectedTarget = data;
    }
}

public partial class ConditionTargetViewModel : ObservableBase
{
    [Notify] private uint id;
    [Notify] private string name;
    
    public ConditionTargetViewModel(ConditionReferenceViewModel parent,
        DatabaseConditionTargetJson json)
    {
        id = json.Id;
        name = json.Name;
    }
}