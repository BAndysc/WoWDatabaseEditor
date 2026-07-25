using System.Collections.Generic;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.MPQ.ViewModels;

namespace WDE.MPQ.Services;

[AutoRegister]
[SingleInstance]
public class MpqAnalyticsSessionProps : IAnalyticsSessionPropsProvider
{
    private readonly IMpqSettings mpqSettings;

    public MpqAnalyticsSessionProps(IMpqSettings mpqSettings)
    {
        this.mpqSettings = mpqSettings;
    }

    public IEnumerable<(string key, object value)> GetSessionProps()
    {
        yield return ("client_files_configured", !string.IsNullOrEmpty(mpqSettings.Path));
        yield return ("client_files_lib", mpqSettings.OpenType.ToString());
    }
}
