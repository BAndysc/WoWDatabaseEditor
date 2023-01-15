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
                    ((DockPanel)mainWindowHolder.Window.GetVisualRoot().VisualChildren[0].VisualChildren[0].VisualChildren[0]).Children.Add(popup);
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
                    return await mainWindowHolder.ShowDialog<bool>(view);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public Task<string?> ShowFolderPickerDialog(string defaultDirectory)
        {
            return new OpenFolderDialog() {Directory = defaultDirectory}.ShowAsync(mainWindowHolder.Window);
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
            }.ShowAsync(mainWindowHolder.Window);
            
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
            }.ShowAsync(mainWindowHolder.Window);
            
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
            mainWindowHolder.Window?.ActivateWorkaround();
        }
    }
}