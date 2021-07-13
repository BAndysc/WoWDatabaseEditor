using System;
using System.Threading.Tasks;
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