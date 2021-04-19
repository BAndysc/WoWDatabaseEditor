using System.Collections.Generic;
using System.IO;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [AutoRegister]
    public class TableDefinitionJsonProvider : ITableDefinitionJsonProvider
    {
        public IEnumerable<string> GetDefinitionSources()
        {
            return new[]
            {
                File.ReadAllText("DbDefinitions/creature_template.json"),
                File.ReadAllText("DbDefinitions/gameobject_template.json"),
                File.ReadAllText("DbDefinitions/creature_loot_template.json")
            };
        }
    }
}