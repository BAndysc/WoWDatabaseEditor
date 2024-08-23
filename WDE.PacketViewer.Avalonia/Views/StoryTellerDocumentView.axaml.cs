using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using Prism.Commands;
using WDE.Common.Avalonia.Components;
using WDE.PacketViewer.Utils;
using WDE.PacketViewer.ViewModels;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.Avalonia.Views;

public partial class StoryTellerDocumentView : UserControl
{
    private ContextMenu contextMenu_guid;
    private ContextMenu contextMenu_packetId;
    private MenuItem copyGuid;
    private MenuItem excludeGuid;
    private MenuItem includeGuid;
    private MenuItem goToPacket;
    private UniversalGuid lastGuid;
    private long lastPacketId;
    private Point lastMousePosition;

    public StoryTellerDocumentView()
    {
        copyGuid = new MenuItem()
        {
            Header = "Copy guid",
            Command = new DelegateCommand(CopyLastGuid)
        };
        excludeGuid = new MenuItem()
        {
            Header = "Exclude guid",
            Command = new DelegateCommand(ExcludeLastGuid)
        };
        includeGuid = new MenuItem()
        {
            Header = "Include guid",
            Command = new DelegateCommand(IncludeLastGuid)
        };
        contextMenu_guid = new ContextMenu()
        {
            ItemsSource = new MenuItem[]
            {
                copyGuid,
                excludeGuid,
                includeGuid
            }
        };

        goToPacket = new MenuItem()
        {
            Header = "Go to packet",
            Command = new DelegateCommand(GoToLastPacket)
        };
        contextMenu_packetId = new ContextMenu()
        {
            ItemsSource = new MenuItem[]
            {
                goToPacket
            }
        };

        InitializeComponent();

        Editor.AddHandler(PointerPressedEvent, Editor_OnPointerPressed, RoutingStrategies.Tunnel);
    }

    private void GoToLastPacket()
    {
        if (DataContext is not StoryTellerDocumentViewModel viewModel)
            return;

        viewModel.GoToPacket(lastPacketId);
    }

    private void IncludeLastGuid()
    {
        if (DataContext is not StoryTellerDocumentViewModel viewModel)
            return;

        viewModel.IncludeGuid(lastGuid);
    }

    private void ExcludeLastGuid()
    {
        if (DataContext is not StoryTellerDocumentViewModel viewModel)
            return;

        viewModel.ExcludeGuid(lastGuid);
    }

    private void CopyLastGuid()
    {
        TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(lastGuid.ToWowParserString());
    }

    private void Editor_OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        Editor.ContextMenu = null;
        lastGuid = default;

        if (DataContext is not StoryTellerDocumentViewModel viewModel)
            return;

        var textView = Editor.TextArea.TextView;
        if (textView == null)
            return;

        if (!e.TryGetPosition(Editor, out var pos))
            return;

        if (GetBracketsText(Editor, pos) is not { } text)
            return;

        if (text.StartsWith("(GUID "))
        {
            var guidText = text.Substring(6, text.Length - 7);

            if (!int.TryParse(guidText, out var guidId))
                return;

            if (!viewModel.TryGetRealGuid(guidId, out var guid))
                return;

            Editor.ContextMenu = contextMenu_guid;
            lastGuid = guid;
        }
        else if (text == "(me)" && viewModel.PlayerGuid is { } playerGuid)
        {
            Editor.ContextMenu = contextMenu_guid;
            lastGuid = playerGuid;
        }
        else if (TryExtractPacketNumber(text, out var packetId))
        {
            Editor.ContextMenu = contextMenu_packetId;
            lastPacketId = packetId;
        }
    }

    private bool TryExtractPacketNumber(string text, out long packetId)
    {
        packetId = default;
        if (text.StartsWith("["))
        {
            var packetIdText = text.Substring(1, text.Length - 2);

            return long.TryParse(packetIdText, out packetId);
        }
        return false;
    }

    private string? GetBracketsText(TextEditor editor, Point position)
    {
        var offset = Editor.GetPositionFromPoint(position);
        if (!offset.HasValue)
            return null;

        var startOffset = FindOpeningBrace(Editor.Document, Editor.Document.GetOffset(offset.Value.Location), 100);
        if (startOffset == -1)
            return null;

        var endOffset = FindClosingBrace(Editor.Document, Editor.Document.GetOffset(offset.Value.Location), 100);
        if (endOffset == -1)
            return null;

        try
        {
            var brace = Editor.Document.GetText(startOffset, endOffset - startOffset + 1);
            return brace;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private int FindOpeningBrace(TextDocument document, int startOffset, int maxLength)
    {
        while (startOffset >= 0 && maxLength-- >= 0)
        {
            var charAt = document.GetCharAt(startOffset);
            if (charAt == '(' || charAt == '[')
                return startOffset;
            if (charAt == '\n')
                return -1;
            startOffset--;
        }

        return maxLength == 0 ? -1 : startOffset;
    }

    private int FindClosingBrace(TextDocument document, int startOffset, int maxLength)
    {
        var length = document.TextLength;
        while (startOffset < length && maxLength-- >= 0)
        {
            var charAt = document.GetCharAt(startOffset);
            if (charAt == ')' || charAt == ']')
                return startOffset;
            if (charAt == '\n')
                return -1;
            startOffset++;
        }

        return maxLength == 0 ? -1 : startOffset;
    }

    private void Editor_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        lastMousePosition = e.GetPosition(Editor);
    }

    private void Editor_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.KeyModifiers.HasFlagFast(KeyModifiers.Control) || e.KeyModifiers.HasFlagFast(KeyModifiers.Meta))
        {
            var position = e.GetPosition(Editor);

            if (GetBracketsText(Editor, position) is not { } text)
                return;

            if (TryExtractPacketNumber(text, out var packetId))
            {
                if (DataContext is not StoryTellerDocumentViewModel viewModel)
                    return;

                viewModel.GoToPacket(packetId);
            }
        }
    }
}