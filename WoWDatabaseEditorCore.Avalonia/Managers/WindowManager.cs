using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaStyles.Controls;
using WDE.Common;
using WDE.Common.Avalonia.Components;
using WDE.Common.Avalonia.Controls.Popups;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Managers;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;
using WoWDatabaseEditorCore.Avalonia.Controls;
using WoWDatabaseEditorCore.Avalonia.Extensions;
using WoWDatabaseEditorCore.Avalonia.Views;

namespace WoWDatabaseEditorCore.Avalonia.Managers
{
    [SingleInstance]
    [AutoRegister]
    public class WindowManager : IWindowManager
    {
        private readonly IMainWindowHolder mainWindowHolder;
        private readonly PopupMenu popupMenu;
        private string? lastFolder;

        public WindowManager(IMainWindowHolder mainWindowHolder)
        {
            this.mainWindowHolder = mainWindowHolder;
            popupMenu = new PopupMenu();
        }

        private class FakeWindowWrapper : IAbstractWindowView
        {
            private readonly WeakReference<FakeWindow> window;

            public FakeWindowWrapper(WeakReference<FakeWindow> window)
            {
                this.window = window;
                if (window.TryGetTarget(out var target))
                {
                    target.Closing += (sender, args) => OnClosing?.Invoke(this);
                }
            }

            public void Activate()
            {
            }

            public bool IsMaximized => false;

            public (int x, int y) Position => (0, 0);

            public (int x, int y) Size => (100, 100);

            public (int x, int y) LogicalSize { get; set; }

            public void Reposition(int x, int y, bool isMaximized, int width, int height)
            {
            }

            public event Action<IAbstractWindowView>? OnClosing;
        }

        private class WindowWrapper : IAbstractWindowView
        {
            private readonly WeakReference<Window> window;

            public WindowWrapper(WeakReference<Window> window)
            {
                this.window = window;
                if (window.TryGetTarget(out var target))
                {
                    target.Closing += (sender, args) => OnClosing?.Invoke(this);
                }
            }

            public void Activate()
            {
                if (window.TryGetTarget(out var target))
                    target.Activate();
            }

            public bool IsMaximized
            {
                get
                {
                    if (window.TryGetTarget(out var target))
                        return target.WindowState == WindowState.Maximized;
                    return false;
                }
            }

            public (int x, int y) Position
            {
                get
                {
                    if (window.TryGetTarget(out var target))
                    {
                        var pos = target.Position;
                        return (pos.X, pos.Y);
                    }
                    return (0, 0);
                }
            }

            public (int x, int y) Size
            {
                get
                {
                    if (window.TryGetTarget(out var target))
                    {
                        // we use screen coords for size so that size is custom app scaling independent
                        var screenTopLeftPoint = target.PointToScreen(new Point(target.Position.X, target.Position.Y));
                        var screenBottomRightPoint = target.PointToScreen(new Point(target.Position.X + target.ClientSize.Width, target.Position.Y + target.ClientSize.Height));
                        var width = screenBottomRightPoint.X - screenTopLeftPoint.X;
                        var height = screenBottomRightPoint.Y - screenTopLeftPoint.Y;
                        return (width, height);
                    }
                    return (100, 100);
                }
            }

            public (int x, int y) LogicalSize
            {
                get
                {
                    if (window.TryGetTarget(out var target))
                    {
                        return ((int)target.Width, (int)target.Height);
                    }
                    return (100, 100);
                }
            }

            public void Reposition(int x, int y, bool isMaximized, int width, int height)
            {
                if (window.TryGetTarget(out var target))
                {
                    if (isMaximized)
                    {
                        target.WindowState = WindowState.Maximized;
                    }
                    else
                    {
                        var point = new PixelPoint(x, y);
                        var screen = target.Screens.ScreenFromPoint(point);
                        if (screen == null)
                            return;

                        target.Position = point;

                        var tl = target.PointToClient(new PixelPoint(x, y));
                        var br = target.PointToClient(new PixelPoint(x + width, y + height));
                        target.Width = Math.Max(br.X - tl.X, 100);
                        target.Height = Math.Max(br.Y - tl.Y, 100);
                    }
                }
            }

            public event Action<IAbstractWindowView>? OnClosing;
        }

        private class PopupWrapper : IAbstractWindowView
        {
            private readonly WeakReference<Popup> popup;

            public PopupWrapper(WeakReference<Popup> popup)
            {
                this.popup = popup;
                if (popup.TryGetTarget(out var target))
                {
                    target.Closed += (sender, args) => OnClosing?.Invoke(this);
                }
            }

            public void Activate()
            {
            }

            public bool IsMaximized => false;

            public (int x, int y) Position => (0, 0);

            public (int x, int y) Size => (100, 100);

            public (int x, int y) LogicalSize => (100, 100);

            public void Reposition(int x, int y, bool isMaximized, int width, int height)
            {
            }

            public event Action<IAbstractWindowView>? OnClosing;
        }

        public IAbstractWindowView ShowWindow(IWindowViewModel viewModel, out Task task)
        {
            try
            {
                TaskCompletionSource taskCompletionSource = new TaskCompletionSource();

                if (GlobalApplication.SingleView)
                {
                    FakeWindow view = new FakeWindow();
                    var window = new FakeWindowWrapper(new WeakReference<FakeWindow>(view));
                    view.DataContext = viewModel;
                    view.Content = viewModel;

                    Control? visualRoot;

                    if (Application.Current!.ApplicationLifetime is ISingleViewApplicationLifetime viewApp)
                        visualRoot = viewApp.MainView;
                    else
                        visualRoot = mainWindowHolder.RootWindow.GetLogicalChildren().FirstOrDefault() as MainWebView;

                    var panel = visualRoot!.GetControl<PseudoWindowsPanel>("PART_WindowsContainer");

                    task = panel.Show(view);

                    return window;
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
                    view.ShowInTaskbar = true;
                    view.ShowActivated = true;
                    if (viewModel.Icon.HasValue)
                    {
                        var bitmap = WdeImage.LoadBitmapNowOrAsync(viewModel.Icon.Value, b =>
                        {
                            if (b != null)
                            {
                                view.Icon = new WindowIcon(b);
                                view.ManagedIcon = b;
                            }
                        });
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
            }
            catch (Exception e)
            {
                LOG.LogError(e);
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

        public Task<bool> ShowDialog(IDialog viewModel, out IAbstractWindowView? window)
        {
            try
            {
                if (GlobalApplication.SingleView)
                {
                    FakeWindow view = new FakeWindow();
                    window = new FakeWindowWrapper(new WeakReference<FakeWindow>(view));
                    view.DataContext = viewModel;
                    view.Content = viewModel;

                    Control? visualRoot;

                    if (Application.Current!.ApplicationLifetime is ISingleViewApplicationLifetime viewApp)
                        visualRoot = viewApp.MainView;
                    else
                        visualRoot = mainWindowHolder.RootWindow.GetLogicalChildren().FirstOrDefault() as MainWebView;

                    var panel = visualRoot!.GetControl<PseudoWindowsPanel>("PART_WindowsContainer");

                    async Task<bool> inner()
                    {
                        var result = await panel.ShowDialog(view);
                        view.DataContext = null;
                        return result;
                    }

                    return inner();
                }
                else
                {
                    DialogWindow view = new DialogWindow();
                    window = new WindowWrapper(new WeakReference<Window>(view));
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

                    async Task<bool> inner()
                    {
                        WeakReference<IInputElement>? focusedElement = null;
                        var parentWindow = GetFocusedWindow();
                        if (TopLevel.GetTopLevel(parentWindow)?.FocusManager?.GetFocusedElement() is
                            { } focusedElement_)
                        {
                            focusedElement = new WeakReference<IInputElement>(focusedElement_);
                        }

                        var result = await view.ShowDialog<bool>(parentWindow);

                        Dispatcher.UIThread.Post(() =>
                        {
                            if (focusedElement != null &&
                                focusedElement.TryGetTarget(out var focusedElement__))
                            {
                                if (((Visual)focusedElement__).GetVisualRoot() == null)
                                    (parentWindow.GetLogicalChildren().FirstOrDefault(x => x is not ToolBar) as Control)?.Focus();
                                else
                                    focusedElement__.Focus();
                            }
                        }, DispatcherPriority.Input);

                        view.DataContext = null;
                        return result;
                    }

                    return inner();
                }
            }
            catch (Exception e)
            {
                LOG.LogError(e);
                throw;
            }
        }

        public Task<bool> ShowDialog(IDialog viewModel)
        {
            return ShowDialog(viewModel, out _);
        }

        public IAbstractWindowView ShowStandaloneDocument(IDocument document, out Task task)
        {
            var vm = new WindowViewModelDocumentWrapper(document);
            var window = ShowWindow(vm, out task);
            task.ContinueWith(_ => vm.Dispose());
            return window;
        }

        public async Task<string?> ShowFolderPickerDialog(string defaultDirectory)
        {
            if (!Directory.Exists(defaultDirectory))
                defaultDirectory = lastFolder ?? "";
            if (!Directory.Exists(lastFolder))
                lastFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            {
                var window = GetFocusedWindow();
                var folder = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
                {
                    SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(defaultDirectory)
                });
                if (folder.Count == 0)
                    return null;
                lastFolder = folder[0].Path.LocalPath;
            }
            return lastFolder;
        }

        public async Task<string?> ShowOpenFileDialog(string filter, string? defaultDirectory = null)
        {
            var f = filter.Split("|");

            var window = GetFocusedWindow();

            var result = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                SuggestedStartLocation = defaultDirectory == null ? null : await window.StorageProvider.TryGetFolderFromPathAsync(defaultDirectory),
                AllowMultiple = false,
                FileTypeFilter = f.Zip(f.Skip(1))
                    .Where((x, i) => i % 2 == 0)
                    .Select(x => new FilePickerFileType(x.First)
                    {
                        Patterns = x.Second.Split(",").Select(ext => ext.StartsWith("*") ? ext : $"*.{ext}").ToList()
                    }).ToList()
            });

            if (result.Count == 0)
                return null;

            return result[0].Path.LocalPath;
        }

        public async Task<string?> ShowSaveFileDialog(string filter, string? defaultDirectory = null, string? initialFileName = null)
        {
            var f = filter.Split("|");

            var window = GetFocusedWindow();

            var types = f.Zip(f.Skip(1))
                .Where((x, i) => i % 2 == 0)
                .Select(x => new FilePickerFileType(x.First)
                {
                    Patterns = x.Second.Split(",").Select(ext => ext.StartsWith("*") ? ext : $"*.{ext}").ToList()
                }).ToList();

            var result = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                SuggestedStartLocation = defaultDirectory == null ? null : await window.StorageProvider.TryGetFolderFromPathAsync(defaultDirectory),
                SuggestedFileName = initialFileName ?? "",
                FileTypeChoices = types,
                DefaultExtension = types.Count > 0 ? types[0].Patterns![0] : null
            });

            if (result == null)
                return null;

            return result.Path.LocalPath;
        }

        private Window GetFocusedWindow()
        {
            return ((IClassicDesktopStyleApplicationLifetime?)Application.Current!.ApplicationLifetime)?.Windows
                .FirstOrDefault(x => x.IsActive) ?? mainWindowHolder.RootWindow;
        }

        public void OpenUrl(string url, string arguments)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = url,
                Arguments = arguments
            };
            System.Diagnostics.Process.Start(psi);
        }

        public void Activate()
        {
            mainWindowHolder.RootWindow?.ActivateWorkaround();
        }

        public async Task OpenPopupMenu<T>(T menuViewModel, object? control) where T : IPopupMenuViewModel
        {
            popupMenu.DataContext = menuViewModel;
            if (control is Control c)
                await popupMenu.Open(c);
            else
            {
                if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime dekstopLifetime &&
                    dekstopLifetime.Windows.FirstOrDefault(x => x.IsActive) is { } activeWindow)
                {
                    await popupMenu.Open(activeWindow);
                }
                else
                    throw new Exception("No active window, couldn't open popup menu");
            }
        }
    }
}
