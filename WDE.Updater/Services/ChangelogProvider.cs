using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.Updater.Models;
using WDE.Updater.ViewModels;

namespace WDE.Updater.Services
{
    [AutoRegister]
    [SingleInstance]
    public class ChangelogProvider : IChangelogProvider
    {
        private readonly IUpdaterSettingsProvider settings;
        private readonly IApplicationVersion currentVersion;
        private readonly IDocumentManager documentManager;
        private readonly IFileSystem fileSystem;

        public ChangelogProvider(IUpdaterSettingsProvider settings, 
            IApplicationVersion currentVersion,
            IDocumentManager documentManager,
            IFileSystem fileSystem)
        {
            this.settings = settings;
            this.currentVersion = currentVersion;
            this.documentManager = documentManager;
            this.fileSystem = fileSystem;
            
            if (settings.Settings.LastShowedChangelog < currentVersion.BuildVersion)
                TryShowChangelog();
        }

        public void TryShowChangelog()
        {
            var old = settings.Settings;
            old.LastShowedChangelog = currentVersion.BuildVersion;
            settings.Settings = old;

            var changes = LoadChanges();
            if (changes != null)
                documentManager.OpenDocument(new ChangeLogViewModel(changes));
        }

        public bool HasChangelog()
        {
            return fileSystem.Exists("~/changelog.json");
        }

        private List<ChangeLogEntry>? LoadChanges()
        {
            if (!HasChangelog())
                return null;
            try
            {
                return JsonConvert.DeserializeObject<List<ChangeLogEntry>>(fileSystem.ReadAllText("~/changelog.json"));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
