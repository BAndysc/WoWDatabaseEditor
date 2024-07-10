using System;
using System.Collections.Generic;
using System.Reactive;
using System.Runtime.Versioning;
using WDE.Module.Attributes;
using WDE.PacketViewer.Utils;
using WDE.PacketViewer.Utils.IntervalTrees;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors;

public enum SniffObjectState
{
    Unknown,
    Destroyed,
    OutOfRange,
    Spawned,
    InRange,
}

[UniqueProvider]
public interface IUpdateFieldsHistory : IPacketProcessor<Unit>
{
    (Interval<int, SniffObjectState> current, Interval<int, SniffObjectState>? previous, Interval<int, SniffObjectState>? next) GetState(UniversalGuid guid, int time);
    IEnumerable<(string key, Interval<int, long> current, Interval<int, long>? previous, Interval<int, long>? next)> IntValues(UniversalGuid guid, int time);
    IEnumerable<(string key, Interval<int, float> current, Interval<int, float>? previous, Interval<int, float>? next)> FloatValues(UniversalGuid guid, int time);
    ICollection<UniversalGuid> AllGuids { get; }
    void Finish();
}

[AutoRegister]
[RequiresPreviewFeatures]
public class UpdateFieldsHistory : PacketProcessor<Unit>, IUpdateFieldsHistory
{
    private int lastPacketNumber;
    public class ObjectState
    {
        public OptimizedIntervalTree<int, SniffObjectState>? spawnState;
        public Dictionary<string, OptimizedIntervalTree<int, long>> IntValues = new();
        public Dictionary<string, OptimizedIntervalTree<int, float>> FloatValues = new();
        
        public Dictionary<string, (long value, int numberStart)> pendingIntChanges = new();
        public Dictionary<string, (float value, int numberStart)> pendingFloatChanges = new();
        public (SniffObjectState state, int numberStart)? pendingState;

        public void FinalizeState(int ending)
        {
            if (pendingState.HasValue)
            {
                if (spawnState == null)
                    spawnState = new OptimizedIntervalTree<int, SniffObjectState>(pendingState.Value.numberStart, ending, pendingState.Value.state);
                else
                    spawnState.Add(pendingState.Value.numberStart, ending, pendingState.Value.state);
            }
            pendingState = null;
        }
        
        public void FinalizeInt(string key, int ending)
        {
            if (pendingIntChanges.TryGetValue(key, out var pending))
            {
                if (IntValues.TryGetValue(key, out var intHistory))
                    intHistory.Add(pending.numberStart, ending, pending.value);
                else
                    IntValues[key] = new (pending.numberStart, ending, pending.value);
                pendingIntChanges.Remove(key);
            }
        }
        
        public void FinalizeFloat(string key, int ending)
        {
            if (pendingFloatChanges.TryGetValue(key, out var pending))
            {
                if (FloatValues.TryGetValue(key, out var intHistory))
                    intHistory.Add(pending.numberStart, ending, pending.value);
                else
                    FloatValues[key] = new (pending.numberStart, ending, pending.value);
                pendingFloatChanges.Remove(key);
            }
        }
        
        public void FinalizeAllPending(int ending)
        {
            foreach (var (key, (value, numberStart)) in pendingIntChanges)
            {
                if (IntValues.TryGetValue(key, out var intHistory))
                    intHistory.Add(numberStart, ending, value);
                else
                    IntValues[key] = new (numberStart, ending, value);
            }
            
            foreach (var (key, (value, numberStart)) in pendingFloatChanges)
            {
                if (FloatValues.TryGetValue(key, out var intHistory))
                    intHistory.Add(numberStart, ending, value);
                else
                    FloatValues[key] = new (numberStart, ending, value);
            }

            pendingFloatChanges.Clear();
            pendingIntChanges.Clear();
        }
    }

    private Dictionary<UniversalGuid, ObjectState> states = new();

    public ObjectState GetState(UniversalGuid guid)
    {
        if (states.TryGetValue(guid, out var state))
            return state;
        return states[guid] = new();
    }

    public void Finish()
    {
        foreach (var state in states.Values)
        {
            state.FinalizeAllPending(lastPacketNumber);
            state.FinalizeState(lastPacketNumber);
        }
    }

    public override Unit Process(ref readonly PacketHolder packet)
    {
        lastPacketNumber = packet.BaseData.Number;
        return base.Process(in packet);
    }

    protected override Unit Process(ref readonly PacketBase basePacket, ref readonly PacketUpdateObject packet)
    {
        foreach (ref readonly var create in packet.Created.AsSpan())
        {
            if (SkipGuid(create.Guid))
                continue;
            var state = GetState(create.Guid);
            //Debug.Assert(state.pendingIntChanges.Count == 0);
            //Debug.Assert(state.pendingFloatChanges.Count == 0);

            state.FinalizeState(basePacket.Number - 1);
            state.pendingState = (create.CreateType == CreateObjectType.Spawn ? SniffObjectState.Spawned : SniffObjectState.InRange, basePacket.Number);
            
            foreach (var intVal in create.Values.Ints())
            {
                state.pendingIntChanges[intVal.Key] = (intVal.Value, basePacket.Number);
            }
            
            foreach (var floatVal in create.Values.Floats())
            {
                state.pendingFloatChanges[floatVal.Key] = (floatVal.Value, basePacket.Number);
            }
        }

        foreach (ref readonly var update in packet.Updated.AsSpan())
        {
            if (SkipGuid(update.Guid))
                continue;
            
            var state = GetState(update.Guid);
            foreach (var intVal in update.Values.Ints())
            {
                state.FinalizeInt(intVal.Key, basePacket.Number - 1);
                state.pendingIntChanges[intVal.Key] = (intVal.Value, basePacket.Number);
            }
            
            foreach (var floatVal in update.Values.Floats())
            {
                state.FinalizeFloat(floatVal.Key, basePacket.Number - 1);
                state.pendingFloatChanges[floatVal.Key] = (floatVal.Value, basePacket.Number);
            }
        }

        foreach (ref readonly var delete in packet.Destroyed.AsSpan())
        {
            if (SkipGuid(delete.Guid))
                continue;
            
            var state = GetState(delete.Guid);
            state.FinalizeState(basePacket.Number - 1);
            state.pendingState = (SniffObjectState.Destroyed, basePacket.Number);
            state.FinalizeAllPending(basePacket.Number);
        }
        
        foreach (ref readonly var delete in packet.OutOfRange.AsSpan())
        {
            if (SkipGuid(delete.Guid))
                continue;
            
            var state = GetState(delete.Guid);
            state.FinalizeState(basePacket.Number - 1);
            state.pendingState = (SniffObjectState.OutOfRange, basePacket.Number);
            state.FinalizeAllPending(basePacket.Number);
        }
        return base.Process(in basePacket, in packet);
    }

    private bool SkipGuid(UniversalGuid guid)
    {
        return guid.Type != UniversalHighGuid.Player &&
               guid.Type != UniversalHighGuid.GameObject &&
               guid.Type != UniversalHighGuid.Creature &&
               guid.Type != UniversalHighGuid.Vehicle &&
               guid.Type != UniversalHighGuid.Transport;
    }

    public (Interval<int, SniffObjectState> current, Interval<int, SniffObjectState>? previous, Interval<int, SniffObjectState>? next) GetState(UniversalGuid guid, int time)
    {
        if (!states.TryGetValue(guid, out var state))
            return (new Interval<int, SniffObjectState>(0, 0, SniffObjectState.Unknown), null, null);
        
        var before = state.spawnState?.QueryBefore(time);
        var now = state.spawnState?.Query(time);
        var next = state.spawnState?.QueryAfter(time);
        
        return (now ?? new Interval<int, SniffObjectState>(0, 0, SniffObjectState.Unknown), before, next);
    }

    public IEnumerable<(string, Interval<int, long>, Interval<int, long>?, Interval<int, long>?)> IntValues(UniversalGuid guid, int time)
    {
        if (!states.TryGetValue(guid, out var state))
            yield break;

        foreach (var key in state.IntValues)
        {
            var before = key.Value.QueryBefore(time);
            var now = key.Value.Query(time);
            var next = key.Value.QueryAfter(time);
            if (now.HasValue || before.HasValue || next.HasValue)
            {
                if (next.HasValue && next.Value.value == now?.value)
                    next = null;
                if (before.HasValue && before.Value.value == now?.value)
                    before = null;
                yield return (key.Key, now ?? new Interval<int, long>(time, time, 0), before, next);
            }
        }
    }

    public IEnumerable<(string key, Interval<int, float> current, Interval<int, float>? previous, Interval<int, float>? next)> FloatValues(UniversalGuid guid, int time)
    {
        if (!states.TryGetValue(guid, out var state))
            yield break;

        foreach (var key in state.FloatValues)
        {
            var before = key.Value.QueryBefore(time);
            var now = key.Value.Query(time);
            var next = key.Value.QueryAfter(time);
            if (now.HasValue || before.HasValue || next.HasValue)
            {
                if (next.HasValue && Math.Abs(next.Value.value - (now?.value ?? 0)) < 0.001f)
                    next = null;
                if (before.HasValue && Math.Abs(before.Value.value - (now?.value ?? 0)) < 0.001f)
                    before = null;
                yield return (key.Key, now ?? new Interval<int, float>(time, time, 0), before, next);
            }
        }
    }

    public ICollection<UniversalGuid> AllGuids => states.Keys;
}
