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
using Avalonia.Media;
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
    
    public bool IsOpened { get; private set; }

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
        
        // avalonia 11 bug? https://github.com/AvaloniaUI/Avalonia/issues/9845
        var transformed = parent.TranslatePoint(position.Position, (parent.GetVisualRoot() as Visual)!)!.Value;
                    
        element.Width = position.Width;
        element.Height = position.Height;
        element.HorizontalAlignment = HorizontalAlignment.Left;
        element.VerticalAlignment = VerticalAlignment.Top;
        element.Margin = new Thickness(transformed.X, transformed.Y, 0, 0);
        
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

        IsOpened = true;
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
            inputElement.Focus();
        element = null;
        parent = null;
        IsOpened = false;
    }
}

public class PhantomTextBox : PhantomControlBase<TextBox>
{
    public enum ActionAfterSave
    {
        None,
        MoveUp,
        MoveDown,
        MoveNext
    }
    
    private Action<string, ActionAfterSave>? currentOnApply = null;
    private ActionAfterSave actionAfterSave;

    public void Spawn(Visual parent, Rect position, string text, bool selectAll, Action<string, ActionAfterSave> onApply)
    {
        currentOnApply = onApply;
        var textBox = new TextBox()
        {
        };
        textBox.MinWidth = 0;
        textBox.MinHeight = 0;
        textBox.Padding = new Thickness(5 + 3, -2, 0, 0);
        textBox.Margin = new Thickness(0,  0, 0, 0);
        textBox.Text = text;
        textBox.FontFamily = new FontFamily("Consolas,Menlo,Courier,Courier New");
        textBox.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Enter),
            Command = new DelegateCommand(() => Despawn(true))
        });
        textBox.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Tab),
            Command = new DelegateCommand(() =>
            {
                actionAfterSave = ActionAfterSave.MoveNext;
                Despawn(true);
            })
        });
        textBox.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Up),
            Command = new DelegateCommand(() =>
            {
                actionAfterSave = ActionAfterSave.MoveUp;
                Despawn(true);
            })
        });
        textBox.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Down),
            Command = new DelegateCommand(() =>
            {
                actionAfterSave = ActionAfterSave.MoveDown;
                Despawn(true);
            })
        });
        textBox.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Escape),
            Command = new DelegateCommand(() => Despawn(false))
        });
        textBox.LostFocus += ElementLostFocus;

        if (!AttachAsAdorner(parent, position, textBox))
            return;

        DispatcherTimer.RunOnce(() => textBox.Focus(), TimeSpan.FromMilliseconds(1));
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
        if (element != null)
            element.LostFocus -= ElementLostFocus;
        currentOnApply = null;
    }

    protected override void Save(TextBox element)
    {
        currentOnApply?.Invoke(element.Text ?? "", actionAfterSave);
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
