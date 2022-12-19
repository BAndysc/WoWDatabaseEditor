using System;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.QuickAccess;
using WDE.Common.Services;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.QuickAccess;

[AutoRegister]
[SingleInstance]
internal class QuickCommands : IQuickCommands
{
    public QuickCommands(Lazy<IQuickAccessService> service,
        Lazy<IQuickAccessViewModel> viewModel,
        IClipboardService clipboardService,
        IStatusBar statusBar)
    {
        CopyCommand = new DelegateCommand<object>(o =>
        {
            var text = o.ToString() ?? "";
            clipboardService.SetText(text);
            statusBar.PublishNotification(new PlainNotification(NotificationType.Info, "Copied " + text));
            viewModel.Value.CloseSearch();
        });

        SetSearchCommand = new DelegateCommand<object>(o =>
        {
            viewModel.Value.OpenSearch(o.ToString());
        });

        CloseSearchCommand = new DelegateCommand<object>(o =>
        {
            viewModel.Value.CloseSearch();
        });

        NoCommand = new DelegateCommand(() => { });
    }
    
    public ICommand CloseSearchCommand { get; set; }
    public ICommand CopyCommand { get; set; }
    public ICommand SetSearchCommand { get; set; }
    public ICommand NoCommand { get; set; }
    public QuickAccessItem AndMoreItem => new QuickAccessItem(ImageUri.Empty, "...and more", "", "specify a more specific search", NoCommand, null, 0);
}