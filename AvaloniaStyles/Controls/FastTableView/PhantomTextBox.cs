using System;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AvaloniaStyles.Controls.FastTableView;

public abstract class PhantomControlBase<T> where T : Control
{
    private AdornerLayer? adornerLayer = null;
    private Panel panel = null!;
    private Visual? parent = null!;
    private IDisposable? clickDisposable = null;
    private T? element = null;

    protected virtual void Cleanup(T element) {}
    protected abstract void Save(T element);
    
    protected bool AttachAsAdorner(Visual parent, Rect position, T element)
    {
        Debug.Assert(element != null);
        this.parent = parent;
        adornerLayer = AdornerLayer.GetAdornerLayer(parent);
        if (adornerLayer == null)
        {
            Despawn(false);
            return false;
        }
        adornerLayer.Children.Add(element);
        AdornerLayer.SetAdornedElement(element, parent);
        AdornerLayer.SetIsClipEnabled(element, false);
        this.element = element;
        
        element.Width = position.Width;
        element.Height = position.Height;
        element.HorizontalAlignment = HorizontalAlignment.Left;
        element.VerticalAlignment = VerticalAlignment.Top;
        element.Margin = new Thickness(position.X, position.Y, 0, 0);
        
        if (parent.GetVisualRoot() is TopLevel toplevel)
        {
            clickDisposable = toplevel.AddDisposableHandler(InputElement.PointerPressedEvent, (s, ev) =>
            {
                bool hitTextbox = false;
                ILogical? logical = ev.Source as ILogical;
                while (logical != null)
                {
                    if (ReferenceEquals(logical, element))
                    {
                        hitTextbox = true;
                        break;
                    }
                    logical = logical.LogicalParent;
                }
                if (!hitTextbox)
                    Despawn(true);
            }, RoutingStrategies.Tunnel);
        }

        return true;
    }
    
    protected void Despawn(bool save)
    {
        if (element == null)
            return;
        
        if (save)
            Save(element);
        
        Cleanup(element);
        adornerLayer?.Children.Remove(element);
        adornerLayer = null;

        clickDisposable?.Dispose();
        clickDisposable = null;
        
        if (parent is IInputElement inputElement)
            FocusManager.Instance!.Focus(inputElement);
        element = null;
        parent = null;
    }
}

public class PhantomTextBox : PhantomControlBase<TextBox>
{
    private Action<string>? currentOnApply = null;

    public void Spawn(Visual parent, Rect position, string text, bool selectAll, Action<string> onApply)
    {
        currentOnApply = onApply;
        var textBox = new TextBox()
        {
        };
        textBox.MinWidth = 0;
        textBox.MinHeight = 0;
        textBox.Padding = new Thickness(5, 0, 0, 0);
        textBox.Text = text;
        textBox.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Enter),
            Command = new DelegateCommand(() => Despawn(true))
        });
        textBox.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Escape),
            Command = new DelegateCommand(() => Despawn(false))
        });
        textBox.LostFocus += ElementLostFocus;

        if (!AttachAsAdorner(parent, position, textBox))
            return;

        DispatcherTimer.RunOnce(textBox.Focus, TimeSpan.FromMilliseconds(1));
        if (selectAll)
            textBox.SelectAll();
        else
            textBox.SelectionStart = textBox.SelectionEnd = textBox.Text.Length;
    }
    
    private void ElementLostFocus(object? sender, RoutedEventArgs e)
    {
        Despawn(false);
    }

    protected override void Cleanup(TextBox element)
    {
        element.LostFocus -= ElementLostFocus;
        currentOnApply = null;
    }

    protected override void Save(TextBox element)
    {
        currentOnApply?.Invoke(element.Text);
    }
}

internal class DelegateCommand : ICommand
{
    private readonly Action action;

    public DelegateCommand(Action action)
    {
        this.action = action;
    }

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        action();
    }

    public event EventHandler? CanExecuteChanged;
}