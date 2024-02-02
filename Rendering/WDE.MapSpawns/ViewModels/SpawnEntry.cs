using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using PropertyChanged.SourceGenerator;
using WDE.Common.Database;
using WDE.Common.Utils;

namespace WDE.MapSpawns.ViewModels;

public partial class SpawnEntry : IParentType
{
    public SpawnEntry(ICreatureTemplate creatureTemplate)
    {
        Entry = creatureTemplate.Entry;
        Type = SpawnType.Creature;
        Name = creatureTemplate.Name;
        Header = creatureTemplate.Name + " (" + Entry + ")";
    }
    
    public SpawnEntry(IGameObjectTemplate gameObjectTemplate)
    {
        Entry = gameObjectTemplate.Entry;
        Type = SpawnType.GameObject;
        Name = gameObjectTemplate.Name;
        Header = gameObjectTemplate.Name + " (" + Entry + ")";
    }

    [Notify] private bool isExpanded;
    public uint Entry { get; }
    public SpawnType Type { get; }
    public string Name { get; }
    public string Header { get;  }

    public override string ToString() => Name;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event NotifyCollectionChangedEventHandler? ChildrenChanged
    {
        add => Spawns.CollectionChanged += value;
        remove => Spawns.CollectionChanged -= value;
    }

    public event NotifyCollectionChangedEventHandler? NestedParentsChanged;

    public IReadOnlyList<IParentType> NestedParents { get; } = new List<IParentType>();
    public IReadOnlyList<IChildType> Children => Spawns;
    public ObservableCollection<SpawnInstance> Spawns { get; } = new();
    public bool CanBeExpanded => true;
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; }
    public IParentType? Parent { get; set; }
}