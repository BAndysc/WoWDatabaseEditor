using System;
using System.Collections.Generic;
using WDE.Module.Attributes;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors.Utils;

[UniqueProvider]
public interface ISpellCastProcessor : IPacketProcessor<bool>
{
    DateTime? GetSpellStartTime(UniversalGuid? castId);
    DateTime? GetSpellGoTime(UniversalGuid? castId);
    DateTime? GetSpellFailTime(UniversalGuid? castId);
}

public static class SpellCastExtensions
{
    public static bool HasFinishedCastingAt(this ISpellCastProcessor processor, UniversalGuid? castId, PacketBase packet)
    {
        var time = processor.GetSpellGoTime(castId);
        return time.HasValue && time.Value == packet.Time.ToDateTime();
    }
    
    public static bool HasStartedCastingAt(this ISpellCastProcessor processor, UniversalGuid? castId, PacketBase packet)
    {
        var time = processor.GetSpellStartTime(castId);
        return time.HasValue && time.Value == packet.Time.ToDateTime();
    }
    
    public static bool HasFailedCastingAt(this ISpellCastProcessor processor, UniversalGuid? castId, PacketBase packet)
    {
        var time = processor.GetSpellFailTime(castId);
        return time.HasValue && time.Value == packet.Time.ToDateTime();
    }

    public static bool WillSpellFail(this ISpellCastProcessor processor, UniversalGuid? castId)
    {
        var time = processor.GetSpellFailTime(castId);
        return time.HasValue;
    }
    
    public static bool WillSpellSucceed(this ISpellCastProcessor processor, UniversalGuid? castId)
    {
        var time = processor.GetSpellGoTime(castId);
        return time.HasValue;
    }
}

[AutoRegister]
public unsafe class SpellCastProcessor : PacketProcessor<bool>, ISpellCastProcessor
{
    private class SpellData
    {
        public DateTime? SpellStart;
        public DateTime? SpellGo;
        public DateTime? SpellFail;
        public static SpellData Fake { get; } = new();
    }

    private Dictionary<UniversalGuid, SpellData> datas = new();

    private SpellData GetData(UniversalGuid? guid)
    {
        if (guid == null)
            return SpellData.Fake;

        if (datas.TryGetValue(guid.Value, out var d))
            return d;
        
        return datas[guid.Value] = new();
    }
    
    protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellStart packet)
    {
        var castId = packet.Data != null && packet.Data->IdKindCase == PacketSpellData.IdKindOneofCase.CastGuid ? packet.Data->CastGuid : (UniversalGuid?)null;
        var data = GetData(castId);
        data.SpellStart = basePacket.Time.ToDateTime();
        return base.Process(in basePacket, in packet);
    }

    protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellGo packet)
    {
        var castId = packet.Data != null && packet.Data->IdKindCase == PacketSpellData.IdKindOneofCase.CastGuid ? packet.Data->CastGuid : (UniversalGuid?)null;
        var data = GetData(castId);
        data.SpellGo = basePacket.Time.ToDateTime();
        return base.Process(in basePacket, in packet);
    }

    protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellFailure packet)
    {
        var castId = Unpack(packet.CastGuid);
        var data = GetData(castId);
        data.SpellFail = basePacket.Time.ToDateTime();
        return base.Process(in basePacket, in packet);
    }

    protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellCastFailed packet)
    {
        var castId = Unpack(packet.CastGuid);
        var data = GetData(castId);
        data.SpellFail = basePacket.Time.ToDateTime();
        return base.Process(in basePacket, in packet);
    }

    public DateTime? GetSpellStartTime(UniversalGuid? castId)
    {
        if (castId == null || !datas.TryGetValue(castId.Value, out var data))
            return null;
        return data.SpellStart;
    }

    public DateTime? GetSpellGoTime(UniversalGuid? castId)
    {
        if (castId == null || !datas.TryGetValue(castId.Value, out var data))
            return null;
        return data.SpellGo;
    }

    public DateTime? GetSpellFailTime(UniversalGuid? castId)
    {
        if (castId == null || !datas.TryGetValue(castId.Value, out var data))
            return null;
        return data.SpellFail;
    }
}