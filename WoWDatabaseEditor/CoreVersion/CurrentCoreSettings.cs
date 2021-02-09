using System.IO;
using Newtonsoft.Json;
using WDE.Common.CoreVersion;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.CoreVersion
{
    [SingleInstance]
    [AutoRegister]
    public class CurrentCoreSettings : ICurrentCoreSettings
    {
        private const string settingsFile = "coreversion.json";
        
        public string? CurrentCore { get; }
        
        public CurrentCoreSettings()
        {
            if (File.Exists(settingsFile))
            {
                Data data = JsonConvert.DeserializeObject<Data>(File.ReadAllText(settingsFile));
                CurrentCore = data.Version;
            }
        }
        
        public void UpdateCore(ICoreVersion core)
        {
            Data data = new Data() {Version = core.Tag};
            File.WriteAllText(settingsFile, JsonConvert.SerializeObject(data));
        }

        private struct Data
        {
            public string Version { get; set; }
        }
    }
}