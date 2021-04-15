using System.Collections.Generic;
using System.IO;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [AutoRegister]
    public class DbTableDataJsonProvider : IDbTableDataJsonProvider
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