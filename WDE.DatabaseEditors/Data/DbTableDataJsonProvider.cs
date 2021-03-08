using System.IO;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [AutoRegister]
    public class DbTableDataJsonProvider : IDbTableDataJsonProvider
    {
        public string GetCreatureTemplateDefinitionJson() => File.ReadAllText("DbDefinitions/creature_template.json");
        public string GetGameobjectTemplateDefinitionJson() => File.ReadAllText("DbDefinitions/gameobject_template.json");
    }
}