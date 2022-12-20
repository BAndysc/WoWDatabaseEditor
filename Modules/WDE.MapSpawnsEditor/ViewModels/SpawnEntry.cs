using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using PropertyChanged.SourceGenerator;
using WDE.Common.Database;
using WDE.Common.Utils;

namespace WDE.MapSpawnsEditor.ViewModels;

public partial class SpawnGroup : IParentType
{
    public SpawnGroup(GroupType type, uint entry, string name)
    {
        Entry = entry;
        Type = type;
        Name = name;
        Header = (name + " (" + Entry + ")");
        Nested.CollectionChanged += (_, e) => NestedParentsChanged?.Invoke(this, e);
        Spawns.CollectionChanged += (_, e) => ChildrenChanged?.Invoke(this, e);
    }
    
    public SpawnGroup(ICreatureTemplate creatureTemplate) : this(GroupType.Creature, creatureTemplate.Entry, creatureTemplate.Name)
    {
    }
    
    public SpawnGroup(IGameObjectTemplate gameObjectTemplate) : this(GroupType.Creature, gameObjectTemplate.Entry, gameObjectTemplate.Name)
    {
    }

    [Notify] private bool isExpanded;
    public uint Entry { get; }
    public GroupType Type { get; }
    public string Name { get; }
    public string Header { get;  }

    public override string ToString() => Header;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event NotifyCollectionChangedEventHandler? ChildrenChanged;
    public event NotifyCollectionChangedEventHandler? NestedParentsChanged;

    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; }
    public IReadOnlyList<IParentType> NestedParents => Nested;
    public IReadOnlyList<IChildType> Children => Spawns;
    public ObservableCollection<SpawnGroup> Nested { get; } = new();
    public ObservableCollection<SpawnInstance> Spawns { get; } = new();
    public IParentType? Parent { get; set; }
}