using NUnit.Framework;
using WDE.MapSpawns.Models.Solution;

namespace WDE.MapSpawns.Test;

/// <summary>The items are KEY-ONLY, keyed by their natural key (formations: leader guid; spawn
/// groups: group id; waypoints: source table + path key) - all content is re-read from the DB at
/// query-generate time. EditorSession.Insert finds-or-adds by item equality, so equality-by-key is
/// what makes a re-save of the same formation/group/path REPLACE its session entry while different
/// keys get separate entries.</summary>
public class SolutionItemKeyTests
{
    [Test]
    public void Formations_EqualIffSameLeader()
    {
        var a = new FormationsSolutionItem { LeaderGuid = 10 };

        Assert.IsTrue(a.Equals(new FormationsSolutionItem { LeaderGuid = 10 }));
        Assert.AreEqual(a.GetHashCode(), new FormationsSolutionItem { LeaderGuid = 10 }.GetHashCode());
        Assert.IsFalse(a.Equals(new FormationsSolutionItem { LeaderGuid = 20 }));
    }

    [Test]
    public void SpawnGroups_EqualIffSameGroupId()
    {
        var a = new SpawnGroupsSolutionItem { GroupId = 5 };

        Assert.IsTrue(a.Equals(new SpawnGroupsSolutionItem { GroupId = 5 }));
        Assert.AreEqual(a.GetHashCode(), new SpawnGroupsSolutionItem { GroupId = 5 }.GetHashCode());
        Assert.IsFalse(a.Equals(new SpawnGroupsSolutionItem { GroupId = 6 }));
    }

    [Test]
    public void Waypoints_EqualIffSameSourceAndKey()
    {
        var a = new WaypointsSolutionItem { Source = 0, Key = 100 };

        Assert.IsTrue(a.Equals(new WaypointsSolutionItem { Source = 0, Key = 100 }));
        Assert.AreEqual(a.GetHashCode(), new WaypointsSolutionItem { Source = 0, Key = 100 }.GetHashCode());
        Assert.IsFalse(a.Equals(new WaypointsSolutionItem { Source = 0, Key = 101 })); // other path
        Assert.IsFalse(a.Equals(new WaypointsSolutionItem { Source = 2, Key = 100 })); // same key, other table
    }

    [Test]
    public void DifferentItemTypesAreNeverEqual_EvenWithSameNumericKey()
    {
        Assert.IsFalse(new FormationsSolutionItem { LeaderGuid = 5 }
            .Equals(new SpawnGroupsSolutionItem { GroupId = 5 }));
        Assert.IsFalse(new SpawnGroupsSolutionItem { GroupId = 100 }
            .Equals(new WaypointsSolutionItem { Source = 0, Key = 100 }));
    }

    [Test]
    public void Clones_KeepTheKey()
    {
        Assert.IsTrue(new FormationsSolutionItem { LeaderGuid = 10 }
            .Equals(new FormationsSolutionItem { LeaderGuid = 10 }.Clone()));
        Assert.IsTrue(new SpawnGroupsSolutionItem { GroupId = 5 }
            .Equals(new SpawnGroupsSolutionItem { GroupId = 5 }.Clone()));
        Assert.IsTrue(new WaypointsSolutionItem { Source = 2, Key = 7 }
            .Equals(new WaypointsSolutionItem { Source = 2, Key = 7 }.Clone()));
    }
}
