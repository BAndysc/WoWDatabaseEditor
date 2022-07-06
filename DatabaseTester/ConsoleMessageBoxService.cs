using WDE.Common.Services.MessageBox;

namespace DatabaseTester;

public class ConsoleMessageBoxService : IMessageBoxService
{
    public Task<T?> ShowDialog<T>(IMessageBox<T> messageBox)
    {
        throw new Exception(messageBox.MainInstruction + messageBox.Content);
        Console.WriteLine(messageBox.MainInstruction);
        Console.WriteLine(messageBox.Content);
        return Task.FromException<T?>(new Exception(messageBox.Content));
    }
}