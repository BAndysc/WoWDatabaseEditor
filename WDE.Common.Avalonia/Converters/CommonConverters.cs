namespace WDE.Common.Avalonia.Converters
{
    public static class CommonConverters
    {
        public static LongToBoolConverter LongToBoolConverter { get; } = new();
        public static NullConverter NullToBoolConverter { get; } = new();
        public static NullConverter InversedNullToBoolConverter { get; } = new(){Inverted = true};
    }
}