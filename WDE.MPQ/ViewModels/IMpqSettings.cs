using WDE.Module.Attributes;

namespace WDE.MPQ.ViewModels
{
    [UniqueProvider]
    public interface IMpqSettings
    {
        public string? Path { get; set; }
        public MpqOpenType OpenType { get; set; }
        void Save();
    }

    public enum MpqOpenType
    {
        WowDatabaseEditor = 0,
        Stormlib = 1
    }
}