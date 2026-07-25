using WDE.Common.Database;

namespace WDE.MapSpawns.Models.CreatureLinking;

/// <summary>Which table a new drag-created link is written to.</summary>
public enum CreatureLinkMode
{
    /// <summary>creature_linking: one concrete slave spawn → one master spawn (guids).</summary>
    Guid,
    /// <summary>creature_linking_template: every spawn of the slave entry on the map → the master
    /// entry (paired by proximity in-core).</summary>
    Entry,
}

/// <summary>
/// One loaded <c>creature_linking</c> row being edited: a slave spawn reacts to its master spawn.
/// The slave guid is the row identity (table primary key) and is immutable; the master guid and
/// flag are editable. Endpoint world positions aren't stored here - they're resolved live from the
/// spawns each frame, so dragging a creature moves its arrows.
/// </summary>
public sealed class EditableCreatureLink : ICreatureLinking
{
    public uint Guid { get; }
    public uint MasterGuid { get; private set; }
    public uint Flag { get; private set; }

    public bool IsDirty { get; private set; }

    public EditableCreatureLink(uint guid, uint masterGuid, uint flag, bool dirty = false)
    {
        Guid = guid;
        MasterGuid = masterGuid;
        Flag = flag;
        IsDirty = dirty;
    }

    public void SetMaster(uint masterGuid)
    {
        MasterGuid = masterGuid;
        IsDirty = true;
    }

    public void SetFlag(uint flag)
    {
        Flag = flag;
        IsDirty = true;
    }

    public void MarkDirty() => IsDirty = true;
    public void ClearDirty() => IsDirty = false;
}

/// <summary>
/// One loaded <c>creature_linking_template</c> row being edited: all spawns of the slave entry on
/// the map react to the master entry. The (entry, map) pair is the row identity (table primary key)
/// and is immutable; the master entry, flag and search range are editable.
/// </summary>
public sealed class EditableCreatureLinkTemplate : ICreatureLinkingTemplate
{
    public uint Entry { get; }
    public uint Map { get; }
    public uint MasterEntry { get; private set; }
    public uint Flag { get; private set; }
    public uint SearchRange { get; private set; }

    public bool IsDirty { get; private set; }

    public EditableCreatureLinkTemplate(uint entry, uint map, uint masterEntry, uint flag,
        uint searchRange, bool dirty = false)
    {
        Entry = entry;
        Map = map;
        MasterEntry = masterEntry;
        Flag = flag;
        SearchRange = searchRange;
        IsDirty = dirty;
    }

    public void SetMaster(uint masterEntry)
    {
        MasterEntry = masterEntry;
        IsDirty = true;
    }

    public void SetFlag(uint flag)
    {
        Flag = flag;
        IsDirty = true;
    }

    public void SetSearchRange(uint searchRange)
    {
        SearchRange = searchRange;
        IsDirty = true;
    }

    public void MarkDirty() => IsDirty = true;
    public void ClearDirty() => IsDirty = false;
}

/// <summary>The creature_linking flag bits (shared by both tables), matching the generic
/// CmangosCreatureLinkingFlagsParameter definition. Rendered as a checklist in the inspector.</summary>
public static class CreatureLinkFlags
{
    public static readonly (uint Bit, string Name)[] All =
    {
        (1, "aggro on aggro"),
        (2, "to aggro on aggro"),
        (4, "respawn on evade"),
        (8, "to respawn on evade"),
        (16, "despawn on death"),
        (32, "selfkill on death"),
        (64, "respawn on death"),
        (128, "respawn on respawn"),
        (256, "despawn on respawn"),
        (512, "follow"),
        (1024, "cant spawn if boss dead"),
        (2048, "cant spawn if boss alive"),
        (4096, "despawn on evade"),
        (8192, "despawn on despawn"),
        (16384, "evade on evade"),
    };
}
