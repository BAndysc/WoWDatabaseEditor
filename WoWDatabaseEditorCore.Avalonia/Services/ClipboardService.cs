using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input.Platform;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Avalonia.Services
{
    [AutoRegister]
    public class ClipboardService : IClipboardService
    {
        private readonly IMainThread mainThread;

        public ClipboardService(IMainThread mainThread)
        {
            this.mainThread = mainThread;
        }

        private IClipboard clipboard => Application.Current?.GetTopLevel()?.Clipboard!;

        public async Task<string?> GetText()
        {
            return await mainThread.Schedule(async () =>
            {
                if (await clipboard.TryGetTextAsync() is { } text)
                    return text;
                return null;
            });
        }

        public void SetText(string text)
        {
            mainThread.Schedule(async () => clipboard.SetTextAsync(text)).ListenErrors();
        }
    }
}
