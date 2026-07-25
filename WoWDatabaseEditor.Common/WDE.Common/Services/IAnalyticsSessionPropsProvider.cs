using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

/// <summary>
/// Modules can implement this to contribute configuration facts (i.e. dbc version,
/// client files setup) as properties of the "app_started" analytics event.
/// Only category-level values - never paths, host names or other user data.
/// In scoped modules remember to use [AutoRegisterToParentScope].
/// </summary>
[NonUniqueProvider]
public interface IAnalyticsSessionPropsProvider
{
    IEnumerable<(string key, object value)> GetSessionProps();
}
