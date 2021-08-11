using System.Collections.Generic;
using System.ComponentModel;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.Providers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Sessions;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Providers
{
    [AutoRegister]
    public class SessionsMenuItemProvider : IMainMenuItem, INotifyPropertyChanged
    {
        public ISessionService SessionService { get; }
        public string ItemName => "_Sessions";
        public List<IMenuItem> SubItems { get; }
        public MainMenuItemSortPriority SortPriority => MainMenuItemSortPriority.PriorityNormal;
        public event PropertyChangedEventHandler? PropertyChanged;

        public SessionsMenuItemProvider(ISessionServiceViewModel sessionServiceViewModel, ISessionService sessionService)
        {
            SessionService = sessionService;
            SubItems = new();
            SubItems.Add(new ModuleMenuItem("Begin a new session", sessionServiceViewModel.NewSessionCommand));
            SubItems.Add(new ModuleMenuItem("Preview the current session", sessionServiceViewModel.PreviewCurrentCommand));
            SubItems.Add(new ModuleMenuItem("Finalize the current session", sessionServiceViewModel.SaveCurrentCurrent));
            SubItems.Add(new ModuleManuSeparatorItem());
            SubItems.Add(new CheckableModuleMenuItem("Pause session", sessionService.ToObservable(s => s.IsPaused), new DelegateCommand(
                () =>
                {
                    sessionService.IsPaused = !sessionService.IsPaused;
                }, () => sessionService.IsOpened).ObservesProperty(() => SessionService.IsOpened)));
        }
    }
}