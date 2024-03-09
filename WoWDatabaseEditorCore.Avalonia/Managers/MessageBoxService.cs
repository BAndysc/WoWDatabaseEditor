using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Prism.Events;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;
using WoWDatabaseEditorCore.Avalonia.Controls;
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
        private HashSet<Window> dialogOpened = new();
        private List<(Func<Task>, Window)> pending = new();

        public MessageBoxService(IMainWindowHolder mainWindowHolder,
            IEventAggregator eventAggregator)
        {
            this.mainWindowHolder = mainWindowHolder;
            eventAggregator.GetEvent<AllModulesLoaded>()
                .Subscribe(OnLoaded, true);
        }

        private void OnLoaded()
        {
            try
            {
                if (mainWindowHolder.RootWindow != null)
                    mainWindowHolder.RootWindow.Activated += WindowOnActivated;
            }
            catch (Exception e)
            {
                LOG.LogError(e);
                throw;
            }
        }

        private void WindowOnActivated(object? sender, EventArgs e)
        {
            if (loaded)
                return;
            loaded = true;
            if (pending.Count > 0)
            {
                HashSet<Window> uniqueWindows = new();
                foreach (var (task, owner) in pending.ToList())
                {
                    if (uniqueWindows.Add(owner))
                    {
                        pending.Remove((task, owner));
                        task().ListenErrors();
                    }
                }
            }
        }

        private async Task<T?> ShowNow<T>(IMessageBox<T> messageBox, Window owner)
        {
            Debug.Assert(!dialogOpened.Contains(owner));
            dialogOpened.Add(owner);
            var viewModel = new MessageBoxViewModel<T>(messageBox);
            if (GlobalApplication.SingleView)
            {
                var view = new MessageBoxControlView() { DataContext = viewModel };
                viewModel.Close += () =>
                {
                };
                Control? visualRoot;

                if (Application.Current!.ApplicationLifetime is ISingleViewApplicationLifetime viewApp)
                    visualRoot = viewApp.MainView;
                else
                    visualRoot = mainWindowHolder.RootWindow.GetLogicalChildren().FirstOrDefault() as MainWebView;

                var panel = visualRoot!.GetControl<PseudoWindowsPanel>("PART_WindowsContainer");

                await panel.ShowDialog(new FakeWindow()
                {
                    Content = view,
                    DataContext = viewModel
                });
            }
            else
            {
                MessageBoxView view = new MessageBoxView();
                view.DataContext = viewModel;
                await mainWindowHolder.ShowDialog<bool>(view);
            }

            dialogOpened.Remove(owner);

            if (pending.Count > 0)
            {
                var indexOfAnotherDialog = pending.FindIndex(x => x.Item2 == owner);
                if (indexOfAnotherDialog == -1)
                    return viewModel.SelectedOption;

                var pendingTask = pending[indexOfAnotherDialog];
                pending.RemoveAt(indexOfAnotherDialog);
                pendingTask.Item1().ListenErrors(); // no await! this is another dialog
            }

            return viewModel.SelectedOption;
        }
        
        public Task<T?> ShowDialog<T>(IMessageBox<T> messageBox)
        {
            var topWindow = mainWindowHolder.TopWindow;

            if (loaded && !dialogOpened.Contains(topWindow))
            {
                return ShowNow(messageBox, topWindow);
            }

            TaskCompletionSource<T?> completionSource = new();
            Func<Task> f = async () =>
            {
                var result = await ShowNow(messageBox, topWindow);
                completionSource.SetResult(result);
            };
            pending.Add((f, topWindow));
            return completionSource.Task;
        }
    }
}