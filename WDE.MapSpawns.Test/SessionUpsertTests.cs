using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Database;
using WDE.MapSpawns.Models.Solution;
using WDE.QueryGenerators.Base;
using WDE.Sessions.Sessions;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Test;

/// <summary>
/// Regression tests for the "session shows stale formation state" bug: add a formation + save, then
/// delete it + save — the session kept showing the INSERT (and vice versa: delete + save, re-add +
/// save — the session kept showing the DELETE). Two independent guarantees cover it now:
/// (1) EditorSession.Insert's replace branch must raise CollectionChanged (it used to be silent, so
/// the sessions panel — which mirrors the session via that event — kept the stale item), and
/// (2) the items are KEY-ONLY, so the query is regenerated from the CURRENT DB rows on every upsert.
/// </summary>
public class SessionUpsertTests
{
    private EditorSession session = null!;
    private List<NotifyCollectionChangedEventArgs> events = null!;

    [SetUp]
    public void SetUp()
    {
        session = new EditorSession("test", "test.sql", DateTime.Now, DateTime.Now);
        events = new List<NotifyCollectionChangedEventArgs>();
        session.CollectionChanged += (_, e) => events.Add(e);
    }

    [Test]
    public void AddFormationThenDeleteIt_SessionEndsUpWithTheDeleteQuery()
    {
        // add a formation, save -> the per-leader item lands in the session with its DELETE+INSERT
        var created = new FormationsSolutionItem { LeaderGuid = 10 };
        session.Insert(created, "DELETE FROM creature_formations WHERE leaderGUID = 10;\nINSERT INTO creature_formations ...;");

        // delete the formation, save -> the editor publishes a NEW item instance (same key) and the
        // bridge upserts it with the regenerated query; the session entry must now be just the DELETE
        var deleted = new FormationsSolutionItem { LeaderGuid = 10 };
        session.Insert(deleted, "DELETE FROM creature_formations WHERE leaderGUID = 10;");

        var entries = session.ToList();
        Assert.AreEqual(1, entries.Count);
        Assert.AreSame(deleted, entries[0].Item1);
        StringAssert.DoesNotContain("INSERT", entries[0].Item2);

        // the sessions panel mirrors the session through CollectionChanged - the replace MUST notify
        // (this is the regression: it used to be silent, leaving the stale item in the UI)
        Assert.AreEqual(2, events.Count);
        Assert.AreEqual(NotifyCollectionChangedAction.Replace, events[1].Action);
        Assert.AreSame(deleted, events[1].NewItems![0]);
        Assert.AreSame(created, events[1].OldItems![0]);
        Assert.AreEqual(0, events[1].OldStartingIndex);
    }

    [Test]
    public void DeleteFormationThenReAddIt_SessionEndsUpWithTheInsertQuery()
    {
        // delete an existing formation, save -> DELETE in the session (this part always worked)
        var deleted = new FormationsSolutionItem { LeaderGuid = 10 };
        session.Insert(deleted, "DELETE FROM creature_formations WHERE leaderGUID = 10;");

        // re-add it, save -> the session entry must flip back to DELETE+INSERT and notify
        var recreated = new FormationsSolutionItem { LeaderGuid = 10 };
        session.Insert(recreated, "DELETE FROM creature_formations WHERE leaderGUID = 10;\nINSERT INTO creature_formations ...;");

        var entries = session.ToList();
        Assert.AreEqual(1, entries.Count);
        Assert.AreSame(recreated, entries[0].Item1);
        StringAssert.Contains("INSERT", entries[0].Item2);
        Assert.AreEqual(NotifyCollectionChangedAction.Replace, events[1].Action);
    }

    [Test]
    public void ReplaceNotifiesWithTheRightIndex_WhenOtherItemsSurroundIt()
    {
        session.Insert(new FormationsSolutionItem { LeaderGuid = 10 }, "q10");
        session.Insert(new WaypointsSolutionItem { Source = 0, Key = 100 }, "qw");
        session.Insert(new FormationsSolutionItem { LeaderGuid = 20 }, "q20");
        events.Clear();

        // re-save of leader 10 replaces only ITS entry, in place
        session.Insert(new FormationsSolutionItem { LeaderGuid = 10 }, "q10b");

        Assert.AreEqual(new[] { "q10b", "qw", "q20" }, session.Select(p => p.Item2).ToArray());
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(NotifyCollectionChangedAction.Replace, events[0].Action);
        Assert.AreEqual(0, events[0].OldStartingIndex);
    }

    private class FormationRow : ICreatureFormation
    {
        public uint LeaderGuid { get; init; }
        public uint MemberGuid { get; init; }
        public float Dist { get; init; }
        public float Angle { get; init; }
        public uint GroupAi { get; init; }
        public uint Point1 { get; init; }
        public uint Point2 { get; init; }
    }

    [Test]
    public void Formations_GeneratedSql_ReflectsTheCurrentDatabaseState()
    {
        // the item is key-only: what ends up in the session/export is regenerated from the CURRENT
        // creature_formations rows, so the same item yields DELETE+INSERT while the formation exists
        // and DELETE-only after it was removed
        var db = Substitute.For<IDatabaseProvider>();
        var gen = Substitute.For<IQueryGenerator<ICreatureFormation>>();
        gen.TryDelete(Arg.Any<ICreatureFormation>())
            .Returns(ci => Queries.Raw(DataDatabaseType.World,
                $"DELETE FROM creature_formations WHERE leaderGUID = {ci.Arg<ICreatureFormation>().LeaderGuid};"));
        gen.TryBulkInsert(Arg.Any<IReadOnlyCollection<ICreatureFormation>>())
            .Returns(ci => Queries.Raw(DataDatabaseType.World,
                $"INSERT INTO creature_formations VALUES ({ci.Arg<IReadOnlyCollection<ICreatureFormation>>().Count} rows);"));
        var providers = new FormationsSolutionItemProviders(db, gen);
        var item = new FormationsSolutionItem { LeaderGuid = 10 };

        // formation exists in the DB (2 members, plus another leader's row that must be ignored)
        db.GetCreatureFormations().Returns(new List<ICreatureFormation>
        {
            new FormationRow { LeaderGuid = 10, MemberGuid = 11 },
            new FormationRow { LeaderGuid = 10, MemberGuid = 12 },
            new FormationRow { LeaderGuid = 99, MemberGuid = 98 },
        });
        var liveSql = providers.GenerateSql(item).Result.QueryString;
        StringAssert.Contains("DELETE", liveSql);
        StringAssert.Contains("(2 rows)", liveSql);
        Assert.Less(liveSql.IndexOf("DELETE", StringComparison.Ordinal),
            liveSql.IndexOf("INSERT", StringComparison.Ordinal), "idempotent: DELETE must precede INSERT");

        // formation deleted since -> the SAME item now regenerates to just the DELETE
        db.GetCreatureFormations().Returns(new List<ICreatureFormation>());
        var deletedSql = providers.GenerateSql(item).Result.QueryString;
        StringAssert.Contains("DELETE", deletedSql);
        StringAssert.DoesNotContain("INSERT", deletedSql);
    }
}
