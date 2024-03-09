using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface IClipboardService
    {
        Task<string?> GetText();
        void SetText(string text);
    }
}