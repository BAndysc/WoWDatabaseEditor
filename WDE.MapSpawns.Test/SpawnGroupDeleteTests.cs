using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Tasks;
using WDE.MapSpawns.Models.Solution;
using WDE.MapSpawns.Models.SpawnGroups;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Test;

/// <summary>
/// DeleteGroup semantics: with the generic spawn_group table editors removed on cmangos, the 3D
/// editor is the only spawn-group editor, so it must cover the full lifecycle - including deletion.
/// A deleted saved group empties every table on Save (no inserts), reverse links from other groups
/// are cleaned up, a never-saved group is just forgotten, and a pending-deleted id is not handed
/// out again by CreateGroup before the save applies the deletes.
/// </summary>
public class SpawnGroupDeleteTests
{
    private IDatabaseProvider databaseProvider = null!;
    private IMySqlExecutor mySqlExecutor = null!;
    private IQueryGenerator<ISpawnGroupTemplate> templateGen = null!;
    private IQueryGenerator<ISpawnGroupSpawn> spawnGen = null!;
    private IQueryGenerator<ISpawnGroupFormation> formationGen = null!;
    private IQueryGenerator<ISpawnGroupRandomEntry> randomEntryGen = null!;
    private IQueryGenerator<ISpawnGroupLinkedGroup> linkedGroupGen = null!;
    private IQueryGenerator<ISpawnGroupSquadMember> squadGen = null!;
    private SpawnGroupEditorService service = null!;
    private List<SpawnGroupsSolutionItem> publishedItems = null!;

    private static readonly SpawnGroupMember Member100 = new(true, 100);
    private static readonly SpawnGroupMember Member200 = new(true, 200);

    private IQueryGenerator<T> Generator<T>(string table) where T : class
    {
        var gen = Substitute.For<IQueryGenerator<T>>();
        gen.TableName.Returns(DatabaseTable.WorldTable(table));
        gen.TryDelete(Arg.Any<T>()).Returns(Queries.Raw(DataDatabaseType.World, $"-- delete {table}"));
        gen.TryDeleteAll(Arg.Any<T>()).Returns(Queries.Raw(DataDatabaseType.World, $"-- delete all {table}"));
        gen.TryInsert(Arg.Any<T>()).Returns(Queries.Raw(DataDatabaseType.World, $"-- insert {table}"));
        gen.TryBulkInsert(Arg.Any<IReadOnlyCollection<T>>()).Returns(Queries.Raw(DataDatabaseType.World, $"-- bulk insert {table}"));
        return gen;
    }

    [SetUp]
    public async Task SetUp()
    {
        databaseProvider = Substitute.For<IDatabaseProvider>();
        // group 1 "Alpha" (member 100) is linked FROM group 2 "Beta" (member 200)
        databaseProvider.GetSpawnGroupTemplatesAsync().Returns(new List<ISpawnGroupTemplate>
        {
            new AbstractSpawnGroupTemplate { Id = 1, Name = "Alpha", Type = SpawnGroupTemplateType.Creature },
            new AbstractSpawnGroupTemplate { Id = 2, Name = "Beta", Type = SpawnGroupTemplateType.Creature },
        });
        databaseProvider.GetSpawnGroupSpawnsAsync().Returns(new List<ISpawnGroupSpawn>
        {
            new AbstractSpawnGroupSpawn { TemplateId = 1, Guid = 100, Type = SpawnGroupTemplateType.Creature },
            new AbstractSpawnGroupSpawn { TemplateId = 2, Guid = 200, Type = SpawnGroupTemplateType.Creature },
        });
        databaseProvider.GetSpawnGroupLinkedGroupsAsync().Returns(new List<ISpawnGroupLinkedGroup>
        {
            new AbstractSpawnGroupLinkedGroup { GroupId = 2, LinkedGroupId = 1 },
        });

        mySqlExecutor = Substitute.For<IMySqlExecutor>();
        var mainThread = Substitute.For<IMainThread>();
        mainThread.Schedule(Arg.Any<Func<Task<bool>>>()).Returns(ci => ci.Arg<Func<Task<bool>>>()());

        var eventAggregator = new EventAggregator();
        publishedItems = new List<SpawnGroupsSolutionItem>();
        eventAggregator.GetEvent<SpawnGroupsSavedEvent>().Subscribe(items => publishedItems.AddRange(items));

        templateGen = Generator<ISpawnGroupTemplate>("spawn_group");
        spawnGen = Generator<ISpawnGroupSpawn>("spawn_group_spawn");
        formationGen = Generator<ISpawnGroupFormation>("spawn_group_formation");
        randomEntryGen = Generator<ISpawnGroupRandomEntry>("spawn_group_entry");
        linkedGroupGen = Generator<ISpawnGroupLinkedGroup>("spawn_group_linked_group");
        squadGen = Generator<ISpawnGroupSquadMember>("spawn_group_squad");

        var schemaInfo = Substitute.For<ISpawnGroupSchemaInfoProvider>();
        schemaInfo.SupportsFullSpawnGroupRow.Returns(true);

        service = new SpawnGroupEditorService(databaseProvider, mySqlExecutor, mainThread, eventAggregator,
            templateGen, spawnGen, formationGen, randomEntryGen, linkedGroupGen, squadGen,
            new[] { schemaInfo });

        await service.LoadForMap(0);
        service.PumpPendingLoads();
    }

    [Test]
    public void DeleteSavedGroup_RemovesItFromLiveState_AndCleansReverseLinks()
    {
        Assert.AreEqual(1u, service.GroupOf(Member100));
        CollectionAssert.Contains(service.GetDetails(2)!.LinkedGroups, 1u);

        service.DeleteGroup(1);

        Assert.IsFalse(service.GroupNames.ContainsKey(1));
        Assert.IsNull(service.GroupOf(Member100));
        Assert.IsNull(service.GetDetails(1));
        CollectionAssert.DoesNotContain(service.GetDetails(2)!.LinkedGroups, 1u);
        Assert.IsTrue(service.AnyDirty);
    }

    [Test]
    public async Task Save_AfterDelete_DeletesEveryTable_InsertsNothing()
    {
        service.DeleteGroup(1);
        await service.Save();

        templateGen.Received().TryDelete(Arg.Is<ISpawnGroupTemplate>(t => t.Id == 1));
        spawnGen.Received().TryDeleteAll(Arg.Is<ISpawnGroupSpawn>(s => s.TemplateId == 1));
        formationGen.Received().TryDeleteAll(Arg.Is<ISpawnGroupFormation>(f => f.Id == 1));
        randomEntryGen.Received().TryDeleteAll(Arg.Is<ISpawnGroupRandomEntry>(e => e.GroupId == 1));
        linkedGroupGen.Received().TryDeleteAll(Arg.Is<ISpawnGroupLinkedGroup>(l => l.GroupId == 1));
        squadGen.Received().TryDeleteAll(Arg.Is<ISpawnGroupSquadMember>(s => s.GroupId == 1));

        templateGen.DidNotReceive().TryInsert(Arg.Is<ISpawnGroupTemplate>(t => t.Id == 1));
        spawnGen.DidNotReceive().TryBulkInsert(
            Arg.Is<IReadOnlyCollection<ISpawnGroupSpawn>>(rows => rows.Any(r => r.TemplateId == 1)));

        await mySqlExecutor.Received(1).ExecuteSql(Arg.Any<IQuery>());
        // the deleted group's key-only item still goes to the session - its SQL provider re-reads
        // the DB, finds no template and exports just the DELETEs (group 2 saves too: link cleanup)
        CollectionAssert.AreEquivalent(new uint[] { 1, 2 }, publishedItems.Select(i => i.GroupId));
        Assert.IsFalse(service.AnyDirty);

        // deleting it again is a no-op
        service.DeleteGroup(1);
        Assert.IsFalse(service.AnyDirty);
    }

    [Test]
    public async Task DeleteUnsavedGroup_IsForgotten_NothingHitsTheDatabase()
    {
        var id = service.CreateGroup("temp", new[] { new SpawnGroupMember(true, 300) });
        service.DeleteGroup(id);

        Assert.IsFalse(service.GroupNames.ContainsKey(id));
        Assert.IsFalse(service.AnyDirty);

        await service.Save();
        await mySqlExecutor.DidNotReceive().ExecuteSql(Arg.Any<IQuery>());
    }

    [Test]
    public void PendingDeletedId_IsNotHandedOutAgain()
    {
        service.DeleteGroup(2); // frees the highest id, but only in memory until Save
        var id = service.CreateGroup("new", new[] { new SpawnGroupMember(true, 300) });
        Assert.AreEqual(3u, id);
    }
}
