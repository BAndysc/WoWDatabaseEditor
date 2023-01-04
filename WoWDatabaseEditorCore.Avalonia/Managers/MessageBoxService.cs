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
using WDE.Common.Events;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;
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
            try
            {
                if (mainWindowHolder.RootWindow != null)
                    mainWindowHolder.RootWindow.Activated += WindowOnActivated;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
                var first = pending[0];
                pending.RemoveAt(0);
                first().ListenErrors();
            }
        }

        private async Task<T?> ShowNow<T>(IMessageBox<T> messageBox)
        {
            Debug.Assert(!dialogOpened);
            dialogOpened = true;
            var viewModel = new MessageBoxViewModel<T>(messageBox);
            if (GlobalApplication.SingleView)
            {
                var tcs = new TaskCompletionSource<T?>();
                var popup = new Popup();
                popup.IsLightDismissEnabled = true;
                popup.WindowManagerAddShadowHint = true;
                bool closed = false;
                popup.DataContext = viewModel;
                popup.Child = new MessageBoxControlView() { DataContext = viewModel };
                viewModel.Close += () =>
                {
                    closed = true;
                    popup.Close();
                    tcs.SetResult(viewModel.SelectedOption);
                };
                popup.GetObservable(Popup.IsOpenProperty).Skip(1).SubscribeAction(@is =>
                {
                    if (!@is && !closed)
                    {
                        if (viewModel.CancelButtonCommand.CanExecute(null))
                            viewModel.CancelButtonCommand.Execute(null);
                        else
                        {
                            popup.IsOpen = true;
                        }
                        //closed = true;
                        //tcs.SetResult(default);
                    }
                });
                Control? visualRoot;

                if (Application.Current!.ApplicationLifetime is ISingleViewApplicationLifetime viewApp)
                    visualRoot = viewApp.MainView;
                else
                    visualRoot = mainWindowHolder.RootWindow.GetLogicalChildren().FirstOrDefault() as MainWebView;

                var panel = visualRoot!.GetControl<Panel>("PART_Overlay");
                panel.Children.Add(popup);
                popup.PlacementMode = PlacementMode.Center;
                popup.IsOpen = true;

                await tcs.Task;
            }
            else
            {
                MessageBoxView view = new MessageBoxView();
                view.DataContext = viewModel;
                await mainWindowHolder.ShowDialog<bool>(view);
            }

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