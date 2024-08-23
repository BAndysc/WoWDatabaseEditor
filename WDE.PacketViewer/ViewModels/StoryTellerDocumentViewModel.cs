using System;
using System.Collections.Generic;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.ViewModels;

public class StoryTellerDocumentViewModel : ObservableBase, IDocument
{
    private readonly Dictionary<int, UniversalGuid> guidMapping;
    private readonly IDocumentManager documentManager;
    private WeakReference<PacketDocumentViewModel>? parentSniffDocument;
    public PacketDocumentViewModel? ParentSniffDocument => parentSniffDocument?.TryGetTarget(out var target) == true ? target : null;

    public ICommand Undo => AlwaysDisabledCommand.Command;
    public ICommand Redo => AlwaysDisabledCommand.Command;
    public IHistoryManager? History => null;
    public bool IsModified => false;
    public string Title { get; set; }
    public ICommand Copy => AlwaysDisabledCommand.Command;
    public ICommand Cut => AlwaysDisabledCommand.Command;
    public ICommand Paste => AlwaysDisabledCommand.Command;
    public IAsyncCommand Save => AlwaysDisabledAsyncCommand.Command;
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;
    public ImageUri? Icon { get; } = new ImageUri("Icons/document_shortfilm.png");

    public INativeTextDocument Document { get; }
    public UniversalGuid? PlayerGuid { get; }

    public StoryTellerDocumentViewModel(Dictionary<int, UniversalGuid> guidMapping, string text,
        UniversalGuid? playerGuid,
        PacketDocumentViewModel? viewModel,
        IDocumentManager documentManager,
        INativeTextDocument nativeTextDocument)
    {
        this.guidMapping = guidMapping;
        this.documentManager = documentManager;
        Document = nativeTextDocument;
        Title = viewModel?.Title + " Story";
        PlayerGuid = playerGuid;
        if (viewModel != null)
            parentSniffDocument = new WeakReference<PacketDocumentViewModel>(viewModel);
        nativeTextDocument.FromString(text);
    }

    public bool TryGetRealGuid(int guidId, out UniversalGuid guid)
    {
        if (guidMapping.TryGetValue(guidId, out guid))
            return true;
        guid = default;
        return false;
    }

    public void IncludeGuid(UniversalGuid guid)
    {
        if (ParentSniffDocument is { } parent)
        {
            parent.FilterData.IncludeGuid(guid);
            parent.ApplyFilterCommand.ExecuteAsync().ListenErrors();
        }
    }

    public void ExcludeGuid(UniversalGuid guid)
    {
        if (ParentSniffDocument is { } parent)
        {
            parent.FilterData.ExcludeGuid(guid);
            parent.ApplyFilterCommand.ExecuteAsync().ListenErrors();
        }
    }

    public void GoToPacket(long packetNumber)
    {
        if (ParentSniffDocument is { } parent)
        {
            parent.SelectPacket(packetNumber);
            documentManager.ActiveDocument = parent;
        }
    }
}