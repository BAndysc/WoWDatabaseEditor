using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    /// <summary>
    /// Anonymous usage analytics. Events must never contain user data:
    /// no database content, no queries, no file paths, no host names.
    /// Only category-level facts (i.e. "a creature_template editor was opened").
    /// </summary>
    [UniqueProvider]
    public interface IAnalyticsService
    {
        bool Enabled { get; }
        void TrackEvent(string eventName, params (string key, object value)[] props);

        /// <summary>
        /// Increments an in-memory counter grouped by (eventName, groupProps)
        /// and the wall-clock time window the increment happened in.
        /// Counters are flushed periodically (and on exit) as a single event
        /// per group with an additional "count" property, timestamped with the
        /// window the increments belong to - so frequent actions don't produce
        /// an event per occurrence, yet time-bucketed aggregations stay exact.
        /// </summary>
        void CountEvent(string eventName, params (string key, object value)[] groupProps);

        /// <summary>Same as the parameterless-amount CountEvent, but adds an arbitrary amount (i.e. seconds spent).</summary>
        void CountEvent(string eventName, double amount, params (string key, object value)[] groupProps);
    }

    /// <summary>
    /// A document or tool can implement this to control the name it is reported as
    /// in the anonymous usage analytics (the default is the type name).
    /// </summary>
    public interface IAnalyticsNameProvider
    {
        string? AnalyticsName { get; }
    }
}
