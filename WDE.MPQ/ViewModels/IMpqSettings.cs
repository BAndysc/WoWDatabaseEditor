using WDE.Module.Attributes;

namespace WDE.MPQ.ViewModels
{
    [UniqueProvider]
    public interface IMpqSettings
    {
        public string? Path { get; set; }
    }
}