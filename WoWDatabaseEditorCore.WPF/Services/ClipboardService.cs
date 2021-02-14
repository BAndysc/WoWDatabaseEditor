using System.Windows;
using System.Threading.Tasks;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.WPF.Services
{
    [AutoRegister]
    [SingleInstance]
    public class ClipboardService : IClipboardService
    {
        public Task<string> GetText()
        {
            return Task.FromResult(Clipboard.GetText());
        }

        public void SetText(string text)
        {
            Clipboard.SetText(text);
        }
    }
}