using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;

namespace WDE.MapSpawns.Models.Solution;

/// <summary>
/// Session/solution representation of ONE waypoint path touched by the 3D waypoint editor, keyed by
/// its natural key - the source table + path key (path id, or creature guid for mangos
/// creature_movement). Items compare equal iff same type + same (source, key), so a session holds one
/// item per path and a re-save of the same path replaces it. Deliberately does NOT carry the points -
/// the exported query loads the path's CURRENT rows from the DB at generate time (the live save
/// already wrote them), exactly like table solution items, so the session file stays small.
/// </summary>
public class WaypointsSolutionItem : ISolutionItem
{
    /// <summary>WaypointSource as int, for serialization stability.</summary>
    public int Source { get; set; }
    public uint Key { get; set; }

    /// <summary>Secondary key for compound-keyed sources (the PathId of creature_movement_template);
    /// 0 for single-key sources.</summary>
    public uint Key2 { get; set; }

    [JsonIgnore] public bool IsContainer => false;
    [JsonIgnore] public ObservableCollection<ISolutionItem>? Items => null;
    [JsonIgnore] public string? ExtraId => Key2 == 0 ? Key.ToString() : $"{Key}:{Key2}";
    [JsonIgnore] public bool IsExportable => true;

    public ISolutionItem Clone() => new WaypointsSolutionItem { Source = Source, Key = Key, Key2 = Key2 };

    public override bool Equals(object? obj) =>
        obj is WaypointsSolutionItem other && other.Source == Source && other.Key == Key && other.Key2 == Key2;
    public override int GetHashCode() => HashCode.Combine(typeof(WaypointsSolutionItem), Source, Key, Key2);
}
