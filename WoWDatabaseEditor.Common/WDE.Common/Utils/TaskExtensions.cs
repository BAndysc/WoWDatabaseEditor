using System;
using System.Threading.Tasks;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;

namespace WDE.Common.Utils
{
    public static class TaskExtensions
    {
        public static Task ListenErrors(this Task t)
        {
            return t.ContinueWith(e =>
            {
                Console.WriteLine(e.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        
        public static Task ListenErrors(this Task t, IMessageBoxService messageBoxService)
        {
            async Task CoreAsync()
            {
                try
                {
                    await t;
                }
                catch (Exception e)
                {
                    await messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Error")
                        .SetMainInstruction("An error occured")
                        .SetContent(e.Message)
                        .WithOkButton(true)
                        .Build());
                    throw;
                }
            }
            return CoreAsync().ListenErrors();
        }

        public static async Task WrapSafe(this Task t, Action onBefore, Action onFinally)
        {
            onBefore();
            try
            {
                await t;
            }
            finally
            {
                onFinally();
            }
        }
        
        public static void IgnoreResult(this Task t)
        {
            t.ContinueWith(e =>
            {
                var x = e.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public static Progress<(long, long?)> ToProgress(this ITaskProgress taskProgress)
        {
            return new Progress<(long downloaded, long? totalBytes)>((v) =>
            {
                var isDownloaded = (v.totalBytes.HasValue && v.totalBytes.Value == v.downloaded) ||
                                   v.downloaded == -1;
                var isStatusKnown = v.totalBytes.HasValue;
                var currentProgress = v.totalBytes.HasValue ? (int) v.downloaded : (v.downloaded < 0 ? 1 : 0);
                var maxProgress = v.totalBytes ?? 1;
                    
                if (taskProgress.State == TaskState.InProgress)
                {
                    taskProgress.Report(currentProgress, (int)maxProgress, isDownloaded ? 
                        "finished" : 
                        (isStatusKnown ? $"{v.downloaded / 1_000_000f:0.00}/{maxProgress / 1_000_000f:0.00}MB" : $"{v.downloaded / 1_000_000f:0.00}MB"));                        
                }
            });
        }
    }
}