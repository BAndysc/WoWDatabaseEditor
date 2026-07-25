using NSubstitute;
using NUnit.Framework;
using WDE.Common.Database;
using WDE.MapSpawns.Models.Solution;
using WDE.QueryGenerators.Base;

namespace WDE.MapSpawns.Test;

/// <summary>Round-trips each 3D-edit solution item through its session serializer: what the session
/// file stores must deserialize back to an equivalent item (sessions survive app restarts). The items
/// are KEY-ONLY, so serialization is just the natural key - never any row data.</summary>
public class SolutionItemSerializationTests
{
    private FormationsSolutionItemProviders formationsProviders = null!;
    private SpawnGroupsSolutionItemProviders spawnGroupsProviders = null!;
    private WaypointsSolutionItemProviders waypointsProviders = null!;

    [SetUp]
    public void SetUp()
    {
        formationsProviders = new FormationsSolutionItemProviders(
            Substitute.For<IDatabaseProvider>(),
            Substitute.For<IQueryGenerator<ICreatureFormation>>());
        spawnGroupsProviders = new SpawnGroupsSolutionItemProviders(
            Substitute.For<IDatabaseProvider>(),
            Substitute.For<IQueryGenerator<ISpawnGroupTemplate>>(),
            Substitute.For<IQueryGenerator<ISpawnGroupSpawn>>(),
            Substitute.For<IQueryGenerator<ISpawnGroupFormation>>(),
            Substitute.For<IQueryGenerator<ISpawnGroupRandomEntry>>(),
            Substitute.For<IQueryGenerator<ISpawnGroupLinkedGroup>>(),
            Substitute.For<IQueryGenerator<ISpawnGroupSquadMember>>());
        waypointsProviders = new WaypointsSolutionItemProviders(
            Substitute.For<IDatabaseProvider>(),
            Substitute.For<IQueryGenerator<IWaypointData>>(),
            Substitute.For<IQueryGenerator<ISmartScriptWaypoint>>(),
            Substitute.For<IQueryGenerator<IScriptWaypoint>>(),
            Substitute.For<IQueryGenerator<IMangosWaypoint>>(),
            Substitute.For<IQueryGenerator<IMangosCreatureMovement>>(),
            Substitute.For<IQueryGenerator<IMangosCreatureMovementTemplate>>(),
            Substitute.For<IQueryGenerator<IWaypointPathHeader>>(),
            Substitute.For<IQueryGenerator<IMangosWaypointsPathName>>(),
            System.Array.Empty<IWaypointSchemaInfoProvider>());
    }

    [Test]
    public void Formations_RoundTrip_StoresOnlyTheLeaderGuid()
    {
        var item = new FormationsSolutionItem { LeaderGuid = 10 };

        var serialized = formationsProviders.Serialize(item, false);
        Assert.IsNotNull(serialized);
        Assert.IsTrue(string.IsNullOrEmpty(serialized!.Comment)); // key-only: no row data persisted
        Assert.IsTrue(string.IsNullOrEmpty(serialized.StringValue));

        Assert.IsTrue(formationsProviders.TryDeserialize(serialized, out var deserialized));
        var restored = (FormationsSolutionItem)deserialized!;

        Assert.AreEqual(10u, restored.LeaderGuid);
        Assert.IsTrue(item.Equals(restored)); // same natural key -> same session entry
    }

    [Test]
    public void SpawnGroups_RoundTrip_StoresOnlyTheGroupId()
    {
        var item = new SpawnGroupsSolutionItem { GroupId = 5 };

        var serialized = spawnGroupsProviders.Serialize(item, false);
        Assert.IsNotNull(serialized);
        Assert.IsTrue(string.IsNullOrEmpty(serialized!.Comment)); // key-only: no members persisted
        Assert.IsTrue(string.IsNullOrEmpty(serialized.StringValue));

        Assert.IsTrue(spawnGroupsProviders.TryDeserialize(serialized, out var deserialized));
        var restored = (SpawnGroupsSolutionItem)deserialized!;

        Assert.AreEqual(5u, restored.GroupId);
        Assert.IsTrue(item.Equals(restored));
    }

    [Test]
    public void Waypoints_RoundTrip_StoresOnlyTheReference()
    {
        var item = new WaypointsSolutionItem { Source = 4, Key = 12345 };

        var serialized = waypointsProviders.Serialize(item, false);
        Assert.IsNotNull(serialized);
        // the whole payload is a compact "source:key" - points are never persisted
        Assert.AreEqual("4:12345", serialized!.StringValue);
        Assert.IsTrue(string.IsNullOrEmpty(serialized.Comment));

        Assert.IsTrue(waypointsProviders.TryDeserialize(serialized, out var deserialized));
        var restored = (WaypointsSolutionItem)deserialized!;

        Assert.AreEqual(4, restored.Source);
        Assert.AreEqual(12345u, restored.Key);
        Assert.IsTrue(item.Equals(restored));
    }

    [Test]
    public void Waypoints_CompoundKey_RoundTripsBothKeys()
    {
        // creature_movement_template is keyed by (entry, pathId) -> the second key must survive too
        var item = new WaypointsSolutionItem { Source = 5, Key = 30001, Key2 = 2 };

        var serialized = waypointsProviders.Serialize(item, false);
        Assert.IsNotNull(serialized);
        Assert.AreEqual("5:30001:2", serialized!.StringValue); // "source:key:key2"

        Assert.IsTrue(waypointsProviders.TryDeserialize(serialized, out var deserialized));
        var restored = (WaypointsSolutionItem)deserialized!;

        Assert.AreEqual(5, restored.Source);
        Assert.AreEqual(30001u, restored.Key);
        Assert.AreEqual(2u, restored.Key2);
        Assert.IsTrue(item.Equals(restored));
    }

    [Test]
    public void Waypoints_LegacyTwoPart_DeserializesWithZeroSecondaryKey()
    {
        // session files written before the compound key existed store just "source:key" - they must
        // still deserialize (Key2 defaults to 0)
        var legacy = new AbstractSmartScriptProjectItem { Type = 142, StringValue = "4:12345" };

        Assert.IsTrue(waypointsProviders.TryDeserialize(legacy, out var deserialized));
        var restored = (WaypointsSolutionItem)deserialized!;

        Assert.AreEqual(4, restored.Source);
        Assert.AreEqual(12345u, restored.Key);
        Assert.AreEqual(0u, restored.Key2);
    }

    [Test]
    public void Waypoints_NameUsesThePerCoreTableName()
    {
        // TrinityMaster stores the WaypointData family in waypoint_path, not waypoint_data - the
        // display name must come from the per-core schema provider, not a hardcoded family name
        var schema = Substitute.For<IWaypointSchemaInfoProvider>();
        schema.TableName(WDE.Common.CoreVersion.WaypointTables.WaypointData).Returns("waypoint_path");
        var providers = new WaypointsSolutionItemProviders(
            Substitute.For<IDatabaseProvider>(),
            Substitute.For<IQueryGenerator<IWaypointData>>(),
            Substitute.For<IQueryGenerator<ISmartScriptWaypoint>>(),
            Substitute.For<IQueryGenerator<IScriptWaypoint>>(),
            Substitute.For<IQueryGenerator<IMangosWaypoint>>(),
            Substitute.For<IQueryGenerator<IMangosCreatureMovement>>(),
            Substitute.For<IQueryGenerator<IMangosCreatureMovementTemplate>>(),
            Substitute.For<IQueryGenerator<IWaypointPathHeader>>(),
            Substitute.For<IQueryGenerator<IMangosWaypointsPathName>>(),
            new[] { schema });

        var item = new WaypointsSolutionItem
        {
            Source = (int)WDE.MapSpawns.Models.Waypoints.WaypointSource.TrinityWaypointData,
            Key = 1230
        };

        Assert.AreEqual("Waypoints waypoint_path #1230", providers.GetName(item));
        // and without a provider (headless/unknown core) it falls back to the family name
        Assert.AreEqual("Waypoints waypoint_data #1230", waypointsProviders.GetName(item));
    }

    [Test]
    public void Deserializers_RejectOtherTypes()
    {
        var formations = formationsProviders.Serialize(new FormationsSolutionItem { LeaderGuid = 1 }, false)!;
        var spawnGroups = spawnGroupsProviders.Serialize(new SpawnGroupsSolutionItem { GroupId = 1 }, false)!;
        var waypoints = waypointsProviders.Serialize(new WaypointsSolutionItem { Source = 0, Key = 1 }, false)!;

        Assert.IsFalse(formationsProviders.TryDeserialize(waypoints, out _));
        Assert.IsFalse(formationsProviders.TryDeserialize(spawnGroups, out _));
        Assert.IsFalse(waypointsProviders.TryDeserialize(formations, out _));
        Assert.IsFalse(spawnGroupsProviders.TryDeserialize(formations, out _));
        Assert.IsFalse(spawnGroupsProviders.TryDeserialize(waypoints, out _));
    }
}
