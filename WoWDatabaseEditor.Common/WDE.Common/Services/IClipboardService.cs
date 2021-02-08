using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface IClipboardService
    {
        string GetText();
        void SetText(string text);
    }
}