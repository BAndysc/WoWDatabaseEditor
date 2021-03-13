using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [SingleInstance]
    [AutoRegister]
    public class DbFieldNameSwapDataManager : IDbFieldNameSwapDataManager
    {
        private readonly Dictionary<string, TableFieldsNameSwapDefinition> swapDefinitions;

        public DbFieldNameSwapDataManager()
        {
            swapDefinitions = new Dictionary<string, TableFieldsNameSwapDefinition>();
        }
        
        public void RegisterSwapDefinition(string tableName, string path)
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                swapDefinitions[tableName] = JsonConvert.DeserializeObject<TableFieldsNameSwapDefinition>(json);
            }
        }

        public TableFieldsNameSwapDefinition? GetSwapData(string tableName)
        {
            if (swapDefinitions.ContainsKey(tableName))
                return swapDefinitions[tableName];
            return null;
        }
    }
}