using WDE.Common.Services.MessageBox;

namespace DatabaseTester;

public class ConsoleMessageBoxService : IMessageBoxService
{
    public Task<T?> ShowDialog<T>(IMessageBox<T> messageBox)
    {
        throw new Exception(messageBox.MainInstruction + messageBox.Content);
    }
}