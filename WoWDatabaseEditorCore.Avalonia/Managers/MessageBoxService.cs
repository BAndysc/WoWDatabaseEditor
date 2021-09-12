using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Events;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Avalonia.Services.MessageBoxService;
using WoWDatabaseEditorCore.Avalonia.Views;

namespace WoWDatabaseEditorCore.Avalonia.Managers
{
    [SingleInstance]
    [AutoRegister]
    public class MessageBoxService : IMessageBoxService
    {
        private readonly IMainWindowHolder mainWindowHolder;
        private bool loaded;
        private bool dialogOpened;
        private List<Func<Task>> pending = new();

        public MessageBoxService(IMainWindowHolder mainWindowHolder,
            IEventAggregator eventAggregator)
        {
            this.mainWindowHolder = mainWindowHolder;
            eventAggregator.GetEvent<AllModulesLoaded>()
                .Subscribe(OnLoaded, true);
        }

        private void OnLoaded()
        {
            mainWindowHolder.Window.Activated += WindowOnActivated;
        }

        private void WindowOnActivated(object? sender, EventArgs e)
        {
            if (loaded)
                return;
            loaded = true;
            if (pending.Count > 0)
            {
                var first = pending[0];
                pending.RemoveAt(0);
                first().ListenErrors();
            }
        }

        private async Task<T?> ShowNow<T>(IMessageBox<T> messageBox)
        {
            Debug.Assert(!dialogOpened);
            dialogOpened = true;
            MessageBoxView view = new MessageBoxView();
            var viewModel = new MessageBoxViewModel<T>(messageBox);
            view.DataContext = viewModel;
            await view.ShowDialog<bool>(mainWindowHolder.Window);
            dialogOpened = false;

            if (pending.Count > 0)
            {
                var first = pending[0];
                pending.RemoveAt(0);
                first().ListenErrors(); // no await! this is another dialog
            }
            
            return viewModel.SelectedOption;
        }
        
        public Task<T?> ShowDialog<T>(IMessageBox<T> messageBox)
        {
            if (loaded && !dialogOpened)
            {
                return ShowNow(messageBox);
            }

            TaskCompletionSource<T?> completionSource = new();
            Func<Task> f = async () =>
            {
                var result = await ShowNow(messageBox);
                completionSource.SetResult(result);
            };
            pending.Add(f);
            return completionSource.Task;
        }
    }
}