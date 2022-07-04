namespace WDE.SmartScriptEditor.Editor
{
    public interface IEditorFeatures
    {
        string Name { get; }
        bool SupportsSource { get; }
        bool SupportsEventCooldown { get; }
        bool SupportsTargetCondition { get; }
        bool SupportsEventTimerId { get; }
        bool SourceHasPosition { get; }
        ParametersCount EventParametersCount { get; }
        ParametersCount ActionParametersCount { get; }
        ParametersCount TargetParametersCount { get; }
    }

    public readonly struct ParametersCount
    {
        public readonly int IntCount;
        public readonly int FloatCount;
        public readonly int StringCount;

        public ParametersCount(int intCount, int floatCount, int stringCount)
        {
            IntCount = intCount;
            FloatCount = floatCount;
            StringCount = stringCount;
        }
    }
}