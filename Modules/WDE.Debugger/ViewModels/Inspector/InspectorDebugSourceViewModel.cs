using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using PropertyChanged.SourceGenerator;
using WDE.Common.Debugging;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WDE.Debugger.ViewModels.Inspector;

internal partial class InspectorDebugSourceViewModel : ObservableBase, IParentType
{
    private readonly IDebugPointSource source;

    public string Name => source.Name;

    public InspectorDebugSourceViewModel(IDebugPointSource source)
    {
        this.source = source;
        DebugPoints.CollectionChanged += (_, e) =>
        {
            ChildrenChanged?.Invoke(this, e);
            RaisePropertyChanged(nameof(CanBeExpanded));
        };
    }

    public ObservableCollection<InspectorDebugPointViewModel> DebugPoints { get; } = new();
    public bool CanBeExpanded => DebugPoints.Count > 0;
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; } = true;
    public IParentType? Parent { get; set; }
    [Notify] private bool isExpanded = true;
    public IReadOnlyList<IParentType> NestedParents => Array.Empty<IParentType>();
    public IReadOnlyList<IChildType> Children => DebugPoints;
    public event NotifyCollectionChangedEventHandler? ChildrenChanged;
    public event NotifyCollectionChangedEventHandler? NestedParentsChanged;
}