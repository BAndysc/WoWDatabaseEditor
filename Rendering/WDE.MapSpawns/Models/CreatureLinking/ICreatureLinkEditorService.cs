using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TheMaths;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Models.CreatureLinking;

[UniqueProvider]
public interface ICreatureLinkEditorService
{
    /// <summary>True when the active core has a creature_linking SQL provider (gates the tool).</summary>
    bool IsSupported { get; }

    /// <summary>True when the active core has a creature_linking_template SQL provider (gates the
    /// entry link mode).</summary>
    bool SupportsTemplateLinks { get; }

    /// <summary>The map whose links are currently loaded (-1 if none).</summary>
    int LoadedMap { get; }

    /// <summary>Which table a new drag-created link lands in.</summary>
    CreatureLinkMode Mode { get; set; }

    /// <summary>When false, links render as faint, non-interactive arrows and dragging is disabled.</summary>
    bool ToolEnabled { get; set; }

    // in-progress drag rubber-band: set by the module, drawn by the render stage
    bool DragActive { get; set; }
    Vector3 DragFrom { get; set; }
    Vector3 DragTo { get; set; }
    bool DragSnapped { get; set; }

    /// <summary>All loaded guid-based links (creature_linking) whose slave spawn lives on the map.</summary>
    ObservableCollection<EditableCreatureLink> GuidLinks { get; }

    /// <summary>All loaded entry-based links (creature_linking_template) on the map.</summary>
    ObservableCollection<EditableCreatureLinkTemplate> TemplateLinks { get; }

    /// <summary>The selected link: an <see cref="EditableCreatureLink"/> or
    /// <see cref="EditableCreatureLinkTemplate"/> (null = nothing selected).</summary>
    object? Selected { get; set; }

    /// <summary>Merges off-thread loaded links into the collections + refreshes the spawn index.
    /// Call once per frame on the engine thread.</summary>
    void PumpPendingLoads();

    Task LoadForMap(int mapId);

    /// <summary>Resolves the slave/master spawn world positions of a guid link (false if either is
    /// missing).</summary>
    bool TryGetEndpoints(EditableCreatureLink link, out Vector3 slavePos, out Vector3 masterPos);

    /// <summary>Resolves a loaded creature spawn position by guid.</summary>
    bool TryGetCreaturePosition(uint guid, out Vector3 pos);

    /// <summary>Resolves the entry of a loaded creature spawn by guid.</summary>
    bool TryGetCreatureEntry(uint guid, out uint entry);

    /// <summary>Fills <paramref name="output"/> with (slave, master) world-position pairs for an
    /// entry link: each loaded spawn of the slave entry paired with its nearest loaded master-entry
    /// spawn within the search range (0 = whole map). Empty when no pairing resolves.</summary>
    void CollectTemplateArrows(EditableCreatureLinkTemplate link, List<(Vector3 slave, Vector3 master)> output);

    /// <summary>Creates a slave→master guid link, replacing any existing link for the same slave.
    /// Null if either guid isn't a loaded creature (or slave == master).</summary>
    EditableCreatureLink? AddGuidLink(uint slaveGuid, uint masterGuid);

    /// <summary>Creates a slave-entry→master-entry template link on the given map, replacing any
    /// existing link for the same (entry, map). Null if slaveEntry == masterEntry.</summary>
    EditableCreatureLinkTemplate? AddTemplateLink(uint slaveEntry, uint map, uint masterEntry);

    void RemoveGuidLink(EditableCreatureLink link);
    void RemoveTemplateLink(EditableCreatureLinkTemplate link);

    bool AnyDirty { get; }

    /// <summary>The exact SQL <see cref="Save"/> would execute right now (null = nothing dirty).
    /// Pure: no execution, no state change.</summary>
    WDE.SqlQueryGenerator.IQuery? BuildSaveQuery();

    Task Save();
}
