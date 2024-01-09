using System;
using System.Threading.Tasks;
using WDE.Common.Providers;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;

namespace WDE.SqlWorkbench.Services.UserQuestions;

[AutoRegister]
[SingleInstance]
internal class UserQuestionsService : IUserQuestionsService
{
    private readonly IMessageBoxService messageBoxService;
    private readonly IInputBoxService inputBoxService;

    public UserQuestionsService(IMessageBoxService messageBoxService,
        IInputBoxService inputBoxService)
    {
        this.messageBoxService = messageBoxService;
        this.inputBoxService = inputBoxService;
    }
    
    public async Task<bool> CancelAllTasksInConnectionsAsync()
    {
        return await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("Question")
            .SetMainInstruction("Do you want to cancel all tasks in the connection?")
            .SetContent(
                "The current running task doesn't belong to this tab, however it does belong to some another tab which shares the same connection. Do you want to stop all tasks in the connection?")
            .WithYesButton(true)
            .WithCancelButton(false)
            .Build());
    }

    public Task<SaveDialogResult> ApplyPendingChangesAsync(string title)
    {
        return messageBoxService.ShowDialog(new MessageBoxFactory<SaveDialogResult>()
            .SetTitle("Warning")
            .SetMainInstruction($"{title} has pending changes. Do you want to apply them?")
            .SetContent("If you don't apply them, they will be lost.")
            .WithYesButton(SaveDialogResult.Save)
            .WithNoButton(SaveDialogResult.DontSave)
            .WithCancelButton(SaveDialogResult.Cancel)
            .Build());
    }

    public Task ConnectionsErrorAsync(Exception e)
    {
        return messageBoxService.SimpleDialog("Error", "Couldn't open MySQL connection", e.Message);
    }

    public Task InformCantEditNonSelectAsync()
    {
        return messageBoxService.SimpleDialog("Error",
            "Can't edit this query",
            "You can't edit cells in this query, because this is not a simple SELECT query.");
    }

    public Task SaveErrorAsync(Exception e)
    {
        return messageBoxService.SimpleDialog("Error", "Error while saving changes", e.Message);
    }

    public Task NoFullPrimaryKeyAsync()
    {
        return messageBoxService.SimpleDialog("Error",
            "Can't edit this query",
            "You can't edit cells in this query, because this SELECT doesn't have a full primary key.");
    }

    public Task<bool> AskToRevertChangesAsync()
    {
        return messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("Warning")
            .SetMainInstruction("Do you want to revert changes?")
            .SetContent("You have unsaved changes. Do you want to forget them?")
            .WithYesButton(true)
            .WithCancelButton(false)
            .Build());
    }

    public Task<bool> ConfirmExecuteQueryAsync(string query)
    {
        return messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("Confirm?")
            .SetMainInstruction("Do you want to execute the query?")
            .SetContent("The following query will be executed:\n\n" + query)
            .WithYesButton(true)
            .WithCancelButton(false)
            .Build());
    }

    public Task<string?> AskForNewViewNameAsync()
    {
        return inputBoxService.GetString("New view name", "Enter new view name", "NewView");
    }

    public async Task<SaveDialogResult> AskSaveFileAsync()
    {
        return await messageBoxService.ShowDialog(new MessageBoxFactory<SaveDialogResult>()
            .SetTitle("Warning")
            .SetMainInstruction("Do you want to save changes?")
            .SetContent("You have unsaved changes. Do you want to save them?")
            .WithYesButton(SaveDialogResult.Save)
            .WithNoButton(SaveDialogResult.DontSave)
            .WithCancelButton(SaveDialogResult.Cancel)
            .Build());
    }

    public async Task<bool> FileTooBigWarningAsync(long fileSize, int limit)
    {
        return await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("Warning")
            .SetMainInstruction("The file is big. Do you want to load it anyway?")
            .SetContent(
                $"The file you are trying to load is {fileSize/1024/1024} MB big. It might lag the editor. Do you want to continue?")
            .WithYesButton(true)
            .WithNoButton(false)
            .Build());
    }

    public async Task FileTooBigErrorAsync(long fileSize, int limit)
    {
        await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("Error")
            .SetMainInstruction("The file is too big")
            .SetContent(
                $"The file you are trying to load is {fileSize/1024/1024} MB big. The allowed limit is {limit/1024/1024} MB in order to prevent editor crashes and slowdowns.")
            .WithOkButton(true)
            .Build());
    }

    public async Task ShowGenericErrorAsync(string header, string message)
    {
        await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("Error")
            .SetMainInstruction(header)
            .SetContent(message)
            .WithOkButton(true)
            .Build());
    }

    public async Task InformEditErrorAsync(string message)
    {
        await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("Error")
            .SetMainInstruction("Error while editing")
            .SetContent(message)
            .WithOkButton(true)
            .Build());
    }

    public async Task InformLoadedFileTrimmedAsync(int trimmedLength)
    {
        await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("Warning")
            .SetMainInstruction("The file was trimmed")
            .SetContent($"The file was trimmed to {trimmedLength} bytes, because that's the column width.")
            .WithOkButton(true)
            .Build());
    }
}