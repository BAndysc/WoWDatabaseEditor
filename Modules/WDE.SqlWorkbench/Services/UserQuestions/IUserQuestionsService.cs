using System;
using System.Threading.Tasks;
using WDE.Common.Services.MessageBox;
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
}