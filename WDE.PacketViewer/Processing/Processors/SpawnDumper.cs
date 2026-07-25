using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Exceptions;
using WDE.Common.Services;
using WDE.Common.Services.IdGenerator;
using WDE.Module.Attributes;
using WDE.PacketViewer.Utils;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors;

[AutoRegister]
public class SpawnDumper : PacketProcessor<bool>, IPacketTextDumper
{
    private readonly IIdGeneratorService idGenerator;
    private readonly ICachedDatabaseProvider cachedDatabaseProvider;
    private readonly IQueryGenerator<CreatureSpawnModelEssentials> creatureQueryProvider;
    private readonly IQueryGenerator<GameObjectSpawnModelEssentials> gameObjectQueryProvider;
    private Dictionary<UniversalGuid, SpawnData> spawnedCreatures = new();
    private Dictionary<UniversalGuid, SpawnData> spawnedGameObjects = new();

    public SpawnDumper(IIdGeneratorService idGenerator,
        ICachedDatabaseProvider cachedDatabaseProvider,
        IQueryGenerator<CreatureSpawnModelEssentials> creatureQueryProvider,
        IQueryGenerator<GameObjectSpawnModelEssentials> gameObjectQueryProvider)
    {
        this.idGenerator = idGenerator;
        this.cachedDatabaseProvider = cachedDatabaseProvider;
        this.creatureQueryProvider = creatureQueryProvider;
        this.gameObjectQueryProvider = gameObjectQueryProvider;
    }

    public async Task<string> Generate()
    {
        var q = Queries.BeginTransaction(DataDatabaseType.World);

        uint creatureStartGuid = 0, gameObjectStartGuid = 0;
        try
        {
            if (spawnedCreatures.Count > 0)
                creatureStartGuid = (uint)await idGenerator.GetNextRange(new CreatureGuidIdType(), spawnedCreatures.Count);
            if (spawnedGameObjects.Count > 0)
                gameObjectStartGuid = (uint)await idGenerator.GetNextRange(new GameObjectGuidIdType(), spawnedGameObjects.Count);
        }
        catch (IdSourceNotConfiguredException)
        {
            throw new UserException("The guid source is not configured. Please set it in the settings.");
        }
        catch (NoMoreIdsException e)
        {
            throw new UserException(e.Message + " Please extend the range in the settings.");
        }

        if (spawnedCreatures.Count > 0)
        {
            var startGuid = creatureStartGuid;
            var spawns = spawnedCreatures.Values.Select((data, index) =>
                new CreatureSpawnModelEssentials()
                {
                    Entry = data.Entry,
                    Guid = (uint)(startGuid + index),
                    Map = (int)data.MapId,
                    PhaseMask = 0,
                    X = data.Position.X,
                    Y = data.Position.Y,
                    Z = data.Position.Z,
                    O = data.Orientation,
                    __comment = cachedDatabaseProvider.GetCachedCreatureTemplate(data.Entry)?.Name
                }).ToList();
            foreach (var spawn in spawns)
            {
                q.Add(creatureQueryProvider.Delete(spawn));
            }
            q.Add(creatureQueryProvider.BulkInsert(spawns));
        }

        if (spawnedGameObjects.Count > 0)
        {
            var startGuid = gameObjectStartGuid;
            var spawns = spawnedGameObjects.Values.Select((data, index) =>
                new GameObjectSpawnModelEssentials()
                {
                    Entry = data.Entry,
                    Guid = (uint)(startGuid + index),
                    Map = (int)data.MapId,
                    PhaseMask = 0,
                    X = data.Position.X,
                    Y = data.Position.Y,
                    Z = data.Position.Z,
                    Rotation0 = data.Rotation.X,
                    Rotation1 = data.Rotation.Y,
                    Rotation2 = data.Rotation.Z,
                    Rotation3 = data.Rotation.W,
                    State = data.GameObjectState,
                    AnimProgress = (byte)data.GameObjectAnimProgress,
                    __comment = cachedDatabaseProvider.GetCachedGameObjectTemplate(data.Entry)?.Name
                }).ToList();
            foreach (var spawn in spawns)
            {
                q.Add(gameObjectQueryProvider.Delete(spawn));
            }
            q.Add(gameObjectQueryProvider.BulkInsert(spawns));
        }

        return q.Close().QueryString;
    }

    protected override unsafe bool Process(ref readonly PacketBase basePacket, ref readonly PacketUpdateObject packet)
    {
        foreach (ref readonly var create in packet.Created.AsSpan())
        {
            if (create.CreateType != CreateObjectType.InRange)
                continue;
            if (create.Guid.Type != UniversalHighGuid.Vehicle && create.Guid.Type != UniversalHighGuid.Creature && create.Guid.Type != UniversalHighGuid.GameObject)
                continue;

            Vector3 position = default;
            Vector4 rotation = default;
            float orientation = default;

            if (create.Movement != null)
            {
                position = create.Movement->Position.ToVector3();
                orientation = create.Movement->Orientation;
            }
            else if (create.Stationary != null)
            {
                position = create.Stationary->Position.ToVector3();
                orientation = create.Stationary->Orientation;
            }

            if (create.Rotation != null)
            {
                rotation = new Vector4(create.Rotation->X,
                                    create.Rotation->Y,
                                    create.Rotation->Z,
                                    create.Rotation->W);
            }

            uint gameObjectState = 0;
            uint gameObjectAnimProgress = 0;

            if (create.Guid.Type == UniversalHighGuid.GameObject)
            {
                if (create.Values.TryGetInt("GAMEOBJECT_BYTES_1", out var bytes1))
                {
                    gameObjectState = (uint)(bytes1 & 0xFF);
                    gameObjectAnimProgress = (uint)(bytes1 >> 24) & 0xFF;
                }
            }

            SpawnData data = new SpawnData(
                create.Guid.Entry,
                position,
                rotation,
                orientation,
                create.Guid.GetMapId(),
                300,
                gameObjectState,
                gameObjectAnimProgress);
            if (create.Guid.Type == UniversalHighGuid.GameObject)
                spawnedGameObjects[create.Guid] = data;
            else
                spawnedCreatures[create.Guid] = data;
        }
        return default;
    }

    public record SpawnData(
        uint Entry,
        Vector3 Position,
        Vector4 Rotation,
        float Orientation,
        uint MapId,
        uint SpawnTimeSecs,
        uint GameObjectState,
        uint GameObjectAnimProgress
        )
    {

    }
}