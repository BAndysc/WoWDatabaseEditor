using System;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.UserSettingsService;

[AutoRegister(Platforms.Browser)]
[SingleInstance]
public partial class WebUserSettings : IUserSettings
{
    [JSImport("localStorage.setValue", "main.js")]
    internal static partial void SetValue(string key, string value);

    [JSImport("localStorage.getValue", "main.js")]
    internal static partial string? GetValue(string key);

    private readonly JsonSerializer json;

    public WebUserSettings()
    {
        json = new() {TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.None};
        json.Converters.Add(new DatabaseKeyConverter());
    }

    private string GetSettingPath<T>()
    {
        return typeof(T).FullName!.Replace("+", ".");
    }

    public T? Get<T>(T? defaultValue = default) where T : ISettings
    {
        var settingsFile = GetSettingPath<T>();
        var value = GetValue(settingsFile);
        if (value == null)
            return defaultValue;
        try
        {
            return json.Deserialize<T>(new JsonTextReader(new StringReader(value)));
        }
        catch (Exception e)
        {
            LOG.LogError(e, message: "Error while loading settings: " + settingsFile);
            return defaultValue;
        }
    }

    public void Update<T>(T newSettings) where T : ISettings
    {
        var settingsFile = GetSettingPath<T>();
        try
        {
            using var writer = new StringWriter();
            json.Serialize(new JsonTextWriter(writer), newSettings);
            SetValue(settingsFile, writer.ToString());
        }
        catch (Exception e)
        {
            LOG.LogError(e, message: "Error while saving settings: " + settingsFile);
        }
    }
}