using System;
using System.Collections.Generic;
using System.Linq;
using ProtoZeroSharp;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class SplitUpdateProcessor
    {
        private readonly GuidExtractorProcessor guidExtractorProcessor;

        public SplitUpdateProcessor(GuidExtractorProcessor guidExtractorProcessor, int baseNumber, RefCountedArena allocator)
        {
            this.guidExtractorProcessor = guidExtractorProcessor;
            NextNumber = baseNumber;
            this.allocator = allocator;
        }

        public void Initialize(ulong gameBuild)
        {
            guidExtractorProcessor.Initialize(gameBuild);
        }

        private string GenerateText(PacketBase basePacket, string? text)
        {
            text ??= "";
            var unicodeStringData = basePacket.StringData.ToString() ?? "";
            int indexOfFirstLine = unicodeStringData.IndexOf("\n", StringComparison.Ordinal);
            if (indexOfFirstLine == -1)
                return text;
            int indexOfSecondLine = unicodeStringData.IndexOf("\n", indexOfFirstLine + 1, StringComparison.Ordinal);
            if (indexOfSecondLine == -1)
                return text;
            int indexOfThirdLine = unicodeStringData.IndexOf("\n", indexOfSecondLine + 1, StringComparison.Ordinal);
            if (indexOfThirdLine == -1)
                return text;
            return unicodeStringData.Substring(0, indexOfThirdLine + 1) +
                   text;
        }

        private int NextNumber = 0;
        private readonly RefCountedArena allocator;
        private DateTime? LastPacketTime;
        private List<Pointer<PacketHolder>> pending = new();

        private int GetPriority(ref PacketHolder holder)
        {
            var kind = holder.KindCase;
            if (kind == PacketHolder.KindOneofCase.SpellStart)
                return 0;
            if (kind == PacketHolder.KindOneofCase.SpellGo)
                return 1;
            if (kind == PacketHolder.KindOneofCase.AuraUpdate)
                return 2;
            if (kind == PacketHolder.KindOneofCase.UpdateObject)
                return 3;
            if (kind == PacketHolder.KindOneofCase.MonsterMove)
                return 4;
            return 5;
        }

        private List<(Pointer<PacketHolder>, int)> afterSplit = new();
        public IEnumerable<(Pointer<PacketHolder>, int)>? Finalize()
        {
            using var @ref = allocator.Increment();

            pending.Sort(Comparer<Pointer<PacketHolder>>.Create((a, b) => GetPriority(ref a.Value).CompareTo(GetPriority(ref b.Value))));

            afterSplit.Clear();
            foreach (var p in pending)
            {
                var enumerable = InnerSplit(p);
                if (enumerable != null)
                    afterSplit.AddRange(enumerable);
            }
            pending.Clear();

            var created = new List<(Pointer<PacketHolder>, int)>();
            var moveToFirst = new List<(Pointer<PacketHolder>, int)>();
            foreach (var a in afterSplit)
            {
                if (a.Item1.Value.KindCase == PacketHolder.KindOneofCase.UpdateObject && a.Item1.Value.UpdateObject.Created.Count == 1)
                    created.Add(a);
            }

            foreach (var a in afterSplit)
            {
                if (a.Item1.Value.KindCase != PacketHolder.KindOneofCase.UpdateObject)
                {
                    var guid = guidExtractorProcessor.Process(ref a.Item1.Value);
                    if (guid == null)
                        continue;
                    foreach (var create in created)
                    {
                        if (create.Item1.Value.UpdateObject.Created[0].Guid.Equals(guid))
                        {
                            moveToFirst.Add(create);
                            created.Remove(create);
                            break;
                        }
                    }
                }
            }

            foreach (var first in moveToFirst)
                afterSplit.Remove(first);
            
            foreach (var p in moveToFirst.Concat(afterSplit))
            {
                var newPacketHolder = @ref.Array.Alloc<PacketHolder>();
                newPacketHolder.Value = p.Item1.Value; // it is a struct, so this is a shallow copy, perfect
                newPacketHolder.Value.BaseData.Number = NextNumber++;
                yield return (newPacketHolder, p.Item2);
            }
        }
        
        public unsafe IEnumerable<(Pointer<PacketHolder>, int)>? Process(PacketHolder* packet)
        {
            List<(Pointer<PacketHolder>, int)> output = new();
            var packetTime = packet->BaseData.Time.ToDateTime();
            if (!LastPacketTime.HasValue || (packetTime - LastPacketTime.Value).TotalMilliseconds > 0)
            {
                var enumerable = Finalize();
                if (enumerable != null)
                {
                    foreach (var p in enumerable)
                        output.Add(p);
                }
                LastPacketTime = packetTime;
            }
            pending.Add(packet);
            return output;
        }
        
        private IEnumerable<(Pointer<PacketHolder>, int)>? InnerSplit(Pointer<PacketHolder> packet)
        {
            if (packet.Value.KindCase == PacketHolder.KindOneofCase.UpdateObject)
            {
                var iterator = Process(ref packet.Value.BaseData, ref packet.Value.UpdateObject);
                if (iterator != null)
                {
                    foreach (var up in iterator)
                    {
                        yield return up;
                    }
                }
            }
            else
            {
                yield return (packet, packet.Value.BaseData.Number);
            }
        }

        protected IEnumerable<(Pointer<PacketHolder>, int)>? Process(ref readonly PacketBase basePacket, ref readonly PacketUpdateObject packet)
        {
            using var @ref = allocator.Increment();

            List<(Pointer<PacketHolder>, int)> output = new();

            foreach (ref readonly var destroyed in packet.Destroyed.AsSpan())
            {
                var fake = new PacketUpdateObject();
                fake.Destroyed = UnmanagedArray<DestroyedObject>.AllocArray(1, ref @ref.Array);
                fake.Destroyed[0] = destroyed;
                var newBaseData = basePacket; // struct so this is a copy
                newBaseData.StringData = Utf8String.CreateFromString(GenerateText(basePacket, destroyed.Text?.ToString()), ref @ref.Array);

                var newPacketHolder = @ref.Array.Alloc<PacketHolder>();
                newPacketHolder.Value.BaseData = newBaseData;
                newPacketHolder.Value.AllocKind<PacketUpdateObject>(ref @ref.Array) = fake;
                newPacketHolder.Value.KindCase = PacketHolder.KindOneofCase.UpdateObject;

                output.Add((newPacketHolder, basePacket.Number));
            }
            
            foreach (ref readonly var outOfRange in packet.OutOfRange.AsSpan())
            {
                var fake = new PacketUpdateObject();
                fake.OutOfRange = UnmanagedArray<DestroyedObject>.AllocArray(1, ref @ref.Array);
                fake.OutOfRange[0] = outOfRange;
                var newBaseData = basePacket; // struct so this is a copy
                newBaseData.StringData = Utf8String.CreateFromString(GenerateText(basePacket, outOfRange.Text?.ToString()), ref @ref.Array);

                var newPacketHolder = @ref.Array.Alloc<PacketHolder>();
                newPacketHolder.Value.BaseData = newBaseData;
                newPacketHolder.Value.AllocKind<PacketUpdateObject>(ref @ref.Array) = fake;
                newPacketHolder.Value.KindCase = PacketHolder.KindOneofCase.UpdateObject;

                output.Add((newPacketHolder, basePacket.Number));
            }
            
            foreach (ref readonly var created in packet.Created.AsSpan())
            {
                var fake = new PacketUpdateObject();
                fake.Created = UnmanagedArray<CreateObject>.AllocArray(1, ref @ref.Array);
                fake.Created[0] = created;
                var newBaseData = basePacket; // struct so this is a copy
                newBaseData.StringData = Utf8String.CreateFromString(GenerateText(basePacket, created.Text?.ToString()), ref @ref.Array);

                var newPacketHolder = @ref.Array.Alloc<PacketHolder>();
                newPacketHolder.Value.BaseData = newBaseData;
                newPacketHolder.Value.AllocKind<PacketUpdateObject>(ref @ref.Array) = fake;
                newPacketHolder.Value.KindCase = PacketHolder.KindOneofCase.UpdateObject;

                output.Add((newPacketHolder, basePacket.Number));
            }
            
            foreach (ref readonly var updated in packet.Updated.AsSpan())
            {
                var fake = new PacketUpdateObject();
                fake.Updated = UnmanagedArray<UpdateObject>.AllocArray(1, ref @ref.Array);
                fake.Updated[0] = updated;
                var newBaseData = basePacket; // struct so this is a copy
                newBaseData.StringData = Utf8String.CreateFromString(GenerateText(basePacket, updated.Text?.ToString()), ref @ref.Array);

                var newPacketHolder = @ref.Array.Alloc<PacketHolder>();
                newPacketHolder.Value.BaseData = newBaseData;
                newPacketHolder.Value.AllocKind<PacketUpdateObject>(ref @ref.Array) = fake;
                newPacketHolder.Value.KindCase = PacketHolder.KindOneofCase.UpdateObject;

                output.Add((newPacketHolder, basePacket.Number));
            }

            return output;
        }
    }
}