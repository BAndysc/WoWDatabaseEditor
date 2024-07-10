using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Utils;
using WowPacketParser.Proto;
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

        public void Initialize(ulong gameBuild)
        {
            inner.Initialize(gameBuild);
        }

        public bool Process(ref readonly PacketHolder packet)
        {
            var results = inner.Process(in packet);
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

        public async Task<string> Generate()
        {
            return string.Join(",\n", entries.Values.SelectMany(e => e));
        }
    }
}