using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [AutoRegister]
    public class TableDefinitionJsonProvider : ITableDefinitionJsonProvider
    {
        private readonly IRuntimeDataService dataService;

        public TableDefinitionJsonProvider(IRuntimeDataService dataService)
        {
            this.dataService = dataService;
        }
        
        public async Task<IEnumerable<(string file, string content)>> GetDefinitionSources()
        {
            var files = await dataService.GetAllFiles("DbDefinitions/", "*.json");
            List<(string file, string content)> results = new List<(string file, string content)>();
            foreach (var f in files)
            {
                var content = await dataService.ReadAllText(f);
                results.Add((f, content));
            }

            return results;
        }
        
        public async Task<IEnumerable<(string file, string content)>> GetDefinitionReferences()
        {
            var files = await dataService.GetAllFiles("DbDefinitions/", "*.ref");
            List<(string file, string content)> results = new List<(string file, string content)>();
            foreach (var f in files)
            {
                var content = await dataService.ReadAllText(f);
                results.Add((f, content));
            }

            return results;
        }
    }
}