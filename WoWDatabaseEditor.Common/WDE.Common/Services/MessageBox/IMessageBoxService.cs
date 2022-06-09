using System;
using System.Threading;
using System.Threading.Tasks;

namespace WDE.Common.Services.MessageBox
{
    public interface IMessageBoxService
    {
        Task<T?> ShowDialog<T>(IMessageBox<T> messageBox);
    }

    public static class MessageBoxServiceExtensions
    {
        public static Task SimpleDialog(this IMessageBoxService service, string title, string header, string content)
        {
            return service.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle(title)
                .SetMainInstruction(header)
                .SetContent(content)
                .WithOkButton(true)
                .Build());
        }
    
        public static async Task WrapError(this IMessageBoxService service, Func<Task> task)
        {
            try
            {
                await task();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                var msg = e.Message;
                if (e.InnerException != null)
                    msg += "\n\n --> " + e.InnerException.Message;

                await service.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Error")
                    .SetMainInstruction("Error while executing the task")
                    .SetContent(msg)
                    .WithOkButton(true)
                    .Build());
            }
        }
        
        public static Func<CancellationToken, Task> WrapError(this IMessageBoxService service, Func<CancellationToken, Task> task)
        {
            return async (token) =>
            {
                try
                {
                    await task(token);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    var msg = e.Message;
                    if (e.InnerException != null)
                        msg += "\n\n --> " + e.InnerException.Message;

                    await service.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Error")
                        .SetMainInstruction("Error while executing the task")
                        .SetContent(msg)
                        .WithOkButton(true)
                        .Build());
                }
            };
        }
    }
}