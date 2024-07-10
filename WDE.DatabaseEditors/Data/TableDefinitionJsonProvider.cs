using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [AutoRegister]
    public class TableDefinitionJsonProvider : ITableDefinitionJsonProvider
    {
        private readonly IRuntimeDataService dataService;
        private readonly IMainThread mainThread;
        private readonly IDirectoryWatcher watcher;

        private System.IDisposable? pendingFileUpdate;

        public TableDefinitionJsonProvider(IRuntimeDataService dataService,
            IMainThread mainThread)
        {
            this.dataService = dataService;
            this.mainThread = mainThread;
            watcher = dataService.WatchDirectory("DbDefinitions", true);
            watcher.OnChanged += OnFileChanged;
        }

        private void OnFileChanged(WatcherChangeTypes type, string filePath)
        {
            void InvokeFileChanged()
            {
                FilesChanged?.Invoke();
                pendingFileUpdate = null;
            }

            pendingFileUpdate?.Dispose();
            pendingFileUpdate = mainThread.Delay(InvokeFileChanged, TimeSpan.FromMilliseconds(500));
        }

        public async Task<IEnumerable<(string file, string content)>> GetDefinitionSources()
        {
            var files = await dataService.GetAllFiles("DbDefinitions/", "*.json");
            List<(string file, string content)> results = new List<(string file, string content)>();
            foreach (var f in files)
            {
                var content = await dataService.ReadAllText(f);
                if (string.IsNullOrWhiteSpace(content))
                    continue;

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

        public event Action? FilesChanged;
    }
}