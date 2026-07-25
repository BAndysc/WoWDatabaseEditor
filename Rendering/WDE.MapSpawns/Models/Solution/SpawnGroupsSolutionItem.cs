using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;

namespace WDE.MapSpawns.Models.Solution;

/// <summary>
/// Session/solution representation of ONE spawn group touched by the 3D spawn-group editor, keyed by
/// its natural key - the group id (items compare equal iff same type + same group id, so a session
/// holds one item per group and a re-save of the same group replaces it). The item is KEY-ONLY, like
/// table solution items: the live save already wrote the rows, so the SQL provider re-reads the
/// group's CURRENT template + membership from the DB at generate time and rewrites both idempotently
/// (template DELETE+INSERT, membership DELETE-all + bulk INSERT; template gone = just the DELETEs).
/// </summary>
public class SpawnGroupsSolutionItem : ISolutionItem
{
    public uint GroupId { get; set; }

    [JsonIgnore] public bool IsContainer => false;
    [JsonIgnore] public ObservableCollection<ISolutionItem>? Items => null;
    [JsonIgnore] public string? ExtraId => GroupId.ToString();
    [JsonIgnore] public bool IsExportable => true;

    public ISolutionItem Clone() => new SpawnGroupsSolutionItem { GroupId = GroupId };

    public override bool Equals(object? obj) => obj is SpawnGroupsSolutionItem other && other.GroupId == GroupId;
    public override int GetHashCode() => HashCode.Combine(typeof(SpawnGroupsSolutionItem), GroupId);
}
