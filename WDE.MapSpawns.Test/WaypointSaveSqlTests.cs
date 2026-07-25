using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Database;
using WDE.MapSpawns.Models.Waypoints;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Generators.Waypoints;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Test;

/// <summary>
/// The path-LEVEL rows some waypoint storages carry next to their points: TC master's
/// waypoint_path metadata (MoveType/Flags/Velocity/Comment) and cmangos' waypoint_path_name.
/// The save SQL must rewrite them with the points - and must NOT resurrect them when the whole
/// path is being removed (DeleteAll drops them).
/// </summary>
public class WaypointSaveSqlTests
{
    private IQueryGenerator<T> Generator<T>(string table) where T : class
    {
        var gen = Substitute.For<IQueryGenerator<T>>();
        gen.TableName.Returns(DatabaseTable.WorldTable(table));
        gen.TryDelete(Arg.Any<T>()).Returns(Queries.Raw(DataDatabaseType.World, $"-- delete {table}"));
        gen.TryDeleteAll(Arg.Any<T>()).Returns(Queries.Raw(DataDatabaseType.World, $"-- delete all {table}"));
        gen.TryBulkInsert(Arg.Any<IReadOnlyCollection<T>>()).Returns(Queries.Raw(DataDatabaseType.World, $"-- bulk insert {table}"));
        return gen;
    }

    private IQueryGenerator<IWaypointData> waypointDataGen = null!;
    private IQueryGenerator<ISmartScriptWaypoint> smartGen = null!;
    private IQueryGenerator<IScriptWaypoint> scriptGen = null!;
    private IQueryGenerator<IMangosWaypoint> mangosGen = null!;
    private IQueryGenerator<IMangosCreatureMovement> movementGen = null!;
    private IQueryGenerator<IMangosCreatureMovementTemplate> movementTemplateGen = null!;
    private IQueryGenerator<IWaypointPathHeader> headerGen = null!;
    private IQueryGenerator<IMangosWaypointsPathName> nameGen = null!;

    [SetUp]
    public void SetUp()
    {
        waypointDataGen = Generator<IWaypointData>("waypoint_path_node");
        smartGen = Generator<ISmartScriptWaypoint>("waypoints");
        scriptGen = Generator<IScriptWaypoint>("script_waypoint");
        mangosGen = Generator<IMangosWaypoint>("waypoint_path");
        movementGen = Generator<IMangosCreatureMovement>("creature_movement");
        movementTemplateGen = Generator<IMangosCreatureMovementTemplate>("creature_movement_template");
        headerGen = Substitute.For<IQueryGenerator<IWaypointPathHeader>>();
        headerGen.TryUpdate(Arg.Any<IWaypointPathHeader>()).Returns(Queries.Raw(DataDatabaseType.World, "-- header upsert"));
        nameGen = Substitute.For<IQueryGenerator<IMangosWaypointsPathName>>();
        nameGen.TryUpdate(Arg.Any<IMangosWaypointsPathName>()).Returns(Queries.Raw(DataDatabaseType.World, "-- name rewrite"));
    }

    private IQuery? Build(WaypointSource source, IReadOnlyList<UniversalWaypoint> points,
        IWaypointPathHeader? header = null, IMangosWaypointsPathName? name = null) =>
        WaypointSaveSql.Build(source, 5, 0, points,
            waypointDataGen, smartGen, scriptGen, mangosGen, movementGen, movementTemplateGen,
            header, headerGen, name, nameGen);

    private static List<UniversalWaypoint> OnePoint => new() { new UniversalWaypoint { PathId = 5, PointId = 1 } };

    [Test]
    public void MasterHeader_IsWrittenAfterThePoints()
    {
        var query = Build(WaypointSource.TrinityWaypointData, OnePoint,
            new WaypointPathHeader { PathId = 5, MoveType = 1 });

        StringAssert.Contains("-- delete waypoint_path_node", query!.QueryString);
        StringAssert.Contains("-- bulk insert waypoint_path_node", query.QueryString);
        StringAssert.Contains("-- header upsert", query.QueryString);
    }

    [Test]
    public void MasterHeader_IsIgnoredForOtherSources()
    {
        var query = Build(WaypointSource.MangosWaypointPath, OnePoint,
            new WaypointPathHeader { PathId = 5 });

        StringAssert.DoesNotContain("-- header upsert", query!.QueryString);
    }

    [Test]
    public void MangosPathName_IsRewrittenWithThePoints()
    {
        var query = Build(WaypointSource.MangosWaypointPath, OnePoint,
            name: new MangosPathNameAdapter(5, "The route"));

        StringAssert.Contains("-- delete waypoint_path", query!.QueryString);
        StringAssert.Contains("-- name rewrite", query.QueryString);
    }

    [Test]
    public void NoExtras_KeepsThePlainDeleteInsertShape()
    {
        var query = Build(WaypointSource.TrinityWaypointData, OnePoint);

        StringAssert.Contains("-- delete waypoint_path_node", query!.QueryString);
        StringAssert.DoesNotContain("-- header upsert", query.QueryString);
        StringAssert.DoesNotContain("-- name rewrite", query.QueryString);
    }

    [Test]
    public void MasterHeaderProvider_EnsuresTheRowThenWritesTheMeta()
    {
        var provider = new TrinityMasterWaypointPathHeaderQueryProvider();
        var sql = provider.Update(new WaypointPathHeader
        {
            PathId = 7, MoveType = 2, Flags = 1, Velocity = null, Comment = "patrol",
        }).QueryString;

        StringAssert.Contains("INSERT IGNORE INTO `waypoint_path`", sql);
        StringAssert.Contains("UPDATE `waypoint_path` SET", sql);
        StringAssert.Contains("`MoveType` = 2", sql);
        StringAssert.Contains("`Flags` = 1", sql);
        StringAssert.Contains("`Velocity` = NULL", sql);
        StringAssert.Contains("patrol", sql);
        StringAssert.Contains("`PathId` = 7", sql);
    }

    [Test]
    public void MangosPathNameProvider_RewritesTheRow_AndAnEmptyNameOnlyDeletes()
    {
        var provider = new MangosWaypointPathNameQueryProvider();

        var withName = provider.Update(new MangosPathNameAdapter(9, "Guard round")).QueryString;
        StringAssert.Contains("DELETE FROM `waypoint_path_name` WHERE `PathId` = 9", withName);
        StringAssert.Contains("INSERT INTO `waypoint_path_name`", withName);
        StringAssert.Contains("Guard round", withName);

        var cleared = provider.Update(new MangosPathNameAdapter(9, "")).QueryString;
        StringAssert.Contains("DELETE FROM `waypoint_path_name` WHERE `PathId` = 9", cleared);
        StringAssert.DoesNotContain("INSERT", cleared);
    }
}
