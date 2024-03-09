using System;
using System.Threading.Tasks;
using AsyncAwaitBestPractices.MVVM;
using WDE.Common;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.SqlWorkbench.Services.UserQuestions;

[UniqueProvider]
internal interface IUserQuestionsService
{
    Task<bool> CancelAllTasksInConnectionsAsync();
    Task<SaveDialogResult> ApplyPendingChangesAsync(string title);
    Task ConnectionsErrorAsync(Exception exception);
    Task InformCantEditNonSelectAsync();
    Task SaveErrorAsync(Exception e);
    Task NoFullPrimaryKeyAsync();
    Task<bool> AskToRevertChangesAsync();
    Task<bool> ConfirmExecuteQueryAsync(string query);
    Task<string?> AskForNewViewNameAsync();
    Task<SaveDialogResult> AskSaveFileAsync();
    /// <summary>
    /// The file is big. Do you want to load anyway? It might slow down the editor.
    /// </summary>
    /// <param name="fileSize"></param>
    /// <param name="limit"></param>
    /// <returns>true to continue loading, false to stop loading</returns>
    Task<bool> FileTooBigWarningAsync(long fileSize, int limit);
    Task FileTooBigErrorAsync(long fileSize, int limit);
    Task ShowGenericErrorAsync(string header, string message); 
    Task InformEditErrorAsync(string message);
    Task InformLoadedFileTrimmedAsync(int trimmedLength);
}

internal static class CommandExtensions
{
    public static IAsyncCommand WrapMessageBox<T>(this IAsyncCommand cmd, IUserQuestionsService userQuestions,
        string? header = null) where T : Exception
    {
        return new AsyncCommandExceptionWrap<T>(cmd, async (e) =>
        {
            if (e is TaskCanceledException)
                return; // this is ok, user cancelled the task, no need to inform him again
            
            LOG.LogError(e);
            await userQuestions.ShowGenericErrorAsync(header ?? "Error occured while executing the command",
                e.Message);
        });
    }
    
    public static IAsyncCommand<R> WrapMessageBox<T, R>(this IAsyncCommand<R> cmd, IUserQuestionsService userQuestions, string? header = null) where T : Exception
    {
        return new AsyncCommandExceptionWrap<T, R>(cmd, async (e) =>
        {
            if (e is TaskCanceledException)
                return; // this is ok, user cancelled the task, no need to inform him again
            
            LOG.LogError(e);
            await userQuestions.ShowGenericErrorAsync(header ?? "Error occured while executing the command", e.Message);
        });
    }
}