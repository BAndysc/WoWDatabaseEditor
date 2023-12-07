using System;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using AvaloniaEdit.Document;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SqlWorkbench.Services.Connection;

namespace WDE.SqlWorkbench.Services.QueryConfirmation;

internal class QueryConfirmationViewModel : ObservableBase, IDialog
{
    private readonly IConnection connection;
    public TextDocument Document { get; }

    public QueryConfirmationViewModel(IMessageBoxService messageBoxService,
        IConnection connection,
        string query)
    {
        this.connection = connection;
        Document = new TextDocument(query);
        
        Apply = new AsyncAutoCommand(async () =>
        {
            var session = await this.connection.OpenSessionAsync();
            await session.ExecuteSqlAsync(this.Document.Text);
            await messageBoxService.SimpleDialog("Success", "Success", "Query executed successfully!");
            CloseOk?.Invoke();
        }).WrapMessageBox<Exception>(messageBoxService);
        Cancel = new DelegateCommand(() =>
        {
            CloseCancel?.Invoke();
        });
    }
    
    public int DesiredWidth => 600;
    public int DesiredHeight => 500;
    public string Title => "Query confirmation";
    public bool Resizeable => true;
    public ICommand Accept => AlwaysDisabledCommand.Command; // not using Accept as the Apply command, because it is bound to ENTER key. we don't want accidental execution by pressing enter, do we?
    public IAsyncCommand Apply { get; }
    public ICommand Cancel { get; }
    public event Action? CloseCancel;
    public event Action? CloseOk;
}