using System;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.AnalyticsUsage;

[SingleInstance]
[AutoRegister]
public class UsageConfigService
{
    private readonly IUserSettings userSettings;

    private Config config;

    public event Action? EnabledChanged;

    /// <summary>null means the user was not asked for consent yet</summary>
    public bool? Enabled
    {
        get => config.Enabled;
        set
        {
            config.Enabled = value;
            Save();
            EnabledChanged?.Invoke();
        }
    }

    /// <summary>
    /// A random, persistent identifier of this installation, used only when
    /// the build has no UPDATE_KEY (i.e. self-compiled builds).
    /// </summary>
    public Guid InstallId
    {
        get
        {
            if (config.InstallId == Guid.Empty)
            {
                config.InstallId = Guid.NewGuid();
                Save();
            }
            return config.InstallId;
        }
    }

    public UsageConfigService(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        config = userSettings.Get<Config>();
    }

    private void Save()
    {
        userSettings.Update(config);
    }

    public struct Config : ISettings
    {
        public bool? Enabled { get; set; }
        public Guid InstallId { get; set; }
    }
}
