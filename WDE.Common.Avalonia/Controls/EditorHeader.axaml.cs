using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Managers;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Controls;

public class EditorHeader : TemplatedControl
{
    public static readonly StyledProperty<long> EntryProperty = AvaloniaProperty.Register<EditorHeader, long>(nameof(Entry));
    public static readonly StyledProperty<string> DisplayNameProperty = AvaloniaProperty.Register<EditorHeader, string>(nameof(DisplayName));
    public static readonly StyledProperty<object?> ToolBarContentProperty = AvaloniaProperty.Register<EditorHeader, object?>(nameof(ToolBarContent));
    public static readonly StyledProperty<object?> RightContentProperty = AvaloniaProperty.Register<EditorHeader, object?>(nameof(RightContent));
    public static readonly StyledProperty<object?> BottomContentProperty = AvaloniaProperty.Register<EditorHeader, object?>(nameof(BottomContent));

    public long Entry
    {
        get => GetValue(EntryProperty);
        set => SetValue(EntryProperty, value);
    }

    public string DisplayName
    {
        get => GetValue(DisplayNameProperty);
        set => SetValue(DisplayNameProperty, value);
    }

    public object? ToolBarContent
    {
        get => GetValue(ToolBarContentProperty);
        set => SetValue(ToolBarContentProperty, value);
    }

    public object? RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    public object? BottomContent
    {
        get => GetValue(BottomContentProperty);
        set => SetValue(BottomContentProperty, value);
    }

    public bool Success
    {
        get => GetValue(SuccessProperty);
        set => SetValue(SuccessProperty, value);
    }

    private TextBlock? idTextBlock;
    private TextBlock? displayNameTextBlock;
    public static readonly StyledProperty<bool> SuccessProperty = AvaloniaProperty.Register<EditorHeader, bool>(nameof(Success));

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        if (idTextBlock != null)
            idTextBlock.PointerReleased -= OnIdPointerReleased;

        if (displayNameTextBlock != null)
            displayNameTextBlock.PointerReleased -= OnDisplayNamePointerReleased;
        
        idTextBlock = e.NameScope.Get<TextBlock>("PART_IdTextBlock");
        displayNameTextBlock = e.NameScope.Get<TextBlock>("PART_DisplayNameTextBlock");

        idTextBlock.PointerReleased += OnIdPointerReleased;
        displayNameTextBlock.PointerReleased += OnDisplayNamePointerReleased;
    }

    private void OnDisplayNamePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton.HasFlagFast(MouseButton.Right))
        {
            DoCopy(DisplayName).ListenErrors();
        }
    }

    private void OnIdPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton.HasFlagFast(MouseButton.Right))
        {
            DoCopy(Entry.ToString()).ListenErrors();
        }
    }

    private async Task DoCopy(string text)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        var statusBar = ViewBind.ResolveViewModel<IStatusBar>();
        
        if (clipboard == null)
        {
            statusBar.PublishNotification(new PlainNotification(NotificationType.Error, "Cannot copy to clipboard"));
            return;
        }
        await clipboard.SetTextAsync(text);
        statusBar.PublishNotification(new PlainNotification(NotificationType.Info, $"Copied '{text}' to the clipboard"));
    }
}