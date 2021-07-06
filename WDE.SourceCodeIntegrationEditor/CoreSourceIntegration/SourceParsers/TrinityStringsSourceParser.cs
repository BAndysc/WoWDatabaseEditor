using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WDE.Module.Attributes;
using WDE.SourceCodeIntegrationEditor.Extensions;

namespace WDE.SourceCodeIntegrationEditor.CoreSourceIntegration.SourceParsers
{
    [SingleInstance]
    [AutoRegister]
    public class TrinityStringsSourceParser : ITrinityStringsSourceParser
    {
        private static string TrinityLanguageFilePath = "src/server/game/Miscellaneous/Language.h";
        private static readonly Regex TrinityStringEnumRegex = new Regex(@"([A-Za-z_0-9]+)\s*=\s*(\d+),");
        private readonly ICoreSourceProvider coreSourceProvider;

        public TrinityStringsSourceParser(ICoreSourceProvider coreSourceProvider)
        {
            this.coreSourceProvider = coreSourceProvider;
        }

        public Dictionary<uint, string> ParseTrinityStringsEnum()
        {
            var result = new Dictionary<uint, string>();
            
            foreach (var rbacLine in coreSourceProvider.ReadLines(TrinityLanguageFilePath)
                .Select(t => t.Trim())
                .Between(t => t.StartsWith("/*"), t => t.EndsWith("*/"), true)
                .Between(t => t.StartsWith("enum TrinityStrings"), t => t.EndsWith("};"))
                .Where(t => !string.IsNullOrEmpty(t))
                .Where(t => !t.StartsWith("//")))
            {
                var match = TrinityStringEnumRegex.Match(rbacLine);
                if (!match.Success)
                    continue;
                var name = match.Groups[1].Value;
                var id = uint.Parse(match.Groups[2].Value);
                result[id] = name;
            }

            return result;
        }
    }
}