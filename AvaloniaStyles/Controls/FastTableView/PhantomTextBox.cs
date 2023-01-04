using System;
using System.Collections;
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
using WDE.MVVM.Observable;

namespace AvaloniaStyles.Controls.FastTableView;

public abstract class PhantomControlBase<T> where T : Control
{
    private AdornerLayer? adornerLayer = null;
    private Visual? parent = null!;
    private IDisposable? clickDisposable = null;
    private IDisposable? focusDisposable = null;
    private T? element = null;
    
    public bool IsOpened { get; private set; }

    protected virtual void Cleanup(T element) {}
    protected abstract void Save(T element);
    
    protected bool AttachAsAdorner(Visual parent, Rect position, T element, bool despawnOnWindowLostFocus = false)
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
            if (despawnOnWindowLostFocus && toplevel is Window w)
            {
                focusDisposable = w.GetObservable(WindowBase.IsActiveProperty)
                    .SubscribeAction(@is =>
                    {
                        if (!@is)
                            Despawn(false);
                    });
            }
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
        focusDisposable?.Dispose();
        focusDisposable = null;
        
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
        Despawn(false);

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
                actionAfterSave = ActionAfterSave.None;
            })
        });
        textBox.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Up),
            Command = new DelegateCommand(() =>
            {
                actionAfterSave = ActionAfterSave.MoveUp;
                Despawn(true);
                actionAfterSave = ActionAfterSave.None;
            })
        });
        textBox.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Down),
            Command = new DelegateCommand(() =>
            {
                actionAfterSave = ActionAfterSave.MoveDown;
                Despawn(true);
                actionAfterSave = ActionAfterSave.None;
            })
        });
        textBox.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Escape),
            Command = new DelegateCommand(() => Despawn(false))
        });

        if (!AttachAsAdorner(parent, position, textBox))
            return;

        textBox.LostFocus += ElementLostFocus;
        
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

public abstract class BasePhantomCompletionComboBox : PhantomControlBase<CompletionComboBox>
{
    protected void Spawn(Visual parent, Rect position, string? searchText, IEnumerable? items, object? value)
    {
        var flagsComboBox = new CompletionComboBox();
        flagsComboBox.Items = items;
        flagsComboBox.SelectedItem = value;
        flagsComboBox.HideButton = true;
        flagsComboBox.IsLightDismissEnabled = false; // we are handling it ourselves, without doing .Handled = true so that as soon as user press outside of popup, the click is treated as actual click
        flagsComboBox.Closed += CompletionComboBoxOnClosed;
        
        if (!AttachAsAdorner(parent, position, flagsComboBox, true))
            return;

        DispatcherTimer.RunOnce(() =>
        {
            flagsComboBox.IsDropDownOpen = true;
            flagsComboBox.SearchText = searchText ?? "";
        }, TimeSpan.FromMilliseconds(1));
    }

    private void CompletionComboBoxOnClosed()
    {
        Despawn(true);
    }

    protected override void Cleanup(CompletionComboBox element)
    {
        element.Closed -= CompletionComboBoxOnClosed;
    }
}
