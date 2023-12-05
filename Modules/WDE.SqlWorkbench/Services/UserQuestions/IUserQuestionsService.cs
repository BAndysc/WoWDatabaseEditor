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
}