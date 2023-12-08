using System;
using System.Threading.Tasks;
using WDE.Common.Services.MessageBox;

namespace CrashReport;

public class MessageBoxService : IMessageBoxService
{
    public async Task<T?> ShowDialog<T>(IMessageBox<T> messageBox)
    {
        Console.WriteLine(messageBox.MainInstruction);
        Console.WriteLine(messageBox.Content);
        return default;
    }
}