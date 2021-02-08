using System.Windows;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditor.Services
{
    [AutoRegister]
    [SingleInstance]
    public class ClipboardService : IClipboardService
    {
        public string GetText()
        {
            return Clipboard.GetText();
        }

        public void SetText(string text)
        {
            Clipboard.SetText(text);
        }
    }
}