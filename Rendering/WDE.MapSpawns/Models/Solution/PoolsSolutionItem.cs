using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;

namespace WDE.MapSpawns.Models.Solution;

/// <summary>
/// Session/solution representation of ONE spawn pool touched by the 3D pool editor, keyed by its
/// natural key - the pool entry (items compare equal iff same type + same pool id, so a session
/// holds one item per pool and a re-save of the same pool replaces it). The item is KEY-ONLY, like
/// table solution items: the live save already wrote the rows, so the SQL provider re-reads the
/// pool's CURRENT template + members + nesting from the DB at generate time and rewrites them
/// idempotently (template DELETE+INSERT, members/nesting DELETE-all + bulk INSERT; template gone =
/// just the DELETEs).
/// </summary>
public class PoolsSolutionItem : ISolutionItem
{
    public uint PoolId { get; set; }

    [JsonIgnore] public bool IsContainer => false;
    [JsonIgnore] public ObservableCollection<ISolutionItem>? Items => null;
    [JsonIgnore] public string? ExtraId => PoolId.ToString();
    [JsonIgnore] public bool IsExportable => true;

    public ISolutionItem Clone() => new PoolsSolutionItem { PoolId = PoolId };

    public override bool Equals(object? obj) => obj is PoolsSolutionItem other && other.PoolId == PoolId;
    public override int GetHashCode() => HashCode.Combine(typeof(PoolsSolutionItem), PoolId);
}
