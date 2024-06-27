using System;
using WDE.Module.Attributes;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors.Utils;

[UniqueProvider]
public interface IFromGuidSpawnTimeProcessor : IPacketProcessor<bool>
{
    TimeSpan? TryGetSpawnTime(UniversalGuid? guid, DateTime currentTime);
}

[AutoRegister]
public class FromGuidSpawnTimeProcessor : IFromGuidSpawnTimeProcessor
{
    private bool hasServerTimeDiff;
    private bool hasServerTimeDiffFromSpawnTime;
    private TimeSpan serverTimeDiff;
    
    public bool Process(ref readonly PacketHolder packet)
    {
        if (!hasServerTimeDiff)
        {
            if (packet.KindCase == PacketHolder.KindOneofCase.LoginSetTimeSpeed)
            {
                hasServerTimeDiff = true;
                serverTimeDiff = packet.BaseData.Time.ToDateTime() - packet.LoginSetTimeSpeed.GameTime.ToDateTime();        
            }
        }

        if (!hasServerTimeDiffFromSpawnTime)
        {
            if (packet.KindCase == PacketHolder.KindOneofCase.UpdateObject)
            {
                foreach (ref readonly var create in packet.UpdateObject.Created.AsSpan())
                {
                    if (create.CreateType == CreateObjectType.Spawn &&
                        create.Guid.Type is UniversalHighGuid.Creature or UniversalHighGuid.Vehicle or UniversalHighGuid.Pet or UniversalHighGuid.GameObject &&
                        create.Guid.KindCase == UniversalGuid.KindOneofCase.Guid128)
                    {
                        var timeQuotient = (new DateTimeOffset(packet.BaseData.Time.ToDateTime()).ToUnixTimeSeconds() & ~((1 << 23) - 1));
                        var timeSpawnOffset = (long)(create.Guid.Guid128.Low & ((1 << 23) - 1));
                        var currentServerTime = DateTimeOffset.FromUnixTimeSeconds(timeQuotient + timeSpawnOffset);
                        serverTimeDiff = packet.BaseData.Time.ToDateTime() - currentServerTime;
                        hasServerTimeDiff = true;
                        hasServerTimeDiffFromSpawnTime = true;
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public TimeSpan? TryGetSpawnTime(UniversalGuid? guid, DateTime currentTime)
    {
        if (guid == null ||
            (guid.Value.Type is not UniversalHighGuid.Creature && guid.Value.Type is not UniversalHighGuid.Pet &&
             guid.Value.Type is not UniversalHighGuid.Vehicle && guid.Value.Type is not UniversalHighGuid.GameObject) ||
            guid.Value.KindCase != UniversalGuid.KindOneofCase.Guid128)
            return null;

        if (!hasServerTimeDiff)
            return null;
        
        var timeStamp = (long)(guid.Value.Guid128.Low & ((1 << 23) - 1));
        var timeQuotient = (new DateTimeOffset(currentTime).ToUnixTimeSeconds() & ~((1 << 23) - 1));
        return (currentTime - serverTimeDiff) - DateTimeOffset.FromUnixTimeSeconds(timeQuotient + timeStamp);
    }
}