using System.Collections.Generic;
using System.Linq;
using WDE.Common.Utils;
using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class NameAsEnumDumper : IPacketTextDumper
    {
        private readonly IPacketProcessor<IEnumerable<(string prefix, string name, uint entry)>> inner;
        private HashSet<uint> existing = new();
        private Dictionary<string, List<string>> entries = new();

        public NameAsEnumDumper(IPacketProcessor<IEnumerable<(string prefix, string name, uint entry)>> inner)
        {
            this.inner = inner;
        }
        
        public bool Process(PacketHolder packet)
        {
            var results = inner.Process(packet);
            if (results == null)
                return false;

            foreach (var result in results)
            {
                if (!existing.Add(result.entry))
                    continue;

                if (!entries.TryGetValue(result.prefix, out var list))
                    list = entries[result.prefix] = new();

                var name = (result.prefix + " " + result.name).ToEnumName();
                list.Add($"{name,-40} = {result.entry}");
            }

            return true;
        }

        public string Generate()
        {
            return string.Join(",\n", entries.Values.SelectMany(e => e));
        }
    }
}