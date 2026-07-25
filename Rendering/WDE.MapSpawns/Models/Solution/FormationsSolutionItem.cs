using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;

namespace WDE.MapSpawns.Models.Solution;

/// <summary>
/// Session/solution representation of ONE formation touched by the 3D formation editor, keyed by its
/// natural key - the leader guid (items compare equal iff same type + same leader, so a session holds
/// one item per formation and a re-save of the same leader replaces it). The item is KEY-ONLY, like
/// table solution items: the live save already wrote the rows, so the SQL provider re-reads the
/// group's CURRENT creature_formations rows from the DB at generate time and rewrites the group
/// wholesale (DELETE by leader + bulk INSERT; no rows = just the DELETE).
/// </summary>
public class FormationsSolutionItem : ISolutionItem
{
    public uint LeaderGuid { get; set; }

    [JsonIgnore] public bool IsContainer => false;
    [JsonIgnore] public ObservableCollection<ISolutionItem>? Items => null;
    [JsonIgnore] public string? ExtraId => LeaderGuid.ToString();
    [JsonIgnore] public bool IsExportable => true;

    public ISolutionItem Clone() => new FormationsSolutionItem { LeaderGuid = LeaderGuid };

    public override bool Equals(object? obj) => obj is FormationsSolutionItem other && other.LeaderGuid == LeaderGuid;
    public override int GetHashCode() => HashCode.Combine(typeof(FormationsSolutionItem), LeaderGuid);
}
