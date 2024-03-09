using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FuzzySharp;
using WDE.Common.Utils;
using WDE.MVVM.Utils;

namespace AvaloniaStyles.Controls
{
    public class CompletionComboBox : TemplatedControl
    {
        public class EnterPressedArgs : EventArgs
        {
            public EnterPressedArgs(string searchText, object? selectedItem)
            {
                SearchText = searchText;
                SelectedItem = selectedItem;
            }

            public bool Handled { get; set; }
            public string SearchText { get; init; }
            public object? SelectedItem { get; init; }
        }
        
        public Func<object?, object?>? SelectedItemExtractor { get; set; }
        private ISelectionAdapter? adapter;
        public event EventHandler<EnterPressedArgs>? OnEnterPressed;
        private Func<IEnumerable, string, CancellationToken, Task<IEnumerable>> asyncPopulator;
        private string searchText = string.Empty;
        private string? watermark = null;
        private bool isDropDownOpen;
        private object? selectedItem;
        private IEnumerable? items;

        public Func<IEnumerable, string, CancellationToken, Task<IEnumerable>> AsyncPopulator
        {
            get => asyncPopulator;
            set => SetAndRaise(AsyncPopulatorProperty, ref asyncPopulator, value);
        }
        
        public string SearchText
        {
            get => searchText;
            set => SetAndRaise(SearchTextProperty, ref searchText, value);
        }
        
        public string? Watermark
        {
            get => watermark;
            set => SetAndRaise(WatermarkProperty, ref watermark, value);
        }

        public bool IsDropDownOpen
        {
            get => isDropDownOpen;
            set => SetAndRaise(IsDropDownOpenProperty, ref  isDropDownOpen, value);
        }
        
        public IEnumerable? Items
        {
            get => items;
            set => SetAndRaise(ItemsProperty, ref items, value);
        }

        public object? SelectedItem
        {
            get => selectedItem;
            set => SetAndRaise(SelectedItemProperty, ref selectedItem, value);
        }
        
        public IDataTemplate ItemTemplate
        {
            get => GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }
        
        public IDataTemplate? ButtonItemTemplate
        {
            get => GetValue(ButtonItemTemplateProperty);
            set => SetValue(ButtonItemTemplateProperty, value);
        }
        
        public bool HideButton
        {
            get => GetValue(HideButtonProperty);
            set => SetValue(HideButtonProperty, value);
        }

        public bool IsLightDismissEnabled
        {
            get => GetValue(IsLightDismissEnabledProperty);
            set => SetValue(IsLightDismissEnabledProperty, value);
        }
        
        public bool SetSelectedItemToNullBeforeCommit
        {
            get => GetValue(SetSelectedItemToNullBeforeCommitProperty);
            set => SetValue(SetSelectedItemToNullBeforeCommitProperty, value);
        }

        public static readonly StyledProperty<IDataTemplate?> ButtonItemTemplateProperty =
            AvaloniaProperty.Register<CompletionComboBox, IDataTemplate?>(nameof(ButtonItemTemplate));
        
        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            AvaloniaProperty.Register<CompletionComboBox, IDataTemplate>(nameof(ItemTemplate));
        
        public static readonly DirectProperty<CompletionComboBox, IEnumerable?> ItemsProperty = 
            AvaloniaProperty.RegisterDirect<CompletionComboBox, IEnumerable?>("Items", 
                o => o.Items, 
                (o, v) => o.Items = v);
        
        public static readonly DirectProperty<CompletionComboBox, string?> WatermarkProperty =
            AvaloniaProperty.RegisterDirect<CompletionComboBox, string?>(
                nameof(Watermark),
                o => o.Watermark,
                (o, v) => o.Watermark = v,
                unsetValue: null);
        
        public static readonly DirectProperty<CompletionComboBox, string> SearchTextProperty =
            AvaloniaProperty.RegisterDirect<CompletionComboBox, string>(
                nameof(SearchText),
                o => o.SearchText,
                (o, v) => o.SearchText = v,
                unsetValue: string.Empty);
        
        public static readonly DirectProperty<CompletionComboBox, Func<IEnumerable,string, CancellationToken, Task<IEnumerable>>> AsyncPopulatorProperty =
            AvaloniaProperty.RegisterDirect<CompletionComboBox, Func<IEnumerable,string, CancellationToken, Task<IEnumerable>>>(
                nameof(AsyncPopulator),
                o => o.AsyncPopulator,
                (o, v) => o.AsyncPopulator = v);
        
        public static readonly DirectProperty<CompletionComboBox, bool> IsDropDownOpenProperty =
            AvaloniaProperty.RegisterDirect<CompletionComboBox, bool>(
                nameof(IsDropDownOpen),
                o => o.IsDropDownOpen,
                (o, v) => o.IsDropDownOpen = v);
        
        public static readonly DirectProperty<CompletionComboBox, object?> SelectedItemProperty =
            AvaloniaProperty.RegisterDirect<CompletionComboBox, object?>(
                nameof(SelectedItem),
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v,
                defaultBindingMode: BindingMode.TwoWay);
        
        public static readonly StyledProperty<bool> SetSelectedItemToNullBeforeCommitProperty =
            AvaloniaProperty.Register<CompletionComboBox, bool>(nameof(SetSelectedItemToNullBeforeCommit));

        public static readonly StyledProperty<bool> IsLightDismissEnabledProperty =
            AvaloniaProperty.Register<CompletionComboBox, bool>(nameof (IsLightDismissEnabled), true);

        public static readonly StyledProperty<bool> HideButtonProperty =
            AvaloniaProperty.Register<CompletionComboBox, bool>(nameof (HideButton));
        
        public event Action? Closed;
        
        public CompletionComboBox()
        {
            // Default async populator searches in Items (toString) using Fuzzy match or normal match depending on the collection size
            asyncPopulator = async (items, s, token) =>
            {
                if (items is not IList o)
                    return Enumerable.Empty<object>();

                if (string.IsNullOrEmpty(s))
                    return items;

                return await Task.Run(() =>
                {
                    if (o.Count < 250)
                    {
                        var result = Process.ExtractSorted(s, items.Cast<object>().Select(item => item.ToString()), cutoff: 51)
                            .Select(item => o[item.Index]!);
                        return result;
                    }

                    List<object> picked = new();
                    var search = s.ToLower();
                    foreach (var item in o)
                    {
                        if (item.ToString()!.Contains(search, StringComparison.InvariantCultureIgnoreCase))
                            picked.Add(item);

                        if (token.IsCancellationRequested)
                            break;
                    }

                    return picked;
                }, token);
            };
            KeyBindings.Add(new KeyBinding()
            {
                Gesture = new KeyGesture(Key.C, KeyModifiers.Control),
                Command = new DelegateCommand(Copy)
            });
            KeyBindings.Add(new KeyBinding()
            {
                Gesture = new KeyGesture(Key.C, KeyModifiers.Meta),
                Command = new DelegateCommand(Copy)
            });
            KeyBindings.Add(new KeyBinding()
            {
                Gesture = new KeyGesture(Key.V, KeyModifiers.Control),
                Command = new AsyncAutoCommand(Paste)
            });
            KeyBindings.Add(new KeyBinding()
            {
                Gesture = new KeyGesture(Key.V, KeyModifiers.Meta),
                Command = new AsyncAutoCommand(Paste)
            });
        }

        private void Copy()
        {
            if (SelectedItem != null)
            {
                if (watermark is not null)
                    TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(watermark);
            }
        }

        private async Task Paste()
        {
            var textTask = TopLevel.GetTopLevel(this)?.Clipboard?.GetTextAsync();
            if (textTask == null)
                return;
            var text = await textTask;
            
            //IsDropDownOpen = true;
            //await Task.Delay(1);
            SearchText = text ?? "";
            SearchTextBox.RaiseEvent(new KeyEventArgs()
            {
                //Device = null,
                Handled = false,
                Key = Key.Enter,
                Route = RoutingStrategies.Tunnel,
                RoutedEvent = KeyDownEvent,
                Source = SearchTextBox
            });
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            // redirect text input from ComboBox to textbox inside.
            if (!e.Handled && e.Text != "\n" && e.Text != "\r")
            {
                IsDropDownOpen = true;
                DispatcherTimer.RunOnce(() =>
                {
                    TextInputEventArgs args = new TextInputEventArgs();
                    args.Handled = false;
                    args.Text = e.Text;
                    args.Route = e.Route;
                    args.RoutedEvent = e.RoutedEvent;
                    args.Source = SearchTextBox;
                    SearchTextBox.RaiseEvent(args);
                }, TimeSpan.FromMilliseconds(1));
                e.Handled = true;
            }
        }
        
        static CompletionComboBox()
        {
            HideButtonProperty.Changed.AddClassHandler<CompletionComboBox>((box, args) =>
            {
                box.PseudoClasses.Set(":hideButton", args.NewValue is true);
            });
            ItemTemplateProperty.Changed.AddClassHandler<CompletionComboBox>((box, args) =>
            {
                if (box.ButtonItemTemplate == null)
                {
                    box.ButtonItemTemplate = args.NewValue as IDataTemplate;
                }
            });
            ItemsProperty.Changed.AddClassHandler<CompletionComboBox>((box, args) =>
            {
                if (box.IsDropDownOpen)
                    box.TextUpdated(box.SearchText);
            });
            IsDropDownOpenProperty.Changed.AddClassHandler<CompletionComboBox>((box, args) =>
            {
                if (args.NewValue is true)
                {
                    DispatcherTimer.RunOnce(() =>
                    {
                        if (box.SearchTextBox == null) // before template applied
                            return;
                        
                        //box.SearchText = "";
                        box.SearchTextBox.Focus(NavigationMethod.Pointer);
                        box.SearchTextBox.SelectionEnd = box.SearchTextBox.SelectionStart = box.SearchText.Length;
                    }, TimeSpan.FromMilliseconds(16));

                    box.TextUpdated("");
                    
                    if (box.SearchTextBox == null) // before template applied
                        return;
                    
                    box.SearchText = "";
                }
            });
        }
        
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            SearchTextBox = e.NameScope.Find<TextBox>("PART_SearchTextBox")  ?? throw new NullReferenceException("Couldn't find PART_SearchTextBox");
            SearchTextBox.AddHandler(KeyDownEvent, SearchTextBoxOnKeyDown, RoutingStrategies.Tunnel);
            
            ToggleButton = e.NameScope.Find<ToggleButton>("PART_Button")  ?? throw new NullReferenceException("Couldn't find PART_Button");
            
            ListBox = e.NameScope.Find<ListBox>("PART_SelectingItemsControl") ?? throw new NullReferenceException("Couldn't find PART_SelectingItemsControl");
            ListBox.PointerReleased += OnSelectorPointerReleased;
            if (ListBox != null)
            {
                // Check if it is already an IItemsSelector
                if (ListBox is ISelectionAdapter selectionAdapter)
                {
                    SelectionAdapter = selectionAdapter;
                }
                else
                {
                    // Built in support for wrapping a Selector control
                    SelectionAdapter = new SelectingItemsControlSelectionAdapter(ListBox);
                }
            }

            base.OnApplyTemplate(e);
        }

        private void SearchTextBoxOnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyModifiers == KeyModifiers.None)
                return;
            if (!string.IsNullOrWhiteSpace(SearchText))
                return;
            if (watermark == null)
                return;
            foreach (var binding in TopLevel.GetTopLevel(this)!.PlatformSettings!.HotkeyConfiguration!.Copy)
            {
                if (binding.Matches(e))
                {
                    TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(watermark);
                    e.Handled = true;
                }
            }
        }

        private void OnSelectorPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.Source is Control control && control.FindAncestorOfType<CheckBox>() != null)
                return;
            
            Commit(adapter?.SelectedItem);
        }

        private void OnTextBoxTextChanged()
        {
            //Uses Dispatcher.Post to allow the TextBox selection to update before processing
            Dispatcher.UIThread.Post(() =>
            {
                // Call the central updated text method as a user-initiated action
                TextUpdated(textBox.Text ?? "");
            });
        }
        
        /// <summary>
        /// Gets or sets the selection adapter used to populate the drop-down
        /// with a list of selectable items.
        /// </summary>
        /// <value>The selection adapter used to populate the drop-down with a
        /// list of selectable items.</value>
        /// <remarks>
        /// You can use this property when you create an automation peer to
        /// use with AutoCompleteBox or deriving from AutoCompleteBox to
        /// create a custom control.
        /// </remarks>
        protected ISelectionAdapter SelectionAdapter
        {
            get => adapter!;
            set
            {
                if (adapter != null)
                {
                    adapter.ItemsSource = null;
                }

                adapter = value;

                if (adapter != null)
                {
                    adapter.ItemsSource = view;
                }
            }
        }

        private TextBox textBox = null!;
        private IDisposable? textBoxSubscriptions;
        public ListBox ListBox { get; set; } = null!;
        private ToggleButton ToggleButton { get; set; } = null!;
        private TextBox SearchTextBox
        {
            get => textBox;
            set
            {
                textBoxSubscriptions?.Dispose();
                textBox = value;

                // Attach handlers
                if (textBox != null)
                {
                    textBoxSubscriptions =
                        textBox.GetObservable(TextBox.TextProperty)
                            .Skip(1)
                            .Subscribe(_ => OnTextBoxTextChanged())
                            .Combine(
                                textBox.AddDisposableHandler(TextInputEvent, (_, args) =>
                                {
                                    if (args.Text == " ")
                                    {
                                        if (SelectionAdapter.SelectedItem != null)
                                        {
                                            var container =
                                                ListBox.ContainerFromIndex(ListBox.SelectedIndex);
                                            if (container?.FindDescendantOfType<CheckBox>() is CheckBox cb)
                                            {
                                                cb.IsChecked = !cb.IsChecked;
                                                args.Handled = true;
                                            }
                                        }
                                    }
                                }, RoutingStrategies.Tunnel))
                            .Combine(
                                textBox.AddDisposableHandler(KeyDownEvent, (_, args) =>
                                {
                                    if (args.Handled)
                                        return;

                                    if (args.Key == Key.Enter)
                                    {
                                        var enterArgs = new EnterPressedArgs(textBox.Text ?? "",
                                            SelectionAdapter.SelectedItem);
                                        OnEnterPressed?.Invoke(this, enterArgs);
                                        if (!enterArgs.Handled)
                                            Commit(SelectionAdapter.SelectedItem ?? (view.Count > 0 ? view[0] : null));
                                        args.Handled = true;
                                        Close();
                                    }
                                    else if (args.Key == Key.Tab)
                                    {
                                        var newArgs = new KeyEventArgs()
                                        {
                                            Key = (args.KeyModifiers & KeyModifiers.Shift) != 0 ? Key.Up : Key.Down,
                                            Route = args.Route,
                                            RoutedEvent = args.RoutedEvent,
                                            KeyModifiers = args.KeyModifiers,
                                            Source = args.Source,
                                        };
                                        SelectionAdapter.HandleKeyDown(newArgs);
                                        args.Handled = true;
                                    }
                                    else if (args.Key == Key.Escape)
                                    {
                                        Close();
                                        args.Handled = true;
                                    }
                                    else
                                    {
                                        SelectionAdapter.HandleKeyDown(args);
                                    }
                                }))
                            .Combine(textBox.AddDisposableHandler(LostFocusEvent, (_, _) =>
                            {
                                // don't ever let textbox lost focus
                                if (IsDropDownOpen)
                                {
                                    Dispatcher.UIThread.Post(() => textBox.Focus());
                                }
                            }));
                }
            }
        }

        private void Commit(object? item)
        {
            if (SelectedItemExtractor != null)
                item = SelectedItemExtractor(item);
            if (SetSelectedItemToNullBeforeCommit)
                SelectedItem = null;
            SelectedItem = item;
            Close();
        }

        private void Close()
        {
            IsDropDownOpen = false;
            ToggleButton.Focus(NavigationMethod.Tab);
            Closed?.Invoke();
        }
        
        /// Filter logic
        /// <summary>
        /// Gets or sets the observable collection that contains references to
        /// all of the items in the generated view of data that is provided to
        /// the selection-style control adapter.
        /// </summary>
        private readonly AvaloniaList<object> view = new();
        
        private void TextUpdated(string text)
        {
            PopulateDropDown(text);
        }
        
        private void PopulateDropDown(string text)
        {
            if (TryPopulateAsync(text))
            {
            }
        }
        
        private CancellationTokenSource? populationCancellationTokenSource;

        private bool TryPopulateAsync(string searchText)
        {
            populationCancellationTokenSource?.Cancel(false);
            populationCancellationTokenSource?.Dispose();
            populationCancellationTokenSource = null;

            populationCancellationTokenSource = new CancellationTokenSource();
            var task = PopulateAsync(searchText, populationCancellationTokenSource.Token);
            if (task.Status == TaskStatus.Created)
                task.Start();

            return true;
        }
        
        private async Task PopulateAsync(string searchText, CancellationToken cancellationToken)
        {
            try
            {
                IEnumerable? result = await asyncPopulator.Invoke(items ?? Array.Empty<object>(), searchText, cancellationToken);
                if (result == null)
                {
                    view.Clear();
                    return;
                }
                var resultList = result is IList ? (IList)result : result.Cast<object>()?.ToList();

                if (cancellationToken.IsCancellationRequested)
                    return;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        view.Clear();
                        if (resultList != null)
                            view.InsertRange(0, resultList.Cast<object>());
                    }
                });
            }
            catch (TaskCanceledException)
            { }
            finally
            {
                populationCancellationTokenSource?.Dispose();
                populationCancellationTokenSource = null;
            }
        }
    }
}
