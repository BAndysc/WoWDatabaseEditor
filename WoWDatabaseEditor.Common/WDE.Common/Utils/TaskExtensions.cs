using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WDE.Common.Exceptions;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;

namespace WDE.Common.Utils
{
    public static class TaskExtensions
    {
        public static void ListenErrors(this Task t,
            [CallerMemberName] string? caller = null,
            [CallerFilePath] string? callerFile = null,
            [CallerLineNumber] int? callerLineNumber = null)
        {
            if (t.IsCompleted)
            {
                if (t.IsFaulted && t.Exception is { } aggregateException)
                {
                    if (aggregateException.InnerExceptions.Count == 1)
                    {
                        if (aggregateException.InnerExceptions[0] is UserException)
                            LOG.LogWarning(aggregateException.InnerExceptions[0], "User error in {0} at {1}:{2}", caller, callerFile, callerLineNumber);
                        else
                            LOG.LogError(aggregateException.InnerExceptions[0], "Error in {0} at {1}:{2}", caller, callerFile, callerLineNumber);
                    }
                    else
                    {
                        LOG.LogError(aggregateException, "Error in {0} at {1}:{2}", caller, callerFile, callerLineNumber);
                    }
                }
                return;
            }

            t.ContinueWith(e =>
            {
                if (e.Exception != null && e.Exception.InnerExceptions.All(e => e is UserException))
                    LOG.LogWarning(e.Exception, "User error in {0} at {1}:{2}", caller, callerFile, callerLineNumber);
                else
                    LOG.LogError(e.Exception, "Error in {0} at {1}:{2}", caller, callerFile, callerLineNumber);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public static void ListenWarnings(this Task t,
            [CallerMemberName] string? caller = null,
            [CallerFilePath] string? callerFile = null,
            [CallerLineNumber] int? callerLineNumber = null)
        {
            if (t.IsCompleted)
            {
                if (t.IsFaulted && t.Exception is { } aggregateException)
                {
                    if (aggregateException.InnerExceptions.Count == 1)
                        LOG.LogWarning(aggregateException.InnerExceptions[0], "Warning in {0} at {1}:{2}", caller, callerFile, callerLineNumber);
                    else
                        LOG.LogWarning(aggregateException, "Error in {0} at {1}:{2}", caller, callerFile, callerLineNumber);
                }

                return;
            }

            t.ContinueWith(e =>
            {
                LOG.LogWarning(e.Exception, "Warning in {0} at {1}:{2}", caller, callerFile, callerLineNumber);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        
        public static Task ListenErrors(this Task t, IMessageBoxService messageBoxService,
            [CallerMemberName] string? caller = null,
            [CallerFilePath] string? callerFile = null,
            [CallerLineNumber] int? callerLineNumber = null)
        {
            if (t.IsCompleted)
            {
                if (t.IsFaulted && t.Exception is { } aggregateException)
                {
                    if (aggregateException.InnerExceptions.Count == 1)
                    {
                        if (aggregateException.InnerExceptions[0] is UserException)
                            LOG.LogWarning(aggregateException.InnerExceptions[0], "User error in {0} at {1}:{2}", caller, callerFile, callerLineNumber);
                        else
                            LOG.LogError(aggregateException.InnerExceptions[0], "Error in {0} at {1}:{2}", caller, callerFile, callerLineNumber);
                    }
                    else
                        LOG.LogError(aggregateException, "Error in {0} at {1}:{2}", caller, callerFile, callerLineNumber);

                    return messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Error")
                        .SetMainInstruction("An error occured")
                        .SetContent(aggregateException.InnerExceptions.Count == 1 ? aggregateException.InnerExceptions[0].Message : aggregateException.Message + "\n" + string.Join("\n", aggregateException.InnerExceptions.Select(x => " - " + x.Message)))
                        .WithOkButton(true)
                        .Build());
                }
                return Task.CompletedTask;
            }

            async Task CoreAsync()
            {
                try
                {
                    await t;
                }
                catch (Exception e)
                {
                    if (e is UserException)
                        LOG.LogWarning(e, "User error in {0} at {1}:{2}", caller, callerFile, callerLineNumber);
                    else
                        LOG.LogError(e, "Error in {0} at {1}:{2}", caller, callerFile, callerLineNumber);
                    await messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Error")
                        .SetMainInstruction("An error occured")
                        .SetContent(e.Message)
                        .WithOkButton(true)
                        .Build());
                }
            }

            return CoreAsync();
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