using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WDE.Module.Attributes;
using WDE.SourceCodeIntegrationEditor.Extensions;

namespace WDE.SourceCodeIntegrationEditor.CoreSourceIntegration.SourceParsers
{
    [SingleInstance]
    [AutoRegister]
    public class RbacSourceParser : IRbacSourceParser
    {
        private static string TrinityRbacFilePath = "src/server/game/Accounts/RBAC.h";
        private static Regex rbacEnum = new Regex(@"([A-Za-z_0-9]+)\s*=\s*(\d+),");
        private readonly ICoreSourceProvider coreSourceProvider;

        public RbacSourceParser(ICoreSourceProvider coreSourceProvider)
        {
            this.coreSourceProvider = coreSourceProvider;
        }

        public bool CoreSupportsRbac() => coreSourceProvider.FileExists(TrinityRbacFilePath);

        public Dictionary<uint, string> ParseRbacEnum()
        {
            var result = new Dictionary<uint, string>();
            
            foreach (var rbacLine in coreSourceProvider.ReadLines(TrinityRbacFilePath)
                .Select(t => t.Trim())
                .Between(t => t.StartsWith("/*"), t => t.EndsWith("*/"), true)
                .Between(t => t.StartsWith("enum RBACPermissions"), t => t.EndsWith("};"))
                .Where(t => !string.IsNullOrEmpty(t))
                .Where(t => !t.StartsWith("//")))
            {
                var match = rbacEnum.Match(rbacLine);
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