using System;
using System.Threading.Tasks;
using Avalonia;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Avalonia.Services
{
    [AutoRegister]
    public class ClipboardService : IClipboardService
    {
        public async Task<string?> GetText()
        {
            return (await Application.Current!.GetTopLevel()!.Clipboard!.GetTextAsync());
        }

        public void SetText(string text)
        {     
            Application.Current!.GetTopLevel()!.Clipboard!.SetTextAsync(text);
        }
    }
}
