using System;
using System.Collections.Generic;
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
            loaded = true;
            ExecutePending().ListenErrors();
        }

        private async Task ExecutePending()
        {
            while (pending.Count > 0)
            {
                await pending[0]();
                pending.RemoveAt(0);
            }
        }

        private async Task<T?> ShowNow<T>(IMessageBox<T> messageBox)
        {
            MessageBoxView view = new MessageBoxView();
            var viewModel = new MessageBoxViewModel<T>(messageBox);
            view.DataContext = viewModel;
            await view.ShowDialog<bool>(mainWindowHolder.Window);
            return viewModel.SelectedOption;
        }
        
        public Task<T?> ShowDialog<T>(IMessageBox<T> messageBox)
        {
            if (loaded)
                return ShowNow(messageBox);
            
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