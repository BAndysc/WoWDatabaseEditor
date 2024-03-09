using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using WDE.Common.Managers;
using WDE.Common.Types;

namespace WoWDatabaseEditorCore.Avalonia.Controls;

public class FakeWindow : TemplatedControl
{
    public static readonly StyledProperty<ImageUri> IconProperty = AvaloniaProperty.Register<FakeWindow, ImageUri>(nameof(Icon));
    public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<FakeWindow, string>(nameof(Title));

    public static readonly StyledProperty<object?> ContentProperty = AvaloniaProperty.Register<FakeWindow, object?>(nameof (Content));

    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public ImageUri Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        var closeButton = e.NameScope.Get<Button>("PART_CloseButton");
        closeButton.Click += (sender, args) => Close();
    }

    private IDialog? boundDialog;
    private IClosableDialog? boundWindow;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        BindToDataContext();
    }

    private void BindToDataContext()
    {
        if (boundDialog != null)
        {
            boundDialog.CloseCancel -= DialogWindowOnCloseCancel;
            boundDialog.CloseOk -= DialogWindowOnCloseOk;
            boundDialog = null;
        }
        if (boundWindow != null)
        {
            boundWindow.Close -= WindowOnClose;
            boundWindow = null;
        }
        if (DataContext is IDialog dialogWindow)
        {
            dialogWindow.CloseCancel += DialogWindowOnCloseCancel;
            dialogWindow.CloseOk += DialogWindowOnCloseOk;
            boundDialog = dialogWindow;
        }
        if (DataContext is IClosableDialog closable)
        {
            closable.Close += WindowOnClose;
            boundWindow = closable;
        }
    }

    private void WindowOnClose()
    {
        DoActualClose(true);
    }

    private void DialogWindowOnCloseOk()
    {
        DoActualClose(true);
    }

    private void DialogWindowOnCloseCancel()
    {
        DoActualClose(false);
    }

    private void DoActualClose(bool dialogResult)
    {
        Closing?.Invoke(this, EventArgs.Empty);
        var owner = this.FindAncestorOfType<FakeWindowControl>();
        this.FindAncestorOfType<PseudoWindowsPanel>()!
            .CloseWindow(owner!, dialogResult);
    }

    private void Close()
    {
        if (DataContext is IClosableDialog closableDialog)
        {
            closableDialog.OnClose();
        }
        else
            DoActualClose(false);
    }

    public event EventHandler? Closing;
}