using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Prism.Commands;

namespace WDE.Common.Avalonia.Components;

public class EditableTextBlock : TemplatedControl
{
    private AdornerLayer? adornerLayer = null;
    private TextBox? textBox = null;
    private Panel panel = null!;

    private IDisposable? clickDisposable = null;
    
    private string text = "";
    public static readonly DirectProperty<EditableTextBlock, string> TextProperty = AvaloniaProperty.RegisterDirect<EditableTextBlock, string>("Text", o => o.Text, (o, v) => o.Text = v, defaultBindingMode: BindingMode.TwoWay);
    
    public string Text
    {
        get => text;
        set => SetAndRaise(TextProperty, ref text, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        panel = e.NameScope.Find<Panel>("PART_Panel") ?? throw new NullReferenceException("PART_Panel not found");
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            SpawnTextBox();
            e.Handled = true;
        }
        else
            base.OnPointerPressed(e);
    }

    private void SpawnTextBox()
    {
        textBox = new TextBox()
        {
        };
        textBox.Text = text;
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
        adornerLayer.Children.Add(textBox);
        AdornerLayer.SetAdornedElement(textBox, this);

        DispatcherTimer.RunOnce(() => textBox.Focus(), TimeSpan.FromMilliseconds(1));
        textBox.SelectAll();

        if (this.GetVisualRoot() is TopLevel toplevel)
        {
            clickDisposable = toplevel.AddDisposableHandler(PointerPressedEvent, (s, ev) =>
            {
                bool hitTextbox = false;
                ILogical? logical = ev.Source as ILogical;
                while (logical != null)
                {
                    if (ReferenceEquals(logical, textBox))
                    {
                        hitTextbox = true;
                        break;
                    }
                    logical = logical.LogicalParent;
                }
                if (!hitTextbox)
                    DespawnTextBox(true);
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

    private void DespawnTextBox(bool save)
    {
        if (textBox == null)
            return;
        textBox.LostFocus -= TextBox_LostFocus;
        adornerLayer?.Children.Remove(textBox);
        adornerLayer = null;

        clickDisposable?.Dispose();
        clickDisposable = null;
        
        if (save)
            Text = textBox.Text ?? "";
        textBox = null;
    }
}