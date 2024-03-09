using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaStyles.Controls;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.MVVM.Observable;

namespace WDE.Common.Avalonia.Components;

public class NewItemRequestArgs : EventArgs
{
    public string Text { get; }

    public NewItemRequestArgs(string text)
    {
        Text = text;
    }

    public bool Handled { get; set; }
}

public class EditableItemsTextBlock : TemplatedControl
{
    private AdornerLayer? adornerLayer = null;
    private TextBox? textBox = null;
    private CompletionComboBox? comboBox;
    private Panel panel = null!;
    private TextBlock textBlock = null!;
    private Button openListButton = null!;

    private IDisposable? clickDisposable = null;
    private IDisposable? lostFocusDisposable = null;
    
    private object? selectedItem;
    public static readonly DirectProperty<EditableItemsTextBlock, object?> SelectedItemProperty = AvaloniaProperty.RegisterDirect<EditableItemsTextBlock, object?>(nameof(SelectedItem), o => o.SelectedItem, (o, v) => o.SelectedItem = v, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<bool> SinglePressToEditProperty = AvaloniaProperty.Register<EditableItemsTextBlock, bool>(nameof(SinglePressToEdit));
    public static readonly StyledProperty<IEnumerable> ItemsProperty = AvaloniaProperty.Register<EditableItemsTextBlock, IEnumerable>(nameof(Items), defaultValue: Enumerable.Empty<object>());
    public event EventHandler<NewItemRequestArgs>? OnNewItemRequest;

    public object? SelectedItem
    {
        get => selectedItem;
        set => SetAndRaise(SelectedItemProperty, ref selectedItem, value);
    }

    public bool SinglePressToEdit
    {
        get => (bool)GetValue(SinglePressToEditProperty);
        set => SetValue(SinglePressToEditProperty, value);
    }
    
    public IEnumerable Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        panel = e.NameScope.Get<Panel>("PART_Panel");
        textBlock = e.NameScope.Get<TextBlock>("PART_TextBlock");
        openListButton = e.NameScope.Get<Button>("PART_OpenListButton");
        openListButton.Click += OpenListButtonOnClick;
    }

    private void OpenListButtonOnClick(object? sender, RoutedEventArgs e)
    {
        SpawnComboBox();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (textBox != null)
            return;
        if (e.ClickCount == 2 || SinglePressToEdit)
        {
            SpawnTextBox();
            e.Handled = true;
        }
        else
            base.OnPointerPressed(e);
    }

    public void BeginEditing(string overrideText)
    {
        SpawnTextBox(overrideText);
    }

    private void SpawnComboBox()
    {
        comboBox = new CompletionComboBox();
        comboBox.Items = Items;
        comboBox.SelectedItem = SelectedItem;
        comboBox.HideButton = true;
        comboBox.IsLightDismissEnabled = false; // we are handling it ourselves, without doing .Handled = true so that as soon as user press outside of popup, the click is treated as actual click
        comboBox.Closed += CompletionComboBoxOnClosed;
        
        adornerLayer = AdornerLayer.GetAdornerLayer(this);
        if (adornerLayer == null)
        {
            DespawnComboBox(false);
            return;
        }
        AdornerLayer.SetAdornedElement(comboBox, this);
        adornerLayer.Children.Add(comboBox);
        
        BindTopLevel();
        
        DispatcherTimer.RunOnce(() =>
        {
            comboBox.IsDropDownOpen = true;
        }, TimeSpan.FromMilliseconds(1));
    }

    private void CompletionComboBoxOnClosed()
    {
        DespawnComboBox(true);
    }

    private void SpawnTextBox(string? overrideText = null)
    {
        textBox = new TextBox()
        {
        };
        textBox.Padding = textBlock.Padding;
        textBox.Margin = textBox.Margin;
        textBox.BorderThickness = new Thickness(0);
        textBox.MinHeight = 0;
        textBox.Text = overrideText ?? selectedItem?.ToString();
        textBox.FontFamily = textBlock.FontFamily;
        textBox.MinWidth = 0;
        textBox.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Enter),
            Command = new DelegateCommand(() => DespawnTextBox(true))
        });
        textBox.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Escape),
            Command = new DelegateCommand(() => DespawnTextBox(false))
        });
        textBox.LostFocus += TextBox_LostFocus;

        adornerLayer = AdornerLayer.GetAdornerLayer(this);
        if (adornerLayer == null)
        {
            DespawnTextBox(false);
            return;
        }
        AdornerLayer.SetAdornedElement(textBox, this);
        adornerLayer.Children.Add(textBox);

        DispatcherTimer.RunOnce(() => textBox.Focus(), TimeSpan.FromMilliseconds(1));
        if (overrideText == null)
            textBox.SelectAll();
        else
            textBox.SelectionStart = textBox.SelectionEnd = textBox.Text?.Length ?? 0;

        BindTopLevel();
    }

    private void BindTopLevel()
    {
        if (this.GetVisualRoot() is TopLevel toplevel)
        {
            lostFocusDisposable = toplevel.GetPropertyChangedObservable(WindowBase.IsActiveProperty)
                .SubscribeAction(e =>
                {
                    if (e.NewValue is bool b && !b)
                    {
                        if (textBox != null)
                            DespawnTextBox(false);
                        else if (comboBox != null)
                            DespawnComboBox(false);
                        else
                            LOG.LogError(LOG.NonCriticalInvalidStateEventId, "EditableItemsTextBlock has neither textbox nor combobox spawned when it should have one of them spawned");
                    }
                });
            clickDisposable = toplevel.AddDisposableHandler(PointerPressedEvent, (s, ev) =>
            {
                bool hitTextbox = false;
                bool hitComboBox = false;
                ILogical? logical = ev.Source as ILogical;
                while (logical != null)
                {
                    if (ReferenceEquals(logical, textBox))
                    {
                        hitTextbox = true;
                        break;
                    }

                    if (ReferenceEquals(logical, comboBox))
                    {
                        hitComboBox = true;
                        break;
                    }
                    logical = logical.LogicalParent;
                }
                if (!hitTextbox)
                    DespawnTextBox(true);
                if (!hitComboBox)
                    DespawnComboBox(true);
            }, RoutingStrategies.Tunnel);
        }
    }

    private void TextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (textBox != null)
        {
            DespawnTextBox(false);
        }
    }

    private void DespawnComboBox(bool save)
    {
        if (comboBox == null)
            return;
        
        comboBox.Closed -= CompletionComboBoxOnClosed;
        
        adornerLayer?.Children.Remove(comboBox);
        adornerLayer = null;
        
        clickDisposable?.Dispose();
        clickDisposable = null;
        lostFocusDisposable?.Dispose();
        lostFocusDisposable = null;
        
        if (save)
        {
            SelectedItem = comboBox.SelectedItem;
        }
        comboBox = null;
    }

    private void DespawnTextBox(bool save)
    {
        if (textBox == null)
            return;
        textBox.LostFocus -= TextBox_LostFocus;
        adornerLayer?.Children.Remove(textBox);
        adornerLayer = null;

        clickDisposable?.Dispose();
        clickDisposable = null;
        lostFocusDisposable?.Dispose();
        lostFocusDisposable = null;

        if (save)
        {
            OnNewItemRequest?.Invoke(this, new NewItemRequestArgs(textBox.Text ?? ""));
        }
        textBox = null;
    }
}
