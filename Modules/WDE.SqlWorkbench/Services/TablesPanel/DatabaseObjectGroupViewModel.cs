using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using PropertyChanged.SourceGenerator;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WDE.SqlWorkbench.Services.TablesPanel;

internal partial class DatabaseObjectGroupViewModel : ObservableBase, INamedParentType
{
    [Notify] private bool isExpanded;
    
    private ObservableCollection<IChildType> children = new ObservableCollection<IChildType>();
    
    public DatabaseObjectGroupViewModel(SchemaViewModel schema,
        string name,
        ImageUri icon, 
        IReadOnlyList<IChildType> childList)
    {
        Parent = schema;
        Schema = schema;
        Name = name;
        Icon = icon;
        children.AddRange(childList);
        children.CollectionChanged += (sender, args) => ChildrenChanged?.Invoke(this, args);
    }
    
    public SchemaViewModel Schema { get; }
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; }
    public IParentType? Parent { get; set; }
    public string Name { get; }
    public ImageUri Icon { get; }
    public bool CanBeExpanded => true;
    public IReadOnlyList<IParentType> NestedParents { get; } = Array.Empty<IParentType>();
    public IReadOnlyList<IChildType> Children => children;
    public event NotifyCollectionChangedEventHandler? ChildrenChanged;
    public event NotifyCollectionChangedEventHandler? NestedParentsChanged;
}