using System;
using System.Collections.Generic;
using System.Linq;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class SplitUpdateProcessor : PacketProcessor<IEnumerable<(PacketHolder, int)>?>
    {
        private readonly GuidExtractorProcessor guidExtractorProcessor;

        public SplitUpdateProcessor(GuidExtractorProcessor guidExtractorProcessor, int baseNumber)
        {
            this.guidExtractorProcessor = guidExtractorProcessor;
            NextNumber = baseNumber;
        }

        public override void Initialize(ulong gameBuild)
        {
            guidExtractorProcessor.Initialize(gameBuild);
        }

        private string GenerateText(PacketBase basePacket, string text)
        {
            int indexOfFirstLine = basePacket.StringData.IndexOf("\n", StringComparison.Ordinal);
            if (indexOfFirstLine == -1)
                return text;
            int indexOfSecondLine = basePacket.StringData.IndexOf("\n", indexOfFirstLine + 1, StringComparison.Ordinal);
            if (indexOfSecondLine == -1)
                return text;
            int indexOfThirdLine = basePacket.StringData.IndexOf("\n", indexOfSecondLine + 1, StringComparison.Ordinal);
            if (indexOfThirdLine == -1)
                return text;
            return basePacket.StringData.Substring(0, indexOfThirdLine + 1) +
                   text;
        }

        private int NextNumber = 0;
        private DateTime? LastPacketTime;
        private List<PacketHolder> pending = new();

        private int GetPriority(PacketHolder holder)
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

        private List<(PacketHolder, int)> afterSplit = new();
        public IEnumerable<(PacketHolder, int)>? Finalize()
        {
            pending.Sort(Comparer<PacketHolder>.Create((a, b) => GetPriority(a).CompareTo(GetPriority(b))));

            afterSplit.Clear();
            foreach (var p in pending)
            {
                var enumerable = InnerSplit(p);
                if (enumerable != null)
                    afterSplit.AddRange(enumerable);
            }
            pending.Clear();

            var created = new List<(PacketHolder, int)>();
            var moveToFirst = new List<(PacketHolder, int)>();
            foreach (var a in afterSplit)
            {
                if (a.Item1.KindCase == PacketHolder.KindOneofCase.UpdateObject && a.Item1.UpdateObject.Created.Count == 1)
                    created.Add(a);
            }

            foreach (var a in afterSplit)
            {
                if (a.Item1.KindCase != PacketHolder.KindOneofCase.UpdateObject)
                {
                    var guid = guidExtractorProcessor.Process(a.Item1);
                    if (guid == null)
                        continue;
                    foreach (var create in created)
                    {
                        if (create.Item1.UpdateObject.Created[0].Guid.Equals(guid))
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
                yield return (new PacketHolder(p.Item1)
                {
                    BaseData = new PacketBase(p.Item1.BaseData) { Number = NextNumber++ },
                }, p.Item2);
            }
        }
        
        public override IEnumerable<(PacketHolder, int)>? Process(PacketHolder packet)
        {
            var packetTime = packet.BaseData.Time.ToDateTime();
            if (!LastPacketTime.HasValue || (packetTime - LastPacketTime.Value).TotalMilliseconds > 0)
            {
                var enumerable = Finalize();
                if (enumerable != null)
                {
                    foreach (var p in enumerable)
                        yield return p;   
                }
                LastPacketTime = packetTime;
            }
            pending.Add(packet);
        }
        
        private IEnumerable<(PacketHolder, int)>? InnerSplit(PacketHolder packet)
        {
            if (packet.KindCase == PacketHolder.KindOneofCase.UpdateObject)
            {
                var iterator = Process(packet.BaseData, packet.UpdateObject);
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
                yield return (packet, packet.BaseData.Number);
            }
        }

        protected override IEnumerable<(PacketHolder, int)>? Process(PacketBase basePacket, PacketUpdateObject packet)
        {
            foreach (var destroyed in packet.Destroyed)
            {
                var fake = new PacketUpdateObject();
                fake.Destroyed.Add(destroyed);
                yield return (new PacketHolder()
                {
                    BaseData = new PacketBase(basePacket){StringData = GenerateText(basePacket, destroyed.Text), TextStartOffset = basePacket.TextStartOffset, TextLength = basePacket.TextLength},
                    UpdateObject = fake
                }, basePacket.Number);
            }
            
            foreach (var outOfRange in packet.OutOfRange)
            {
                var fake = new PacketUpdateObject();
                fake.OutOfRange.Add(outOfRange);
                yield return (new PacketHolder()
                {
                    BaseData = new PacketBase(basePacket){StringData = GenerateText(basePacket, outOfRange.Text), TextStartOffset = basePacket.TextStartOffset, TextLength = basePacket.TextLength},
                    UpdateObject = fake
                }, basePacket.Number);
            }
            
            foreach (var created in packet.Created)
            {
                var fake = new PacketUpdateObject();
                fake.Created.Add(created);
                yield return (new PacketHolder()
                {
                    BaseData = new PacketBase(basePacket){StringData = GenerateText(basePacket, created.Text), TextStartOffset = basePacket.TextStartOffset, TextLength = basePacket.TextLength},
                    UpdateObject = fake
                }, basePacket.Number);
            }
            
            foreach (var updated in packet.Updated)
            {
                var fake = new PacketUpdateObject();
                fake.Updated.Add(updated);
                yield return (new PacketHolder()
                {
                    BaseData = new PacketBase(basePacket){StringData = GenerateText(basePacket, updated.Text), TextStartOffset = basePacket.TextStartOffset, TextLength = basePacket.TextLength},
                    UpdateObject = fake
                }, basePacket.Number);
            }
        }
    }
}