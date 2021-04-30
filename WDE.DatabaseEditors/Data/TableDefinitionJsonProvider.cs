using System.Collections.Generic;
using System.IO;
using System.Linq;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [AutoRegister]
    public class TableDefinitionJsonProvider : ITableDefinitionJsonProvider
    {
        public IEnumerable<(string file, string content)> GetDefinitionSources()
        {
            return Directory.GetFiles("DbDefinitions/", "*.json", SearchOption.AllDirectories).Select(f => (f, File.ReadAllText(f)));
        }
    }
}