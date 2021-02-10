using System;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Avalonia.Managers
{
    [SingleInstance]
    [AutoRegister]
    public class MessageBoxService : IMessageBoxService
    {
        public T ShowDialog<T>(IMessageBox<T> messageBox)
        {
            Console.WriteLine("Modal: " + messageBox.MainInstruction);
            return default;
        }
    }
}