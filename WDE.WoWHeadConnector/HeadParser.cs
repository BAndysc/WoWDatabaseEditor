using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using WDE.Common.Game;
using WDE.Common.Services.HeadConnector;
using WDE.Module.Attributes;

namespace WDE.WoWHeadConnector
{
    [SingleInstance]
    [AutoRegister]
    internal class HeadParser : IHeadParser
    {
        private const string ModeLookupText = "modes:{\"mode\":[";
        private const string AddDataSpell = "WH.Gatherer.addData(6, ";

        private ICollection<MapDifficulty>? FindModesForSpell(string source, uint spell)
        {
            var lookText = "\"id\":" + spell;
            var start = source.IndexOf(lookText, StringComparison.InvariantCultureIgnoreCase);
            if (start == -1)
                return null;
            
            var modesStart = source.IndexOf(ModeLookupText, start, StringComparison.InvariantCultureIgnoreCase);

            if (modesStart == -1)
                return null;

            modesStart += ModeLookupText.Length;

            int end = source.IndexOf(']', modesStart);

            if (end == -1)
                return null;

            var modes = source.Substring(modesStart, end - modesStart);
            var split = modes.Split(',');

            return split.Where(s => uint.TryParse(s, out _)).Select(uint.Parse).Cast<MapDifficulty>().ToList();
        }

        private int PartParseAbilities(string source, int start, List<Ability> list)
        {
            start = source.IndexOf(AddDataSpell, start, StringComparison.Ordinal);
            if (start == -1)
                return -1;

            start += AddDataSpell.Length;

            start = source.IndexOf('{', start);
            if (start == -1)
                throw new HeadParserException("Cannot parse wow head");

            int end = source.IndexOf(");", start, StringComparison.Ordinal);
            if (end == -1)
                throw new HeadParserException("Cannot parse wow head");

            var json = source.Substring(start, end - start);
            try
            {
                var entries = JsonConvert.DeserializeObject<Dictionary<uint, object>>(json);
                if (entries != null)
                    list.AddRange(entries.Select(pair => new Ability() { SpellId = pair.Key, Modes = FindModesForSpell(source, pair.Key) }));
                return end;
            }
            catch (Exception e)
            {
                throw new HeadParserException("Cannot parse json: " + e.Message);
            }
        }
        
        public IList<Ability> ParseAbilities(string source)
        {
            int start = 0;
            List<Ability> abilities = new();
            while (start != -1)
                start = PartParseAbilities(source, start, abilities);
            return abilities;
        }
    }
}