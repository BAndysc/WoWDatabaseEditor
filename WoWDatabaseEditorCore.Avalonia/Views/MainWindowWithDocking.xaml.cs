using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaStyles;
using AvaloniaStyles.Controls;
using Dock.Avalonia.Controls;
using Newtonsoft.Json;
using Prism.Events;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WoWDatabaseEditorCore.Avalonia.Clippy;
using WoWDatabaseEditorCore.Avalonia.Docking;
using WoWDatabaseEditorCore.Avalonia.Docking.Serialization;
using WoWDatabaseEditorCore.Settings;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    public class MainWindowWithDocking : ExtendedWindow
    {
        private static string DockSettingsFile = "~/dock.ava.layout";
        private readonly IFileSystem fileSystem;
        private readonly IUserSettings userSettings;
        private readonly IEventAggregator eventAggregator;
        private readonly IParserViewerSolutionItemService parserViewerSolutionItemService;
        private AvaloniaDockAdapter avaloniaDockAdapter;

        public static readonly AttachedProperty<bool> OnEnterPressedProperty = 
            AvaloniaProperty.RegisterAttached<CompletionComboBox, bool>("OnEnterPressed", typeof(MainWindowWithDocking));

        public MainWindowWithDocking()
        {
            fileSystem = null!;
            userSettings = null!;
            avaloniaDockAdapter = null!;
            eventAggregator = null!;
            parserViewerSolutionItemService = null!;
        }
        
        public static bool GetOnEnterPressed(AvaloniaObject obj)
        {
            return (bool?)obj.GetValue(OnEnterPressedProperty) ?? false;
        }

        public static void SetOnEnterPressed(AvaloniaObject obj, bool value)
        {
            obj.SetValue(OnEnterPressedProperty, value);
        }

        static MainWindowWithDocking()
        {
            OnEnterPressedProperty.Changed.AddClassHandler<CompletionComboBox>((box, args) =>
            {
                box.OnEnterPressed += (sender, pressedArgs) =>
                {
                    var box = (CompletionComboBox)sender!;
                    if (pressedArgs.SelectedItem == null && box.SelectedItem != null && string.IsNullOrEmpty(pressedArgs.SearchText))
                    {
                        var oldItem = box.SelectedItem;
                        box.SelectedItem = null;
                        box.SelectedItem = oldItem; // reselect the same item on subsequent enter press
                        pressedArgs.Handled = true;
                    }
                    else if (pressedArgs.SelectedItem == null && long.TryParse(pressedArgs.SearchText, out var l))
                    {
                        var firstItem = box.Items?.Cast<QuickGoToItemViewModel>().FirstOrDefault(x => x.Key == l);
                        box.SelectedItem = firstItem ?? new QuickGoToItemViewModel(l, "(unknown)");
                        pressedArgs.Handled = true;
                    }
                };
            });
        }
        
        public MainWindowWithDocking(IMainWindowHolder mainWindowHolder, 
            IDocumentManager documentManager, 
            ILayoutViewModelResolver layoutViewModelResolver,
            IFileSystem fileSystem,
            IUserSettings userSettings,
            IEventAggregator eventAggregator,
            IParserViewerSolutionItemService parserViewerSolutionItemService,
            TempToolbarButtonStyleService tempToolbarButtonStyleService)
        {
            this.fileSystem = fileSystem;
            this.userSettings = userSettings;
            this.eventAggregator = eventAggregator;
            this.parserViewerSolutionItemService = parserViewerSolutionItemService;
            avaloniaDockAdapter = new AvaloniaDockAdapter(documentManager, layoutViewModelResolver);
            
            // we have to do it before InitializeComponent!
            var scaling = Screens.All.Select(f => f.Scaling).Max();
            if (OperatingSystem.IsMacOS())
            {
                // https://github.com/AvaloniaUI/Avalonia/commit/c0276f75b9213e8ac90cda97f18f93b397e7a3c4
                // macOS always returns 1.0, but it is not true, if we can't detect it, at least we can set it to 2.0
                scaling = 2;
            }
            GlobalApplication.HighDpi = scaling >= 1.5f; // enable hidpi res for everyone? check if it results in higher quality icons
            
            InitializeComponent();
            this.AttachDevTools();
            mainWindowHolder.RootWindow = this;
            if (SystemTheme.EffectiveTheme is SystemThemeOptions.LightWindows11 or SystemThemeOptions.DarkWindows11)
            {
                this.Classes.Add("win11");
            }
            else
            {
                this.Classes.Add("win10");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ExtendClientAreaToDecorationsHint = true;
                Chrome = ExtendedWindowChrome.NoSystemChrome;
            }
            PersistentDockDataTemplate.DocumentManager = documentManager;

            tempToolbarButtonStyleService.ToObservable(x => x.Style)
                .SubscribeAction(style =>
                {
                    Application.Current!.Resources["DisplayButtonImageIcon"] = style is ToolBarButtonStyle.Icon or ToolBarButtonStyle.IconAndText;
                    Application.Current!.Resources["DisplayButtonImageText"] = style is ToolBarButtonStyle.Text or ToolBarButtonStyle.IconAndText;
                });

            bool once = true;
            this.Activated += (_, __) =>
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.Activated(once, SystemTheme.EffectiveTheme == SystemThemeOptions.Windows9x);
                    once = false;
                }
            };

            DragDrop.SetAllowDrop(this, true);
            AddHandler(DragDrop.DragEnterEvent, DragEnter);
            AddHandler(DragDrop.DragOverEvent, DragOver);
            AddHandler(DragDrop.DropEvent, Drop);
        }

        public static ImageUri DocumentIcon => new ImageUri("Icons/document.png");

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            AddHandler(KeyUpEvent, OnKeyUpTunneled, RoutingStrategies.Tunnel);
            
            DockControl dock = this.GetControl<DockControl>("DockControl");
            
            SerializedDock? serializedDock = null;
            if (fileSystem.Exists(DockSettingsFile))
            {
                try
                {
                    serializedDock = JsonConvert.DeserializeObject<SerializedDock>(fileSystem.ReadAllText(DockSettingsFile));
                } catch {}
            }

            dock.Layout = avaloniaDockAdapter.Initialize(serializedDock);

            var lastSize = userSettings.Get<WindowLastSize>();
            
            if (lastSize.LastX.HasValue && lastSize.LastY.HasValue && lastSize.LastWidth.HasValue &&
                lastSize.LastHeight.HasValue)
            {
                var p = new PixelPoint(lastSize.LastX.Value + 20, lastSize.LastY.Value + 20);
                var screen = Screens.ScreenFromPoint(p);
                if (screen != null)
                {
                    Position = new PixelPoint(lastSize.LastX.Value, lastSize.LastY.Value);
                    if (!(lastSize.WasMaximized ?? false))
                    {
                        var tl = this.PointToClient(new PixelPoint(lastSize.LastX.Value, lastSize.LastY.Value));
                        var br = this.PointToClient(new PixelPoint(lastSize.LastX.Value + lastSize.LastWidth.Value, lastSize.LastY.Value + lastSize.LastHeight.Value));
                        Width = Math.Max(br.X - tl.X, 100);
                        Height = Math.Max(br.Y - tl.Y, 100);   
                    }
                }
            }
            WindowState = (lastSize.WasMaximized ?? false) ? WindowState.Maximized : WindowState.Normal;
        }

        private void ShowTooltipFlyout(Control element, string header, string content)
        {
            var popup = new Popup()
            {
                Child = new BalloonPopup()
                {
                    ShowTail = true,
                    TailAlignment = HorizontalAlignment.Center,
                    Content = new DockPanel()
                    {
                        Width = 300,
                        Children =
                        {
                            new TextBlock()
                            {
                                [DockPanel.DockProperty] = global::Avalonia.Controls.Dock.Top,
                                TextWrapping = TextWrapping.WrapWithOverflow,
                                FontWeight = FontWeight.Bold,
                                Text = header
                            },
                            new TextBlock()
                            {
                                TextWrapping = TextWrapping.WrapWithOverflow,
                                Text = content
                            }
                        }
                    }
                },
                IsLightDismissEnabled = true,
                OverlayDismissEventPassThrough = true,
                Placement = PlacementMode.Bottom,
                PlacementTarget = element,
                HorizontalOffset = 0,
                VerticalOffset = 5
            };
            ((ISetLogicalParent)popup).SetParent(element);
            popup.Open();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            if (DataContext is MainWindowViewModel vm)
            {
                if (vm.ShowSqlEditorNotification())
                {
                    DispatcherTimer.RunOnce(() =>
                    {
                        ShowTooltipFlyout(this.GetControl<Button>("OpenSQLDocument"), "SQL editor is now available!", "WoW Database Editor now features a complete SQL editor just like MySql Workbench, HeidiSQL or SQLyog. Press the button to open.");
                    }, TimeSpan.FromSeconds(1));
                }
                if (vm.ShowGlobalSearchNotification())
                {
                    DispatcherTimer.RunOnce(() =>
                    {
                        ShowTooltipFlyout(this.GetControl<Button>("GlobalSearchButton"), "Global search", "Press this button to open global search in menus, creatures, gameobjects, spells, flags and more.");
                    }, TimeSpan.FromSeconds(1));
                }
            }
        }

        private void OnKeyUpTunneled(object? sender, KeyEventArgs e)
        {
            // I can't do it in KeyBindings, because it needs to be handled in tunnel handler
            // also don't do it in OnKeyDown, because it is not fired for control + tab
            if ((e.KeyModifiers.HasFlagFast(KeyModifiers.Control) ||
                 e.KeyModifiers.HasFlagFast(KeyModifiers.Meta)) &&
                e.Key == Key.Tab)
            {
                var documentControl = (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Visual)?.FindAncestorOfType<DocumentControl>() ?? this.FindDescendantOfType<DocumentControl>();
                if (documentControl?.DataContext is not FocusAwareDocumentDock documentDock)
                    return;
                    
                if (documentDock.ActiveDockable == null || documentDock.VisibleDockables == null)
                    return;
                    
                var activeDockableIndex = documentDock.VisibleDockables.IndexOf(documentDock.ActiveDockable);
                if (activeDockableIndex == -1)
                    return;

                var direction = e.KeyModifiers.HasFlagFast(KeyModifiers.Shift) ? -1 : 1;

                activeDockableIndex = activeDockableIndex + direction;
                if (activeDockableIndex == documentDock.VisibleDockables.Count)
                    activeDockableIndex = 0;
                if (activeDockableIndex == -1)
                    activeDockableIndex = documentDock.VisibleDockables.Count - 1;

                documentDock.ActiveDockable = documentDock.VisibleDockables[activeDockableIndex];
                
                e.Handled = true;
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is ICloseAwareViewModel closeAwareViewModel)
            {
                closeAwareViewModel.CloseRequest += Close;
                closeAwareViewModel.ForceCloseRequest += () =>
                {
                    realClosing = true;
                    Close();
                };
            }
        }

        private bool realClosing = false;
        
        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);
            // we use screen coords for size so that size is custom app scaling independent
            if (DataContext is MainWindowViewModel closeAwareViewModel)
            {
                if (!realClosing)
                {
                    TryClose(closeAwareViewModel).ListenErrors();
                    e.Cancel = true;
                    return;
                }
                else
                    closeAwareViewModel.NotifyWillClose();
            }
            var screenTopLeftPoint = this.PointToScreen(new Point(Position.X, Position.Y));
            var screenBottomRightPoint = this.PointToScreen(new Point(Position.X + this.ClientSize.Width, Position.Y + this.ClientSize.Height));
            userSettings.Update(new WindowLastSize()
            {
                WasMaximized = WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen,
                LastX = Position.X,
                LastY = Position.Y,
                LastWidth = screenBottomRightPoint.X - screenTopLeftPoint.X,
                LastHeight = screenBottomRightPoint.Y - screenTopLeftPoint.Y,
            });
            fileSystem.WriteAllText(DockSettingsFile,
                JsonConvert.SerializeObject(avaloniaDockAdapter.SerializeDock(), Formatting.Indented));
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (DataContext is MainWindowViewModel vm)
                vm.SessionRestoreService.GracefulShutdown();
            GlobalApplication.IsRunning = false;
        }
        
        private async Task TryClose(MainWindowViewModel closeAwareViewModel)
        {
            if (await closeAwareViewModel.CanClose())
            {
                realClosing = true;
                Close();
            }
        }

        private void Drop(object? sender, DragEventArgs e)
        {
            if (!e.Data.Contains(DataFormats.Files) || !parserViewerSolutionItemService.Enabled)
                return;

            var files = e.Data.GetFiles();
            if (files == null)
                return;

            foreach (var file in files.Select(x => x.TryGetLocalPath())
                         .Where(x => x != null))
            {
                if (File.Exists(file))
                {
                    eventAggregator.GetEvent<EventRequestOpenItem>()
                        .Publish(parserViewerSolutionItemService.CreateSolutionItem(file));
                }
            }
        }

        private void DragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = e.Data.Contains(DataFormats.Files) && parserViewerSolutionItemService.Enabled ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void DragEnter(object? sender, DragEventArgs e)
        {
            e.DragEffects = e.Data.Contains(DataFormats.Files) && parserViewerSolutionItemService.Enabled ? DragDropEffects.Copy : DragDropEffects.None;
        }

        public struct WindowLastSize : ISettings
        {
            public bool? WasMaximized { get; set; }
            public int? LastX { get; set; }
            public int? LastY { get; set; }
            public int? LastWidth { get; set; }
            public int? LastHeight { get; set; }
        }

        private void FlyoutBase_OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
                vm.QuickAccessViewModel.IsOpened = true;
        }

        private void FlyoutBase_OnClosed(object? sender, EventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
                vm.QuickAccessViewModel.IsOpened = false;
        }
    }
}
