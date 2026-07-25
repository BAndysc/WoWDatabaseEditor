using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using NSubstitute;
using NUnit.Framework;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Tasks;
using WDE.MapSpawns.Models.Solution;
using WDE.MapSpawns.Models.WorldPoints;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Test;

/// <summary>
/// Save/delete semantics of the two world-point editors (graveyards + spell target positions):
/// idempotent per-key rewrites, deleted keys emit only DELETEs, never-saved keys are forgotten,
/// pending-deleted safe-loc ids stay reserved, links dedupe on save, one destination per spell.
/// </summary>
public class WorldPointEditorTests
{
    private static IQueryGenerator<T> Generator<T>(string table) where T : class
    {
        var gen = Substitute.For<IQueryGenerator<T>>();
        gen.TableName.Returns(DatabaseTable.WorldTable(table));
        gen.TryDelete(Arg.Any<T>()).Returns(Queries.Raw(DataDatabaseType.World, $"-- delete {table}"));
        gen.TryDeleteAll(Arg.Any<T>()).Returns(Queries.Raw(DataDatabaseType.World, $"-- delete all {table}"));
        gen.TryInsert(Arg.Any<T>()).Returns(Queries.Raw(DataDatabaseType.World, $"-- insert {table}"));
        gen.TryBulkInsert(Arg.Any<IReadOnlyCollection<T>>()).Returns(Queries.Raw(DataDatabaseType.World, $"-- bulk insert {table}"));
        return gen;
    }

    private static IMainThread InlineMainThread()
    {
        var mainThread = Substitute.For<IMainThread>();
        mainThread.Schedule(Arg.Any<Func<Task<bool>>>()).Returns(ci => ci.Arg<Func<Task<bool>>>()());
        mainThread.Schedule(Arg.Any<Func<Dictionary<uint, string>>>())
            .Returns(ci => Task.FromResult(ci.Arg<Func<Dictionary<uint, string>>>()()));
        return mainThread;
    }

    // ------------------------------------------------------------ safe locs ----------------------

    private IDatabaseProvider safeLocDb = null!;
    private IMySqlExecutor mySqlExecutor = null!;
    private IQueryGenerator<IWorldSafeLoc> locGen = null!;
    private IQueryGenerator<IGraveyardLink> linkGen = null!;
    private SafeLocEditorService safeLocs = null!;
    private SpellTargetEditorService spellTargets = null!;
    private IQueryGenerator<ISpellTargetPosition> positionGen = null!;
    // saves publish their executed SQL - the bridge turns it into GENERIC table solution items
    // via IQueryParserService (no dedicated solution items for these editors)
    private List<string> publishedQueries = null!;

    [SetUp]
    public async Task SetUp()
    {
        safeLocDb = Substitute.For<IDatabaseProvider>();
        safeLocDb.GetWorldSafeLocsAsync().Returns(new List<IWorldSafeLoc>
        {
            new AbstractWorldSafeLoc { Id = 1, Map = 0, X = 10, Y = 20, Z = 30, O = 1.5f, Name = "Crossroads" },
            new AbstractWorldSafeLoc { Id = 4, Map = 1, X = 5, Y = 5, Z = 5, Name = "Kalimdor GY" },
        });
        safeLocDb.GetGraveyardLinksAsync().Returns(new List<IGraveyardLink>
        {
            new AbstractGraveyardLink { SafeLocId = 1, GhostLoc = 17, LinkKind = GraveyardLinkKind.Area, Faction = 67 },
        });
        safeLocDb.GetSpellTargetPositionsAsync().Returns(new List<ISpellTargetPosition>
        {
            new AbstractSpellTargetPosition { SpellId = 442, Map = 0, X = 1, Y = 2, Z = 3, O = 0.5f },
        });

        mySqlExecutor = Substitute.For<IMySqlExecutor>();
        var mainThread = InlineMainThread();
        var eventAggregator = new EventAggregator();
        publishedQueries = new List<string>();
        // like the real bridge: stamp Handled synchronously, complete Parsed right away
        // (the parse-before-execute ordering itself is the bridge's job, not the service's)
        eventAggregator.GetEvent<WorldEditQuerySavingEvent>().Subscribe(save =>
        {
            publishedQueries.Add(save.Query);
            save.Handled = true;
            save.NotifyParsed();
        });

        locGen = Generator<IWorldSafeLoc>("world_safe_locs");
        linkGen = Generator<IGraveyardLink>("game_graveyard_zone");
        positionGen = Generator<ISpellTargetPosition>("spell_target_position");

        safeLocs = new SafeLocEditorService(safeLocDb, mySqlExecutor, mainThread, eventAggregator, locGen, linkGen);
        await safeLocs.LoadForMap(0);
        safeLocs.PumpPendingLoads();

        var parameterFactory = Substitute.For<IParameterFactory>();
        var spellParameter = new Parameter
        {
            Items = new Dictionary<long, SelectOption> { [442] = new("Fireball") }
        };
        parameterFactory.Factory("SpellParameter").Returns(spellParameter);

        spellTargets = new SpellTargetEditorService(safeLocDb, mySqlExecutor, mainThread, eventAggregator,
            parameterFactory, positionGen);
        await spellTargets.LoadForMap(0);
        spellTargets.PumpPendingLoads();
    }

    [Test]
    public void SafeLocs_LoadsRowsAndLinks()
    {
        Assert.AreEqual(2, safeLocs.Locs.Count);
        var loc = safeLocs.Locs[1];
        Assert.AreEqual("Crossroads", loc.Name);
        Assert.AreEqual(new Vector3(10, 20, 30), loc.Position);
        Assert.AreEqual(1, loc.Links.Count);
        Assert.AreEqual(GraveyardLinkKind.Area, loc.Links[0].Kind);
        Assert.IsFalse(safeLocs.AnyDirty);
    }

    [Test]
    public async Task SafeLocs_Save_RewritesLocAndLinksIdempotently()
    {
        var loc = safeLocs.Locs[1];
        loc.Name = "New name";
        loc.Links.Add(new GraveyardLinkRow { GhostLoc = 12, Kind = GraveyardLinkKind.Area, Faction = 0 });
        loc.Links.Add(new GraveyardLinkRow { GhostLoc = 12, Kind = GraveyardLinkKind.Area, Faction = 469 }); // dupe key
        safeLocs.NotifyChanged(1);

        await safeLocs.Save();

        locGen.Received().TryDelete(Arg.Is<IWorldSafeLoc>(l => l.Id == 1));
        locGen.Received().TryInsert(Arg.Is<IWorldSafeLoc>(l => l.Id == 1 && l.Name == "New name"));
        linkGen.Received().TryDeleteAll(Arg.Is<IGraveyardLink>(l => l.SafeLocId == 1));
        // duplicate (ghost_loc, kind) rows collapse - they'd violate the PK
        linkGen.Received().TryBulkInsert(Arg.Is<IReadOnlyCollection<IGraveyardLink>>(rows =>
            rows.Count == 2 && rows.Count(r => r.GhostLoc == 12) == 1));

        // the executed SQL is published for the bridge's query-parser session tracking
        Assert.AreEqual(1, publishedQueries.Count);
        StringAssert.Contains("insert world_safe_locs", publishedQueries[0]);
        StringAssert.Contains("bulk insert game_graveyard_zone", publishedQueries[0]);
        Assert.IsFalse(safeLocs.AnyDirty);
    }

    [Test]
    public async Task SafeLocs_DeleteSaved_EmitsOnlyDeletes()
    {
        safeLocs.DeleteLoc(1);
        Assert.IsFalse(safeLocs.Locs.ContainsKey(1));

        await safeLocs.Save();

        locGen.Received().TryDelete(Arg.Is<IWorldSafeLoc>(l => l.Id == 1));
        linkGen.Received().TryDeleteAll(Arg.Is<IGraveyardLink>(l => l.SafeLocId == 1));
        locGen.DidNotReceive().TryInsert(Arg.Is<IWorldSafeLoc>(l => l.Id == 1));
        linkGen.DidNotReceive().TryBulkInsert(Arg.Any<IReadOnlyCollection<IGraveyardLink>>());
        Assert.IsFalse(safeLocs.AnyDirty);
    }

    [Test]
    public async Task SafeLocs_DeleteUnsaved_IsForgotten()
    {
        var id = safeLocs.CreateAt(0, new Vector3(1, 2, 3), 0f);
        safeLocs.DeleteLoc(id);

        Assert.IsFalse(safeLocs.AnyDirty);
        await safeLocs.Save();
        await mySqlExecutor.DidNotReceive().ExecuteSql(Arg.Any<IQuery>());
    }

    [Test]
    public void SafeLocs_PendingDeletedId_NotReused()
    {
        safeLocs.DeleteLoc(4); // highest id, saved
        var id = safeLocs.CreateAt(0, new Vector3(0, 0, 0), 0f);
        Assert.AreEqual(5u, id);
    }

    // ------------------------------------------------------------ spell targets ------------------

    [Test]
    public void SpellTargets_LoadsRowsAndNames()
    {
        Assert.AreEqual(1, spellTargets.Positions.Count);
        Assert.AreEqual("Fireball", spellTargets.GetSpellName(442));
        Assert.IsNull(spellTargets.GetSpellName(99999));
    }

    [Test]
    public void SpellTargets_OneDestinationPerSpell()
    {
        Assert.IsFalse(spellTargets.CreateForSpell(442, 0, new Vector3(9, 9, 9), 0f));
        Assert.IsTrue(spellTargets.CreateForSpell(100, 1, new Vector3(9, 9, 9), 0f));
        Assert.IsFalse(spellTargets.CreateForSpell(100, 1, new Vector3(8, 8, 8), 0f));
    }

    [Test]
    public async Task SpellTargets_Save_RewritesRow()
    {
        var row = spellTargets.Positions[442];
        row.Position = new Vector3(50, 60, 70);
        spellTargets.NotifyChanged(442);

        await spellTargets.Save();

        positionGen.Received().TryDelete(Arg.Is<ISpellTargetPosition>(p => p.SpellId == 442));
        positionGen.Received().TryInsert(Arg.Is<ISpellTargetPosition>(p => p.SpellId == 442 && p.X == 50));
        Assert.AreEqual(1, publishedQueries.Count);
        StringAssert.Contains("insert spell_target_position", publishedQueries[0]);
        Assert.IsFalse(spellTargets.AnyDirty);
    }

    [Test]
    public async Task SpellTargets_DeleteSaved_EmitsOnlyDelete()
    {
        spellTargets.DeletePosition(442);
        await spellTargets.Save();

        positionGen.Received().TryDelete(Arg.Is<ISpellTargetPosition>(p => p.SpellId == 442));
        positionGen.DidNotReceive().TryInsert(Arg.Any<ISpellTargetPosition>());
    }

    [Test]
    public async Task SpellTargets_DeleteUnsaved_IsForgotten()
    {
        spellTargets.CreateForSpell(777, 0, new Vector3(1, 1, 1), 0f);
        spellTargets.DeletePosition(777);

        Assert.IsFalse(spellTargets.AnyDirty);
        await spellTargets.Save();
        await mySqlExecutor.DidNotReceive().ExecuteSql(Arg.Any<IQuery>());
    }

    [Test]
    public async Task SpellTargets_RecreatePendingDeleted_BecomesPlainRewrite()
    {
        spellTargets.DeletePosition(442);
        Assert.IsTrue(spellTargets.CreateForSpell(442, 1, new Vector3(4, 4, 4), 0f));

        await spellTargets.Save();

        // the delete-then-recreate collapses into the normal idempotent rewrite
        positionGen.Received().TryDelete(Arg.Is<ISpellTargetPosition>(p => p.SpellId == 442));
        positionGen.Received().TryInsert(Arg.Is<ISpellTargetPosition>(p => p.SpellId == 442 && p.Map == 1));
    }
}
