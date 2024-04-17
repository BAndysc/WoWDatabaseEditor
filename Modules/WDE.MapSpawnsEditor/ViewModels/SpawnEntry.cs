using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using PropertyChanged.SourceGenerator;
using WDE.Common.Database;
using WDE.Common.Types;
using WDE.Common.Utils;

namespace WDE.MapSpawnsEditor.ViewModels;

public partial class SpawnGroup : IParentType
{
    public SpawnGroup(GroupType type, int entry, string name)
    {
        Entry = entry;
        Type = type;
        Name = name;
        Header = (name + " (" + Entry + ")");
        Nested.CollectionChanged += (_, e) => NestedParentsChanged?.Invoke(this, e);
        Spawns.CollectionChanged += (_, e) => ChildrenChanged?.Invoke(this, e);
        switch (type)
        {
            case GroupType.Creature:
                Icon = new ImageUri("Icons/document_gameobjects.png");
                break;
            case GroupType.GameObject:
                Icon = new ImageUri("Icons/document_gameobjects.png");
                break;
            case GroupType.Map:
                Icon = new ImageUri("Icons/icon_world.png");
                break;
            case GroupType.Zone:
            case GroupType.Area:
                Icon = new ImageUri("Icons/icon_folder_2.png");
                break;
            case GroupType.SpawnGroup:
                Icon = new ImageUri("Icons/icon_spawngroup.png");
                break;
        }
    }
    
    public SpawnGroup(ICreatureTemplate creatureTemplate) : this(GroupType.Creature, (int)creatureTemplate.Entry, creatureTemplate.Name)
    {
        Icon = new ImageUri("Icons/document_creatures.png");
    }
    
    public SpawnGroup(IGameObjectTemplate gameObjectTemplate) : this(GroupType.Creature, (int)gameObjectTemplate.Entry, gameObjectTemplate.Name)
    {
        Icon = new ImageUri("Icons/document_gameobjects.png");
    }

    private bool isExpanded;
    private bool isTemporaryExpanded;

    public bool IsTemporaryExpanded
    {
        set
        {
            isTemporaryExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            if (Type is GroupType.Area or GroupType.Zone)
                Icon = IsExpanded ? new ImageUri("Icons/icon_folder_2_opened.png") : new ImageUri("Icons/icon_folder_2.png");
        }
    }

    public bool IsExpanded
    {
        get => isExpanded || isTemporaryExpanded;
        set
        {
            isTemporaryExpanded = false;
            isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            if (Type is GroupType.Area or GroupType.Zone)
                Icon = IsExpanded ? new ImageUri("Icons/icon_folder_2_opened.png") : new ImageUri("Icons/icon_folder_2.png");
        }
    }

    public int Entry { get; }
    public GroupType Type { get; }
    public string Name { get; }
    public string Header { get;  }
    [Notify] private ImageUri icon;

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
    public bool CanBeExpanded => true;
}