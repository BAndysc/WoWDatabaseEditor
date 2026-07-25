using System.Numerics;
using WDE.Common.Database;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace DatabaseTester;

public class QueryGeneratorTester
{
    private readonly IQueryGenerator<CreatureSpawnModelEssentials> creature;
    private readonly IQueryGenerator<GameObjectSpawnModelEssentials> gameobject;
    private readonly IQueryGenerator<ISpawnGroupTemplate> spawnGroupTemplate;
    private readonly IQueryGenerator<ISpawnGroupSpawn> spawnGroupSpawn;
    private readonly IQueryGenerator<QuestChainDiff> questChain;
    private readonly IQueryGenerator<CreatureDiff> creatureDiff;
    private readonly IQueryGenerator<GameObjectDiff> gameobjectDiff;
    private readonly IQueryGenerator<ICreatureText> creatureText;
    private readonly IQueryGenerator<IGossipMenuOption> gossipMenuOption;
    private readonly IQueryGenerator<IGossipMenuLine> gossipMenu;
    private readonly IQueryGenerator<CreatureGossipUpdate> creatureTemplateDiff;
    private readonly IQueryGenerator<IPointOfInterest> pointOfInterest;
    private readonly IQueryGenerator<INpcTextFull> npcTextInsert;
    private readonly IQueryGenerator<INpcText> npcTextDelete;
    private readonly IQueryGenerator<IWaypointData> waypointData;
    private readonly IQueryGenerator<ISmartScriptWaypoint> smartScriptWaypoint;
    private readonly IQueryGenerator<IScriptWaypoint> scriptWaypoint;
    private readonly IQueryGenerator<IMangosWaypoint> mangosWaypoint;
    private readonly IQueryGenerator<IMangosCreatureMovement> mangosCreatureMovement;
    private readonly IQueryGenerator<IMangosCreatureMovementTemplate> mangosCreatureMovementTemplate;
    private readonly IQueryGenerator<IWaypointPathHeader> waypointPathHeader;
    private readonly IQueryGenerator<IMangosWaypointsPathName> mangosPathName;

    public QueryGeneratorTester(IQueryGenerator<CreatureSpawnModelEssentials> creature,
        IQueryGenerator<GameObjectSpawnModelEssentials> gameobject,
        IQueryGenerator<ISpawnGroupTemplate> spawnGroupTemplate,
        IQueryGenerator<ISpawnGroupSpawn> spawnGroupSpawn,
        IQueryGenerator<QuestChainDiff> questChain,
        IQueryGenerator<CreatureDiff> creatureDiff,
        IQueryGenerator<GameObjectDiff> gameobjectDiff,
        IQueryGenerator<ICreatureText> creatureText,
        IQueryGenerator<IGossipMenuOption> gossipMenuOption,
        IQueryGenerator<IGossipMenuLine> gossipMenu,
        IQueryGenerator<CreatureGossipUpdate> creatureTemplateDiff,
        IQueryGenerator<IPointOfInterest> pointOfInterest,
        IQueryGenerator<INpcTextFull> npcTextInsert,
        IQueryGenerator<INpcText> npcTextDelete,
        IQueryGenerator<IWaypointData> waypointData,
        IQueryGenerator<ISmartScriptWaypoint> smartScriptWaypoint,
        IQueryGenerator<IScriptWaypoint> scriptWaypoint,
        IQueryGenerator<IMangosWaypoint> mangosWaypoint,
        IQueryGenerator<IMangosCreatureMovement> mangosCreatureMovement,
        IQueryGenerator<IMangosCreatureMovementTemplate> mangosCreatureMovementTemplate,
        IQueryGenerator<IWaypointPathHeader> waypointPathHeader,
        IQueryGenerator<IMangosWaypointsPathName> mangosPathName)
    {
        this.creature = creature;
        this.gameobject = gameobject;
        this.spawnGroupTemplate = spawnGroupTemplate;
        this.spawnGroupSpawn = spawnGroupSpawn;
        this.questChain = questChain;
        this.creatureDiff = creatureDiff;
        this.gameobjectDiff = gameobjectDiff;
        this.creatureText = creatureText;
        this.gossipMenuOption = gossipMenuOption;
        this.gossipMenu = gossipMenu;
        this.creatureTemplateDiff = creatureTemplateDiff;
        this.pointOfInterest = pointOfInterest;
        this.npcTextInsert = npcTextInsert;
        this.npcTextDelete = npcTextDelete;
        this.waypointData = waypointData;
        this.smartScriptWaypoint = smartScriptWaypoint;
        this.scriptWaypoint = scriptWaypoint;
        this.mangosWaypoint = mangosWaypoint;
        this.mangosCreatureMovement = mangosCreatureMovement;
        this.mangosCreatureMovementTemplate = mangosCreatureMovementTemplate;
        this.waypointPathHeader = waypointPathHeader;
        this.mangosPathName = mangosPathName;
    }

    public IEnumerable<DatabaseTable?> Tables()
    {
        yield return creature.TableName;
        yield return gameobject.TableName;
        yield return spawnGroupTemplate.TableName;
        yield return spawnGroupSpawn.TableName;
        yield return creatureText.TableName;
        yield return gossipMenuOption.TableName;
        yield return gossipMenu.TableName;
        yield return creatureTemplateDiff.TableName;
        yield return pointOfInterest.TableName;
        yield return npcTextInsert.TableName;
        yield return waypointData.TableName;
        yield return smartScriptWaypoint.TableName;
        yield return scriptWaypoint.TableName;
        yield return mangosWaypoint.TableName;
        yield return mangosCreatureMovement.TableName;
        yield return mangosCreatureMovementTemplate.TableName;
        yield return waypointPathHeader.TableName;
        yield return mangosPathName.TableName;
    }

    public IEnumerable<Func<IQuery>> Generate()
    {
        yield return () => gossipMenuOption.Insert(new AbstractGossipMenuOption()
        {
            MenuId = 1,
        });
        yield return () => gossipMenuOption.Delete(new AbstractGossipMenuOption()
        {
            MenuId = 1
        });
        yield return () => gossipMenu.Insert(new AbstractGossipMenuLine()
        {
            MenuId = 1,
        });
        yield return () => gossipMenu.Delete(new AbstractGossipMenuLine()
        {
            MenuId = 1
        });
        yield return () => creature.Insert(new CreatureSpawnModelEssentials()
        {
            Guid = 0xFFFFFF - 1,
            Entry = 1
        });
        yield return () => creatureDiff.Update(new CreatureDiff()
        {
            Guid = 0xFFFFFF - 1,
            Position = Vector3.Zero,
            Orientation = 0
        });
        yield return () => gameobject.Insert(new GameObjectSpawnModelEssentials()
        {
            Guid = 0xFFFFFF - 1,
            Entry = 29
        });
        yield return () => creature.Delete(new CreatureSpawnModelEssentials()
        {
            Guid = 0xFFFFFF - 1
        });
        yield return () => gameobjectDiff.Update(new GameObjectDiff()
        {
            Guid = 0xFFFFFF - 1,
            Position = Vector3.Zero,
            Orientation = 0,
            Rotation = Quaternion.Identity
        });
        yield return () => gameobject.Delete(new GameObjectSpawnModelEssentials()
        {
            Guid = 0xFFFFFF - 1
        });
        yield return () => spawnGroupTemplate.Insert(new AbstractSpawnGroupTemplate()
        {
            Id = int.MaxValue - 1
        });
        yield return () => spawnGroupSpawn.Insert(new AbstractSpawnGroupSpawn()
        {
            TemplateId = int.MaxValue - 1,
            Guid = int.MaxValue - 1
        });
        yield return () => spawnGroupTemplate.Delete(new AbstractSpawnGroupTemplate()
        {
            Id = int.MaxValue - 1
        });
        yield return () => spawnGroupSpawn.Delete(new AbstractSpawnGroupSpawn()
        {
            TemplateId = int.MaxValue - 1,
            Guid = int.MaxValue - 1
        });
        yield return () => questChain.Update(new QuestChainDiff()
        {
            Id = int.MaxValue - 1,
            BreadcrumbQuestId = 1,
            ExclusiveGroup = -2,
            NextQuestId = 3,
            PrevQuestId = -1
        });
        yield return () => creatureText.Insert(new AbstractCreatureText()
        {
            CreatureId = 1,
            GroupId = byte.MaxValue - 1,
            Id = byte.MaxValue - 1,
            Text = "abc"
        });
        yield return () => creatureText.Delete(new AbstractCreatureText()
        {
            CreatureId = 1
        });
        yield return () => creatureTemplateDiff.Update(new CreatureGossipUpdate()
        {
            Entry = 1,
            GossipMenuId = 1
        });
        yield return () => pointOfInterest.Insert(new AbstractPointOfInterest()
        {
            Id = 0xFFFFFF - 1
        });
        yield return () => pointOfInterest.Delete(new AbstractPointOfInterest()
        {
            Id = 0xFFFFFF - 1
        });
        yield return () => npcTextInsert.Insert(new AbstractNpcTextFull()
        {
            Id = 0xFFFFFF - 10
        });
        yield return () => npcTextDelete.Delete(new AbstractNpcText()
        {
            Id = 0xFFFFFF - 10
        });
    }

    // ---- waypoint exporters -----------------------------------------------------------------------
    // Gated separately from Generate(): waypoint tables vary not only per core family but per
    // DATABASE within a family (current TC removed `waypoints`/`script_waypoint`, older TDB forks
    // still have them), so the runner checks the table exists before executing. A missing per-core
    // provider surfaces as TableNotSupportedException, the runner's existing skip path.

    private const uint TestPathId = 0xFFFFF0;

    /// <summary>(gate table, query) pairs: the runner skips entries whose table is absent in the
    /// tested database. Entries per family: insert a far-key point, path-level rows, cleanup.</summary>
    public IEnumerable<(DatabaseTable? table, Func<IQuery> query)> GenerateGated()
    {
        var point = new TestWaypoint();

        // waypoint_data family (waypoint_data on Wrath/Azeroth/Cata, waypoint_path_node on master)
        yield return (waypointData.TableName, Gated(() => waypointData.TryInsert(point), "waypoint_data"));
        // master's per-path metadata row (gate on the node table: cmangos has its own unrelated
        // waypoint_path and must not run this - its provider is absent, so it skips either way)
        yield return (waypointData.TableName, Gated(() => waypointPathHeader.TryUpdate(new WaypointPathHeader
        {
            PathId = TestPathId, MoveType = 1, Flags = 0, Velocity = null, Comment = "wde test",
        }), "waypoint_path"));
        // cleanup: DeleteAll where it exists (master: drops the waypoint_path parent too), else Delete
        yield return (waypointData.TableName, Gated(() => waypointData.TryDeleteAll(point) ?? waypointData.TryDelete(point), "waypoint_data"));

        yield return (smartScriptWaypoint.TableName, Gated(() => smartScriptWaypoint.TryInsert(point), "waypoints"));
        yield return (smartScriptWaypoint.TableName, Gated(() => smartScriptWaypoint.TryDelete(point), "waypoints"));

        yield return (scriptWaypoint.TableName, Gated(() => scriptWaypoint.TryInsert(point), "script_waypoint"));
        yield return (scriptWaypoint.TableName, Gated(() => scriptWaypoint.TryDelete(point), "script_waypoint"));

        yield return (mangosWaypoint.TableName, Gated(() => mangosWaypoint.TryInsert(point), "waypoint_path"));
        yield return (mangosWaypoint.TableName, Gated(() => mangosWaypoint.TryDelete(point), "waypoint_path"));

        yield return (mangosPathName.TableName ?? DatabaseTable.WorldTable("waypoint_path_name"),
            Gated(() => mangosPathName.TryUpdate(new TestPathName(TestPathId, "wde test")), "waypoint_path_name"));
        yield return (mangosPathName.TableName ?? DatabaseTable.WorldTable("waypoint_path_name"),
            Gated(() => mangosPathName.TryUpdate(new TestPathName(TestPathId, "")), "waypoint_path_name"));

        yield return (mangosCreatureMovement.TableName, Gated(() => mangosCreatureMovement.TryInsert(point), "creature_movement"));
        yield return (mangosCreatureMovement.TableName, Gated(() => mangosCreatureMovement.TryDelete(point), "creature_movement"));

        yield return (mangosCreatureMovementTemplate.TableName, Gated(() => mangosCreatureMovementTemplate.TryInsert(point), "creature_movement_template"));
        yield return (mangosCreatureMovementTemplate.TableName, Gated(() => mangosCreatureMovementTemplate.TryDelete(point), "creature_movement_template"));
    }

    // a provider missing on this core -> the runner's TableNotSupportedException skip path
    private static Func<IQuery> Gated(Func<IQuery?> factory, string fallbackTableName) =>
        () => factory() ?? throw new TableNotSupportedException(DatabaseTable.WorldTable(fallbackTableName));

    /// <summary>One far-key waypoint usable as every point contract at once.</summary>
    private class TestWaypoint : IWaypointData, ISmartScriptWaypoint, IScriptWaypoint, IMangosWaypoint,
        IMangosCreatureMovement, IMangosCreatureMovementTemplate
    {
        public uint PathId => TestPathId;
        public uint Guid => TestPathId;         // creature_movement's key
        public uint Entry => TestPathId;        // creature_movement_template's primary key
        public uint PointId => 1;
        public float X => 1;
        public float Y => 2;
        public float Z => 3;
        public uint Delay => 1000;
        public uint WaitTime => 1000;
        public int MoveType => 0;
        public int Action => 0;
        public byte ActionChance => 100;
        public uint ScriptId => 0;
        public bool? SmoothTransition => null;
        public string? Comment => "wde test";
        public float? Velocity => null;
        float? IWaypointData.Orientation => null;
        float? ISmartScriptWaypoint.Orientation => null;
        float IMangosWaypoint.Orientation => 0;
        float IMangosCreatureMovement.Orientation => 0;
        float IMangosCreatureMovementTemplate.Orientation => 0;
        public UniversalWaypoint ToUniversal() => new() { PathId = PathId, PointId = PointId, X = X, Y = Y, Z = Z };
    }

    private class TestPathName : IMangosWaypointsPathName
    {
        public TestPathName(uint pathId, string name)
        {
            PathId = pathId;
            Name = name;
        }

        public uint PathId { get; }
        public string Name { get; }
    }
}