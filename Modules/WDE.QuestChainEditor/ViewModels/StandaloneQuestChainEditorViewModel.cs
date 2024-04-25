using System;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.QuestChainEditor.Documents;

namespace WDE.QuestChainEditor.ViewModels;

public class StandaloneQuestChainEditorViewModel : ObservableBase, IDialog, IWindowViewModel, IClosableDialog
{
    private readonly IMessageBoxService messageBoxService;

    public IAsyncCommand SaveCommand { get; }
    public IAsyncCommand GenerateQueryCommand { get; }

    public QuestChainDocumentViewModel ViewModel { get; }

    public StandaloneQuestChainEditorViewModel(
        IContainerProvider containerProvider,
        IMessageBoxService messageBoxService,
        IWindowManager windowManager,
        ITextDocumentService textDocumentService,
        QuestChainSolutionItem solutionItem
        )
    {
        this.messageBoxService = messageBoxService;
        ViewModel = containerProvider.Resolve<QuestChainDocumentViewModel>((typeof(QuestChainSolutionItem), solutionItem));
        SaveCommand = new AsyncAutoCommand(async () =>
        {
            await ViewModel.Save.ExecuteAsync();
        }).WrapMessageBox<Exception>(messageBoxService);
        GenerateQueryCommand = new AsyncAutoCommand(async () =>
        {
            windowManager.ShowStandaloneDocument(textDocumentService.CreateDocument("SQL Query", (await ViewModel.GenerateQuery()).QueryString, "sql", false), out _);
        }).WrapMessageBox<Exception>(messageBoxService);
        Accept = new AsyncAutoCommand(async () =>
        {
            await ViewModel.Save.ExecuteAsync();
            CloseOk?.Invoke();
        });
        Cancel = NullCommand.Command; // do nothing on Escape, becuase this is a standalone editor, not a dialog
    }

    private async Task<bool> AskToSave()
    {
        if (ViewModel.IsModified)
        {
            var result = await messageBoxService.ShowDialog(new MessageBoxFactory<SaveDialogResult>()
                .SetTitle("Save changes?")
                .SetMainInstruction("Do you want to save changes?")
                .SetContent("The quest chain is modified. Unsaved changes will be lost.")
                .WithYesButton(SaveDialogResult.Save)
                .WithNoButton(SaveDialogResult.DontSave)
                .WithCancelButton(SaveDialogResult.Cancel)
                .Build());

            if (result == SaveDialogResult.Cancel)
                return false;

            if (result == SaveDialogResult.Save)
                await ViewModel.Save.ExecuteAsync();
        }

        return true;
    }

    public int DesiredWidth => 1000;
    public int DesiredHeight => 800;
    public string Title => "Quest Chain Editor";
    public bool Resizeable => true;
    public ICommand Accept { get; set; }
    public ICommand Cancel { get; set; }
    public event Action? CloseCancel;
    public event Action? CloseOk;
    public ImageUri? Icon { get; set; }

    public override void Dispose()
    {
        base.Dispose();
        ViewModel?.Dispose();
    }

    public void OnClose()
    {
        async Task AskToSaveAndClose()
        {
            if (!await AskToSave())
                return;

            CloseCancel?.Invoke();
        }
        AskToSaveAndClose().ListenErrors();
    }

    public event Action? Close;
}