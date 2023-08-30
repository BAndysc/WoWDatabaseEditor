using System.Threading.Tasks;
using Avalonia;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Avalonia.Services
{
    [AutoRegister]
    public class ClipboardService : IClipboardService
    {
        public Task<string> GetText()
        {
            return Application.Current!.Clipboard!.GetTextAsync();
        }

        public void SetText(string text)
        {
            Application.Current!.Clipboard!.SetTextAsync(text);
        }
    }
}