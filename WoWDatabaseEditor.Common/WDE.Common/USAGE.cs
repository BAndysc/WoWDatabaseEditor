using WDE.Common.Services;

namespace WDE.Common;

/// <summary>
/// Static access to the usage service, in the spirit of <see cref="LOG"/>,
/// so that modules can instrument single lines (i.e. a save) without threading
/// <see cref="IAnalyticsService"/> through big constructors. No-op until the
/// usage service is constructed and no-op when the user opted out.
/// Remember: never include user data in events (no database content, queries,
/// entity names, file paths nor host names).
/// </summary>
public static class USAGE
{
    public static IAnalyticsService? Service { get; set; }

    public static void Event(string eventName, params (string key, object value)[] props)
        => Service?.TrackEvent(eventName, props);

    /// <summary>Aggregated counter, see <see cref="IAnalyticsService"/> CountEvent.</summary>
    public static void Count(string eventName, params (string key, object value)[] groupProps)
        => Service?.CountEvent(eventName, groupProps);

    /// <summary>Aggregated counter adding an arbitrary amount (i.e. seconds spent).</summary>
    public static void Count(string eventName, double amount, params (string key, object value)[] groupProps)
        => Service?.CountEvent(eventName, amount, groupProps);
}
