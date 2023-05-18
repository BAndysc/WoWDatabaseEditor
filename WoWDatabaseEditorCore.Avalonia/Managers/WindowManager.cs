using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using WDE.Common.Avalonia.Components;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;
using WoWDatabaseEditorCore.Avalonia.Extensions;
using WoWDatabaseEditorCore.Avalonia.Views;

namespace WoWDatabaseEditorCore.Avalonia.Managers
{
    [SingleInstance]
    [AutoRegister]
    public class WindowManager : IWindowManager
    {
        private readonly bool UseExperimentalPopup = false;
        private readonly IMainWindowHolder mainWindowHolder;

        public WindowManager(IMainWindowHolder mainWindowHolder)
        {
            this.mainWindowHolder = mainWindowHolder;
        }

        private class WindowWrapper : IAbstractWindowView
        {
            private readonly WeakReference<Window> window;

            public WindowWrapper(WeakReference<Window> window)
            {
                this.window = window;
            }
            
            public void Activate()
            {
                if (window.TryGetTarget(out var target))
                    target.Activate();
            }
        }

        public IAbstractWindowView ShowWindow(IWindowViewModel viewModel, out Task task)
        {
            try
            {
                TaskCompletionSource taskCompletionSource = new TaskCompletionSource();
                DialogWindow view = new DialogWindow();
                if (viewModel.AutoSize)
                {
                    view.MinHeight = viewModel.DesiredHeight;
                    view.MinWidth = viewModel.DesiredWidth;
                }
                else
                {
                    view.Height = viewModel.DesiredHeight;
                    view.Width = viewModel.DesiredWidth;
                }
                view.DataContext = viewModel;
                view.ShowInTaskbar = true;
                view.ShowActivated = true;
                if (viewModel.Icon.HasValue)
                {
                    var bitmap = WdeImage.LoadBitmap(viewModel.Icon.Value);
                    if (bitmap != null)
                    {
                        view.Icon = new WindowIcon(bitmap);
                        view.ManagedIcon = bitmap;
                    }
                }
                view.Tag = taskCompletionSource;
                view.Closed += StandaloneWindowClosed;
                view.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                view.Show();
                task = taskCompletionSource.Task;
                return new WindowWrapper(new WeakReference<Window>(view));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void StandaloneWindowClosed(object? sender, EventArgs e)
        {
            var window = ((DialogWindow)sender!);
            var task = (TaskCompletionSource)window.Tag!;
            window.Closed -= StandaloneWindowClosed;
            window.DataContext = null;
            task.SetResult();
        }

        public async Task<bool> ShowDialog(IDialog viewModel)
        {
            try
            {
                if (UseExperimentalPopup)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    var popup = new Popup();
                    popup.IsLightDismissEnabled = true;
                    bool closed = false;
                    popup.Height = viewModel.DesiredHeight;
                    popup.Width = viewModel.DesiredWidth;
                    popup.DataContext = viewModel;
                    var pres = new ContentPresenter();
                    ViewBind.SetModel(pres, viewModel);
                    popup.Child = new Border(){Child = pres, Background = Brushes.White, BorderThickness = new Thickness(1), BorderBrush = Brushes.DarkGray};
                    viewModel.CloseCancel += () =>
                    {
                        closed = true;
                        popup.Close();
                        tcs.SetResult(false);
                    };
                    viewModel.CloseOk += () =>
                    {
                        closed = true;
                        popup.Close();
                        tcs.SetResult(true);
                    };
                    popup.GetObservable(Popup.IsOpenProperty).Skip(1).SubscribeAction(@is =>
                    {
                        if (!@is && !closed)
                        {
                            closed = true;
                            tcs.SetResult(false);
                        }
                    });
                    if (FocusManager.Instance.Current is Control c)
                    {
                        popup.PlacementTarget = c;
                    }
                    ((DockPanel)mainWindowHolder.RootWindow.GetVisualRoot().VisualChildren[0].VisualChildren[0].VisualChildren[0]).Children.Add(popup);
                    popup.PlacementMode = PlacementMode.Pointer;
                    popup.IsOpen = true;
                    return await tcs.Task;
                }
                else
                { 
                    DialogWindow view = new DialogWindow();
                    if (viewModel.AutoSize)
                    {
                        view.MinHeight = viewModel.DesiredHeight;
                        view.MinWidth = viewModel.DesiredWidth;
                    }
                    else
                    {
                        view.Height = viewModel.DesiredHeight;
                        view.Width = viewModel.DesiredWidth;
                    }
                    view.DataContext = viewModel;
                    var result = await mainWindowHolder.ShowDialog<bool>(view);
                    view.DataContext = null;
                    return result;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public IAbstractWindowView ShowStandaloneDocument(IDocument document, out Task task)
        {
            var vm = new WindowViewModelDocumentWrapper(document);
            var window = ShowWindow(vm, out task);
            task.ContinueWith(_ => vm.Dispose());
            return window;
        }

        public Task<string?> ShowFolderPickerDialog(string defaultDirectory)
        {
            return new OpenFolderDialog() {Directory = defaultDirectory}.ShowAsync(mainWindowHolder.RootWindow);
        }

        public async Task<string?> ShowOpenFileDialog(string filter, string? defaultDirectory = null)
        {
            var f = filter.Split("|");
            
            var result = await new OpenFileDialog()
            {
                Directory = defaultDirectory,
                AllowMultiple = false,
                Filters = f.Zip(f.Skip(1))
                    .Where((x, i) => i % 2 == 0)
                    .Select(x => new FileDialogFilter()
                    {
                        Name = x.First,
                        Extensions = x.Second.Split(",").ToList()
                    }).ToList()
            }.ShowAsync(mainWindowHolder.RootWindow);
            
            if (result == null || result.Length == 0)
                return null;
            
            return result[0];
        }

        public async Task<string?> ShowSaveFileDialog(string filter, string? defaultDirectory = null, string? initialFileName = null)
        {
            var f = filter.Split("|");
            
            var result = await new SaveFileDialog()
            {
                Directory = defaultDirectory,
                InitialFileName = initialFileName ?? "",
                Filters = f.Zip(f.Skip(1))
                    .Where((x, i) => i % 2 == 0)
                    .Select(x => new FileDialogFilter()
                    {
                        Name = x.First,
                        Extensions = x.Second.Split(",").ToList()
                    }).ToList()
            }.ShowAsync(mainWindowHolder.RootWindow);
            
            if (string.IsNullOrEmpty(result))
                return null;
            
            return result;
        }
        
        public void OpenUrl(string url)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = url
            };
            System.Diagnostics.Process.Start(psi);
        }

        public void Activate()
        {
            mainWindowHolder.RootWindow?.ActivateWorkaround();
        }
    }
}