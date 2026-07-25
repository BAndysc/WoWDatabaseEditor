using System.ComponentModel;
using Newtonsoft.Json;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Settings
{
    [AutoRegisterToParentScope]
    [SingleInstance]
    public class CMangosSettingsProvider : ICMangosSettingsProvider
    {
        private readonly IUserSettings userSettings;

        public CMangosSettingsProvider(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            var data = userSettings.Get<Data>();
            DbRepositoryPath = data?.DbRepositoryPath;
            UpdateAcidFileOnSave = data?.UpdateAcidFileOnSave ?? true;
        }

        public string? DbRepositoryPath { get; set; }
        public bool UpdateAcidFileOnSave { get; set; }

        public void Apply()
        {
            userSettings.Update(new Data
            {
                DbRepositoryPath = DbRepositoryPath,
                UpdateAcidFileOnSave = UpdateAcidFileOnSave
            });
        }

        private class Data : ISettings
        {
            public string? DbRepositoryPath { get; set; }

            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public bool UpdateAcidFileOnSave { get; set; } = true;
        }
    }
}
