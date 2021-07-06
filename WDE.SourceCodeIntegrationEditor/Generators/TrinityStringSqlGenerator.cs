using System.Collections.Generic;
using System.Text;
using WDE.Common.CoreVersion;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.SourceCodeIntegrationEditor.ViewModels;

namespace WDE.SourceCodeIntegrationEditor.Generators
{
    public interface ITrinityStringSqlGenerator
    {
        string? GenerateWorld(IList<TrinityStringViewModel> strings);
    }

    [SingleInstance]
    [AutoRegister]
    public class TrinityStringSqlGenerator : ITrinityStringSqlGenerator
    {
        private readonly ICurrentCoreVersion currentCoreVersion;

        public TrinityStringSqlGenerator(ICurrentCoreVersion currentCoreVersion)
        {
            this.currentCoreVersion = currentCoreVersion;
        }
        
        public string? GenerateWorld(IList<TrinityStringViewModel> strings)
        {
            if (strings.Count == 0)
                return null;
            
            StringBuilder sb = new();
            
            List<string> sqlCommands = new();
            foreach (var s in strings)
            {
                sqlCommands.Add($"({s.Id}, {s.ContentDefault.ToSqlEscapeString()})");
            }
            
            if (sqlCommands.Count > 0)
            {
                var table = currentCoreVersion.Current.DatabaseFeatures.AlternativeTrinityStrings
                    ? "acore_string"
                    : "trinity_string";
                sb.AppendLine($"REPLACE INTO `{table}` (`entry`, `content_default`) VALUES ");
                sb.Append(string.Join(",\n", sqlCommands));
                sb.AppendLine(";\n");
            }
            
            return sb.ToString();
        }
    }
}