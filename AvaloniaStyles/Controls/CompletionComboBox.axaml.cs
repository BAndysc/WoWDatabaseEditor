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
        private Func<string, CancellationToken, Task<IEnumerable<object>>> asyncPopulator;
        private string searchText = string.Empty;
        private string? watermark = null;
        private bool isDropDownOpen;
        private object? selectedItem;
        private IEnumerable<object>? items;

        public Func<string, CancellationToken, Task<IEnumerable<object>>> AsyncPopulator
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
        
        public IEnumerable<object>? Items
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
        
        public static readonly StyledProperty<IDataTemplate?> ButtonItemTemplateProperty =
            AvaloniaProperty.Register<CompletionComboBox, IDataTemplate?>(nameof(ButtonItemTemplate));
        
        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            AvaloniaProperty.Register<CompletionComboBox, IDataTemplate>(nameof(ItemTemplate));
        
        public static readonly DirectProperty<CompletionComboBox, IEnumerable<object>?> ItemsProperty = 
            AvaloniaProperty.RegisterDirect<CompletionComboBox, IEnumerable<object>?>("Items", 
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
                unsetValue: string.Empty);
        
        public static readonly DirectProperty<CompletionComboBox, Func<string, CancellationToken, Task<IEnumerable<object>>>> AsyncPopulatorProperty =
            AvaloniaProperty.RegisterDirect<CompletionComboBox, Func<string, CancellationToken, Task<IEnumerable<object>>>>(
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

        public static readonly StyledProperty<bool> IsLightDismissEnabledProperty =
            AvaloniaProperty.Register<CompletionComboBox, bool>(nameof (IsLightDismissEnabled), true);

        public static readonly StyledProperty<bool> HideButtonProperty =
            AvaloniaProperty.Register<CompletionComboBox, bool>(nameof (HideButton));
        
        public event Action? Closed;
        
        public CompletionComboBox()
        {
            // Default async populator searches in Items (toString) using Fuzzy match or normal match depending on the collection size
            asyncPopulator = async (s, token) =>
            {
                if (items is not IList o)
                    return Enumerable.Empty<object>();

                if (string.IsNullOrEmpty(s))
                    return items;

                return await Task.Run(() =>
                {
                    if (o.Count < 250)
                    {
                        return Process.ExtractSorted(s, items.Select(item => item.ToString()), cutoff: 51)
                            .Select(item => o[item.Index]!);   
                    }

                    List<object> picked = new();
                    var search = s.ToLower();
                    foreach (var item in o)
                    {
                        if (item.ToString()!.ToLowerInvariant().Contains(search))
                            picked.Add(item);

                        if (token.IsCancellationRequested)
                            break;
                    }

                    return picked;
                }, token);
            };
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            // redirect text input from ComboBox to textbox inside.
            if (!e.Handled && e.Text != "\n" && e.Text != "\r")
            {
                IsDropDownOpen = true;
                SearchTextBox.RaiseEvent(new TextInputEventArgs
                {
                    Device = e.Device,
                    Handled = false,
                    Text = e.Text,
                    Route = e.Route,
                    RoutedEvent = e.RoutedEvent,
                    Source = SearchTextBox
                });
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
            IsDropDownOpenProperty.Changed.AddClassHandler<CompletionComboBox>((box, args) =>
            {
                if (args.NewValue is true)
                {
                    DispatcherTimer.RunOnce(() =>
                    {
                        if (box.SearchTextBox == null) // before template applied
                            return;
                        
                        box.SearchTextBox.Text = "";
                        FocusManager.Instance.Focus(box.SearchTextBox, NavigationMethod.Pointer);
                        box.SearchTextBox.SelectionEnd = box.SearchTextBox.SelectionStart = box.SearchTextBox.Text.Length;
                    }, TimeSpan.FromMilliseconds(16));

                    box.TextUpdated("");
                    
                    if (box.SearchTextBox == null) // before template applied
                        return;
                    
                    box.SearchTextBox.Text = "";
                }
            });
        }
        
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            SearchTextBox = e.NameScope.Find<TextBox>("PART_SearchTextBox");
            SearchTextBox.AddHandler(KeyDownEvent, SearchTextBoxOnKeyDown, RoutingStrategies.Tunnel);
            
            ToggleButton = e.NameScope.Find<ToggleButton>("PART_Button");
            
            ListBox = e.NameScope.Find<ListBox>("PART_SelectingItemsControl");
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
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
                return;
            if (watermark == null)
                return;
            foreach (var binding in AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>().Copy)
            {
                if (binding.Matches(e))
                {
                    AvaloniaLocator.Current.GetService<IClipboard>().SetTextAsync(watermark);
                    e.Handled = true;
                }
            }
        }

        private void OnSelectorPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.Source is IControl control && control.FindAncestorOfType<CheckBox>() != null)
                return;
            
            Commit(adapter?.SelectedItem);
        }

        private void OnTextBoxTextChanged()
        {
            //Uses Dispatcher.Post to allow the TextBox selection to update before processing
            Dispatcher.UIThread.Post(() =>
            {
                // Call the central updated text method as a user-initiated action
                TextUpdated(textBox.Text);
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
                    adapter.Items = null;
                }

                adapter = value;

                if (adapter != null)
                {
                    adapter.Items = view;
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
                                textBox.AddDisposableHandler(KeyDownEvent, (_, args) =>
                                {
                                    if (args.Handled)
                                        return;

                                    if (args.Key == Key.Enter)
                                    {
                                        var enterArgs = new EnterPressedArgs(textBox.Text,
                                            SelectionAdapter.SelectedItem);
                                        OnEnterPressed?.Invoke(this, enterArgs);
                                        if (!enterArgs.Handled)
                                            Commit(SelectionAdapter.SelectedItem ?? (view.Count > 0 ? view[0] : null));
                                        args.Handled = true;
                                        Close();
                                    }
                                    else if (args.Key == Key.Tab)
                                    {
                                        args.Key = (args.KeyModifiers & KeyModifiers.Shift) != 0 ? Key.Up : Key.Down;
                                        SelectionAdapter.HandleKeyDown(args);
                                        args.Handled = true;
                                    }
                                    else if (args.Key == Key.Escape)
                                    {
                                        Close();
                                        args.Handled = true;
                                    }
                                    else if (args.Key == Key.Space)
                                    {
                                        if (SelectionAdapter.SelectedItem != null)
                                        {
                                            var container =
                                                ListBox.ItemContainerGenerator
                                                    .ContainerFromIndex(ListBox.SelectedIndex);
                                            if (container?.FindDescendantOfType<CheckBox>() is CheckBox cb)
                                            {
                                                cb.IsChecked = !cb.IsChecked;
                                                args.Handled = true;
                                            }
                                        }
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
                                    Dispatcher.UIThread.Post(() => FocusManager.Instance.Focus(textBox));
                                }
                            }));
                }
            }
        }
        
        private void Commit(object? item)
        {
            if (SelectedItemExtractor != null)
                item = SelectedItemExtractor(item);
            SelectedItem = item;
            Close();
        }

        private void Close()
        {
            IsDropDownOpen = false;
            FocusManager.Instance.Focus(ToggleButton, NavigationMethod.Tab);
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
                IEnumerable<object> result = await asyncPopulator.Invoke(searchText, cancellationToken);
                var resultList = result is IList ? result : result?.ToList();

                if (cancellationToken.IsCancellationRequested)
                    return;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        view.Clear();
                        if (resultList != null)
                            view.InsertRange(0, resultList);
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