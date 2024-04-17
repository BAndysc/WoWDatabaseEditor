using System.ComponentModel;
using System.Runtime.CompilerServices;
using TheEngine.ECS;
using WDE.Common.Database;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapSpawnsEditor.Models;

namespace WDE.MapSpawnsEditor.ViewModels;

public abstract class SpawnInstance : IChildType, IManagedComponentData, INotifyPropertyChanged
{
    public abstract uint Guid { get; }
    public abstract uint Entry { get; }
    public abstract Vector3 Position { get; }
    public abstract uint PhaseMask { get; }
    public abstract SmallReadOnlyList<int>? Phases { get; }
    public abstract int Map { get; }
    public abstract (int, int) Chunk { get; }
    public abstract string Header { get; protected set; }
    public abstract WorldObjectInstance? WorldObject { get; }
    public ISpawnGroupTemplate? SpawnGroup { get; set; }
    public abstract bool IsVisibleInEvents(IGameEventService eventService);
    public abstract bool IsVisibleInPhase(IGamePhaseService gamePhaseService);
    
    public bool IsSpawned => WorldObject != null;
    public abstract void Dispose();
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; }
    public IParentType? Parent { get; set; }
    public bool CanBeExpanded => false;
    public abstract ImageUri Icon { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}