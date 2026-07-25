using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Tasks;
using WDE.MapSpawns.Models.Pools;
using WDE.MapSpawns.Models.Solution;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Test;

/// <summary>
/// PoolEditorService semantics: exclusive membership (guid and entry stealing), pool_pool cycle
/// rejection, DeletePool (children become top-level, save empties every table, unsaved pools are
/// forgotten, pending-deleted ids stay reserved) and the nesting rewrite (children's rows come from
/// the CHILDREN's current details, never clobbered by the mother's save). Plus the key-only
/// solution item round-trip. Mirrors SpawnGroupDeleteTests - the 3D editor is the only pool editor
/// on cmangos, so it must cover the whole lifecycle.
/// </summary>
public class PoolEditorTests
{
    private IDatabaseProvider databaseProvider = null!;
    private IMySqlExecutor mySqlExecutor = null!;
    private IQueryGenerator<IPoolTemplate> templateGen = null!;
    private IQueryGenerator<IPoolCreatureMember> creatureGen = null!;
    private IQueryGenerator<IPoolGameObjectMember> gameObjectGen = null!;
    private IQueryGenerator<IPoolCreatureEntryMember> creatureEntryGen = null!;
    private IQueryGenerator<IPoolGameObjectEntryMember> gameObjectEntryGen = null!;
    private IQueryGenerator<IPoolNesting> nestingGen = null!;
    private PoolEditorService service = null!;
    private List<PoolsSolutionItem> publishedItems = null!;

    private static readonly PoolMember Creature100 = new(true, 100);
    private static readonly PoolMember Creature200 = new(true, 200);

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
        // pool 1 "Alpha" (creature 100) is the MOTHER of pool 2 "Beta" (creature 200);
        // pool 2 also pools entry 5000 entry-wide
        databaseProvider.GetPoolTemplatesAsync().Returns(new List<IPoolTemplate>
        {
            new AbstractPoolTemplate { Entry = 1, Description = "Alpha", MaxLimit = 1 },
            new AbstractPoolTemplate { Entry = 2, Description = "Beta", MaxLimit = 1 },
        });
        databaseProvider.GetPoolCreaturesAsync().Returns(new List<IPoolCreatureMember>
        {
            new AbstractPoolCreatureMember { Guid = 100, PoolEntry = 1, Chance = 25 },
            new AbstractPoolCreatureMember { Guid = 200, PoolEntry = 2, Chance = 0 },
        });
        databaseProvider.GetPoolGameObjectsAsync().Returns(new List<IPoolGameObjectMember>());
        databaseProvider.GetPoolCreatureEntryPoolsAsync().Returns(new List<IPoolCreatureEntryMember>
        {
            new AbstractPoolCreatureEntryMember { Entry = 5000, PoolEntry = 2, Chance = 0 },
        });
        databaseProvider.GetPoolGameObjectEntryPoolsAsync().Returns(new List<IPoolGameObjectEntryMember>());
        databaseProvider.GetPoolNestingsAsync().Returns(new List<IPoolNesting>
        {
            new AbstractPoolNesting { PoolId = 2, MotherPool = 1, Chance = 50 },
        });

        mySqlExecutor = Substitute.For<IMySqlExecutor>();
        var mainThread = Substitute.For<IMainThread>();
        mainThread.Schedule(Arg.Any<Func<Task<bool>>>()).Returns(ci => ci.Arg<Func<Task<bool>>>()());

        var eventAggregator = new EventAggregator();
        publishedItems = new List<PoolsSolutionItem>();
        eventAggregator.GetEvent<PoolsSavedEvent>().Subscribe(items => publishedItems.AddRange(items));

        templateGen = Generator<IPoolTemplate>("pool_template");
        creatureGen = Generator<IPoolCreatureMember>("pool_creature");
        gameObjectGen = Generator<IPoolGameObjectMember>("pool_gameobject");
        creatureEntryGen = Generator<IPoolCreatureEntryMember>("pool_creature_template");
        gameObjectEntryGen = Generator<IPoolGameObjectEntryMember>("pool_gameobject_template");
        nestingGen = Generator<IPoolNesting>("pool_pool");

        var schemaInfo = Substitute.For<IPoolSchemaInfoProvider>();
        schemaInfo.ExplicitChanceRequiresMaxLimitOne.Returns(true);

        service = new PoolEditorService(databaseProvider, mySqlExecutor, mainThread, eventAggregator,
            templateGen, creatureGen, gameObjectGen, creatureEntryGen, gameObjectEntryGen, nestingGen,
            new[] { schemaInfo });

        await service.LoadForMap(0);
        service.PumpPendingLoads();
    }

    [Test]
    public void Load_ExposesMembershipEntryPoolingAndNesting()
    {
        Assert.IsTrue(service.IsSupported);
        Assert.IsTrue(service.SupportsNestedPools);
        Assert.IsTrue(service.SupportsEntryPooling);
        Assert.IsTrue(service.ExplicitChanceRequiresMaxLimitOne);

        Assert.AreEqual(1u, service.PoolOf(Creature100));
        Assert.AreEqual(2u, service.PoolOf(Creature200));
        Assert.AreEqual(2u, service.PoolOfEntry(new PoolEntryKey(true, 5000)));
        Assert.AreEqual(1u, service.MotherOf(2));
        Assert.IsNull(service.MotherOf(1));
        Assert.AreEqual(25f, service.GetDetails(1)!.DataOf(Creature100).Chance);

        var children = new List<uint>();
        service.CollectChildren(1, children);
        CollectionAssert.AreEqual(new uint[] { 2 }, children);
        Assert.IsFalse(service.AnyDirty);
    }

    [Test]
    public void AddToPool_StealsTheMemberFromItsPreviousPool()
    {
        service.AddToPool(2, new[] { Creature100 });

        Assert.AreEqual(2u, service.PoolOf(Creature100));
        var members = new List<PoolMember>();
        service.CollectMembers(1, members);
        CollectionAssert.IsEmpty(members);
        // the stolen member's per-pool data moved out of the old pool
        Assert.AreEqual(0f, service.GetDetails(1)!.DataOf(Creature100).Chance);
        Assert.IsTrue(service.AnyDirty);
    }

    [Test]
    public void AddEntryMember_StealsTheEntryFromItsPreviousPool()
    {
        var entry = new PoolEntryKey(true, 5000);
        service.AddEntryMember(1, entry);

        Assert.AreEqual(1u, service.PoolOfEntry(entry));
        Assert.IsFalse(service.GetDetails(2)!.EntryMembers.ContainsKey(entry));
        Assert.IsTrue(service.GetDetails(1)!.EntryMembers.ContainsKey(entry));
    }

    [Test]
    public void TrySetMotherPool_RejectsSelfAndCycles()
    {
        Assert.IsFalse(service.TrySetMotherPool(1, 1)); // self
        Assert.IsFalse(service.TrySetMotherPool(1, 2)); // 2's mother is 1 - would cycle
        Assert.IsNull(service.MotherOf(1));

        // legal re-parenting still works: a new pool can go under 2
        var id = service.CreatePool("gamma", Array.Empty<PoolMember>());
        Assert.IsTrue(service.TrySetMotherPool(id, 2));
        Assert.AreEqual(2u, service.MotherOf(id));
        // and now 1 <- 2 <- gamma, so gamma can't mother 1
        Assert.IsFalse(service.TrySetMotherPool(1, id));
    }

    [Test]
    public void DeletePool_DetachesMembersAndChildren()
    {
        service.DeletePool(1);

        Assert.IsFalse(service.PoolNames.ContainsKey(1));
        Assert.IsNull(service.PoolOf(Creature100));
        Assert.IsNull(service.GetDetails(1));
        // child 2 became top-level and is dirty (its pool_pool row must go away on ITS rewrite)
        Assert.IsNull(service.MotherOf(2));
        Assert.IsTrue(service.AnyDirty);
    }

    [Test]
    public async Task Save_AfterDelete_DeletesEveryTable_InsertsNothing()
    {
        service.DeletePool(1);
        await service.Save();

        templateGen.Received().TryDelete(Arg.Is<IPoolTemplate>(t => t.Entry == 1));
        creatureGen.Received().TryDeleteAll(Arg.Is<IPoolCreatureMember>(c => c.PoolEntry == 1));
        gameObjectGen.Received().TryDeleteAll(Arg.Is<IPoolGameObjectMember>(g => g.PoolEntry == 1));
        creatureEntryGen.Received().TryDeleteAll(Arg.Is<IPoolCreatureEntryMember>(c => c.PoolEntry == 1));
        gameObjectEntryGen.Received().TryDeleteAll(Arg.Is<IPoolGameObjectEntryMember>(g => g.PoolEntry == 1));
        nestingGen.Received().TryDeleteAll(Arg.Is<IPoolNesting>(n => n.PoolId == 1));

        templateGen.DidNotReceive().TryInsert(Arg.Is<IPoolTemplate>(t => t.Entry == 1));
        creatureGen.DidNotReceive().TryBulkInsert(
            Arg.Is<IReadOnlyCollection<IPoolCreatureMember>>(rows => rows.Any(r => r.PoolEntry == 1)));

        await mySqlExecutor.Received(1).ExecuteSql(Arg.Any<IQuery>());
        // pool 2 saves too (it lost its mother) and its rewrite must NOT reinsert the mother row
        nestingGen.DidNotReceive().TryBulkInsert(
            Arg.Is<IReadOnlyCollection<IPoolNesting>>(rows => rows.Any(r => r.PoolId == 2)));
        CollectionAssert.AreEquivalent(new uint[] { 1, 2 }, publishedItems.Select(i => i.PoolId));
        Assert.IsFalse(service.AnyDirty);

        // deleting it again is a no-op
        service.DeletePool(1);
        Assert.IsFalse(service.AnyDirty);
    }

    [Test]
    public async Task Save_OfTheMother_WritesChildNestingRowsFromTheChildsCurrentDetails()
    {
        // edit the CHILD's chance, then dirty and save only the MOTHER: the mother's nesting rewrite
        // (DeleteAll covers both directions) must reinsert the child's row with the CHILD's edit
        service.GetDetails(2)!.MotherChance = 75;
        service.NotifyDetailsChanged(2);

        service.GetDetails(1)!.MaxLimit = 3;
        service.NotifyDetailsChanged(1);

        await service.Save();

        nestingGen.Received().TryBulkInsert(Arg.Is<IReadOnlyCollection<IPoolNesting>>(rows =>
            rows.Any(r => r.PoolId == 2 && r.MotherPool == 1 && Math.Abs(r.Chance - 75) < 0.001f)));
        // members keep their per-member data through the rewrite
        creatureGen.Received().TryBulkInsert(Arg.Is<IReadOnlyCollection<IPoolCreatureMember>>(rows =>
            rows.Any(r => r.Guid == 100 && r.PoolEntry == 1 && Math.Abs(r.Chance - 25) < 0.001f)));
        creatureEntryGen.Received().TryBulkInsert(Arg.Is<IReadOnlyCollection<IPoolCreatureEntryMember>>(rows =>
            rows.Any(r => r.Entry == 5000 && r.PoolEntry == 2)));
    }

    [Test]
    public async Task DeleteUnsavedPool_IsForgotten_NothingHitsTheDatabase()
    {
        var id = service.CreatePool("temp", new[] { new PoolMember(true, 300) });
        service.DeletePool(id);

        Assert.IsFalse(service.PoolNames.ContainsKey(id));
        Assert.IsFalse(service.AnyDirty);

        await service.Save();
        await mySqlExecutor.DidNotReceive().ExecuteSql(Arg.Any<IQuery>());
    }

    [Test]
    public void PendingDeletedId_IsNotHandedOutAgain()
    {
        service.DeletePool(2); // frees the highest id, but only in memory until Save
        var id = service.CreatePool("new", new[] { new PoolMember(true, 300) });
        Assert.AreEqual(3u, id);
    }

    [Test]
    public void SolutionItem_RoundTrip_StoresOnlyThePoolId()
    {
        var providers = new PoolsSolutionItemProviders(databaseProvider,
            templateGen, creatureGen, gameObjectGen, creatureEntryGen, gameObjectEntryGen, nestingGen);

        var item = new PoolsSolutionItem { PoolId = 7 };
        var serialized = providers.Serialize(item, false);
        Assert.IsNotNull(serialized);
        Assert.IsTrue(string.IsNullOrEmpty(serialized!.Comment)); // key-only: no row data persisted
        Assert.IsTrue(string.IsNullOrEmpty(serialized.StringValue));

        Assert.IsTrue(providers.TryDeserialize(serialized, out var deserialized));
        var restored = (PoolsSolutionItem)deserialized!;
        Assert.AreEqual(7u, restored.PoolId);
        Assert.IsTrue(item.Equals(restored));
        Assert.IsTrue(item.Equals(item.Clone()));
        Assert.IsFalse(item.Equals(new PoolsSolutionItem { PoolId = 8 }));
        Assert.IsFalse(item.Equals(new SpawnGroupsSolutionItem { GroupId = 7 })); // other type, same key
    }
}
