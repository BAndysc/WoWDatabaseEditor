using System;
using System.Threading.Tasks;

namespace WDE.Common.Services.MessageBox
{
    public interface IMessageBoxService
    {
        Task<T?> ShowDialog<T>(IMessageBox<T> messageBox);
    }

    public static class MessageBoxServiceExtensions
    {
        public static async Task WrapError(this IMessageBoxService service, Func<Task> task)
        {
            try
            {
                await task();
            }
            catch (Exception e)
            {
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
    }
}