using System;
using System.IO;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.UserSettingsService
{
    [SingleInstance]
    [AutoRegister(Platforms.NonBrowser)]
    public class UserSettings : IUserSettings
    {
        private readonly IFileSystem fileSystem;
        private readonly Lazy<IStatusBar> statusBar;
        private readonly string basePath;
        private readonly JsonSerializer json;
        
        public UserSettings(IFileSystem fileSystem, Lazy<IStatusBar> statusBar)
        {
            json = new() {TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented};
            json.Converters.Add(new DatabaseKeyConverter());
            this.fileSystem = fileSystem;
            this.statusBar = statusBar;
            basePath = "~";
        }
        
        private string GetSettingPath<T>()
        {
            return Path.Join(basePath, typeof(T).FullName!.Replace("+", ".") + ".json");
        }
        
        public T? Get<T>(T? defaultValue = default) where T : ISettings
        {
            var settingsFile = GetSettingPath<T>();
            if (!fileSystem.Exists(settingsFile))
                return defaultValue;
            
            try
            {
                using var file = fileSystem.OpenRead(settingsFile);
                using var stream = new StreamReader(file);
                using var reader = new JsonTextReader(stream);
                return json.Deserialize<T>(reader);
            }
            catch (Exception e)
            {
                statusBar.Value.PublishNotification(new PlainNotification(NotificationType.Error, "Error while loading settings: " + e));
                LOG.LogError(e, message: "Error while loading settings: " + fileSystem?.ResolvePhysicalPath(settingsFile)?.FullName);
                return defaultValue;
            }
        }

        public void Update<T>(T newSettings) where T : ISettings
        {
            var settingsFile = GetSettingPath<T>();
            try
            {
                using var writer = fileSystem.OpenWrite(settingsFile);
                using var streamWriter = new StreamWriter(writer);
                json.Serialize(streamWriter, newSettings);
            }
            catch (Exception e)
            {
                statusBar.Value.PublishNotification(new PlainNotification(NotificationType.Error, "Error while saving settings: " + e));
            }
        }
    }
}