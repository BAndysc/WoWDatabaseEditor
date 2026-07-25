using System.Collections.Generic;
using WDE.Common.Services;
using WDE.DbcStore.Providers;
using WDE.Module.Attributes;

namespace WDE.DbcStore;

[AutoRegister]
[SingleInstance]
public class DbcAnalyticsSessionProps : IAnalyticsSessionPropsProvider
{
    private readonly IDbcSettingsProvider dbcSettings;

    public DbcAnalyticsSessionProps(IDbcSettingsProvider dbcSettings)
    {
        this.dbcSettings = dbcSettings;
    }

    public IEnumerable<(string key, object value)> GetSessionProps()
    {
        var settings = dbcSettings.GetSettings();
        yield return ("dbc_configured", !string.IsNullOrEmpty(settings.Path) && !settings.SkipLoading);
        yield return ("dbc_version", settings.DBCVersion.ToString());
        yield return ("dbc_locale", settings.DBCLocale.ToString());
    }
}
