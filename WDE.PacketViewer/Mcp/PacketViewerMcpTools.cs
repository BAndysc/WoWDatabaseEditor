using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services.Mcp;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.ProcessorProviders;
using WDE.PacketViewer.Solutions;
using WDE.PacketViewer.ViewModels;

namespace WDE.PacketViewer.Mcp
{
    internal static class PacketMcpHelpers
    {
        public static PacketDocumentViewModel GetPacketDocument(IDocumentManager documentManager, int index)
        {
            if (index < 0 || index >= documentManager.OpenedDocuments.Count)
                throw new McpToolException($"No document with index {index}; call packet_documents (or documents_list) first");
            if (documentManager.OpenedDocuments[index] is not PacketDocumentViewModel packetDocument)
                throw new McpToolException($"Document '{documentManager.OpenedDocuments[index].Title}' is not a packet viewer document; call packet_documents to list packet viewer documents");
            return packetDocument;
        }

        /// <summary>Waits until the document is neither loading the sniff nor filtering packets.</summary>
        public static async Task WaitUntilIdle(PacketDocumentViewModel document, CancellationToken token)
        {
            while (document.LoadingInProgress || document.FilteringInProgress)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(50, token);
            }
        }

        /// <summary>Sets the filter text and applies it exactly like the user typing it and pressing apply.</summary>
        public static async Task<int> SetFilterAndApply(PacketDocumentViewModel document, string filter, CancellationToken token)
        {
            await WaitUntilIdle(document, token);
            document.FilterText.FromString(filter);
            await document.ApplyFilterCommand.ExecuteAsync();
            await WaitUntilIdle(document, token);
            return document.FilteredPackets.Count;
        }
    }

    public sealed class PacketDocumentInfo
    {
        [Description("Index of the document in the shared open-documents index space (same as documents_list)")]
        public required int Index { get; init; }
        public required string Title { get; init; }
        [Description("Total number of loaded packets (split packets if 'split UPDATE_OBJECT' is enabled)")]
        public required int TotalPackets { get; init; }
        [Description("Number of packets matching the current filter")]
        public required int FilteredPackets { get; init; }
        [Description("Current packet filter text (empty = no filter)")]
        public required string FilterText { get; init; }
        [Description("Whether 'split UPDATE_OBJECT' mode is enabled (each object in an UPDATE_OBJECT packet becomes its own pseudo packet)")]
        public required bool SplitUpdateEnabled { get; init; }
        [Description("True while the document is still loading the sniff or filtering; other packet tools wait for this automatically")]
        public required bool Busy { get; init; }
    }

    [AutoRegister]
    [SingleInstance]
    public class PacketDocumentsTool : McpTool<EmptyInput, List<PacketDocumentInfo>>
    {
        private readonly IDocumentManager documentManager;

        public PacketDocumentsTool(IDocumentManager documentManager)
        {
            this.documentManager = documentManager;
        }

        public override string Name => "packet_documents";
        public override string Description => "Lists open packet viewer (sniff) documents with their packet counts and current filter. The returned index is shared with documents_list and is used by packet_set_filter and packet_dump.";

        protected override Task<List<PacketDocumentInfo>> Execute(EmptyInput input, CancellationToken token)
        {
            var result = new List<PacketDocumentInfo>();
            for (int i = 0; i < documentManager.OpenedDocuments.Count; ++i)
            {
                if (documentManager.OpenedDocuments[i] is not PacketDocumentViewModel packetDocument)
                    continue;
                result.Add(new PacketDocumentInfo
                {
                    Index = i,
                    Title = packetDocument.Title,
                    TotalPackets = packetDocument.TotalPacketCount,
                    FilteredPackets = packetDocument.FilteredPackets.Count,
                    FilterText = packetDocument.FilterText.ToString(),
                    SplitUpdateEnabled = packetDocument.SplitUpdate,
                    Busy = packetDocument.LoadingInProgress || packetDocument.FilteringInProgress
                });
            }
            return Task.FromResult(result);
        }
    }

    public sealed class PacketDumpersInput
    {
        [Description("Optional case-insensitive substring to filter dumpers by name or description")]
        public string? Filter { get; init; }
    }

    public sealed class PacketDumperInfo
    {
        [Description("Dumper name, pass it to packet_dump")]
        public required string Name { get; init; }
        public required string Description { get; init; }
        [Description("'text' = produces plain text, 'document' = produces an editor document")]
        public required string Kind { get; init; }
        [Description("True if packet_dump can return this dumper's output as text (all 'text' dumpers and the story teller document dumpers)")]
        public required bool ProducesText { get; init; }
        [Description("True if the dumper needs 'split UPDATE_OBJECT' mode; packet_dump enables it automatically when needed")]
        public required bool RequiresSplitUpdateObject { get; init; }
        [Description("Output file extension for text dumpers (i.e. 'txt', 'sql')")]
        public string? Extension { get; init; }
    }

    [AutoRegister]
    [SingleInstance]
    public class PacketDumpersTool : McpTool<PacketDumpersInput, List<PacketDumperInfo>>
    {
        private readonly IEnumerable<IPacketDumperProvider> dumperProviders;

        public PacketDumpersTool(IEnumerable<IPacketDumperProvider> dumperProviders)
        {
            this.dumperProviders = dumperProviders;
        }

        public override string Name => "packet_dumpers";
        public override string Description => "Lists the packet dumpers/processors available in the packet viewer (the same list the UI offers under 'run processors'). Use packet_dump to run one. The most useful one for understanding a sniff is 'Story teller' which produces a human readable narrative of the sniff.";

        protected override Task<List<PacketDumperInfo>> Execute(PacketDumpersInput input, CancellationToken token)
        {
            var result = new List<PacketDumperInfo>();
            foreach (var provider in dumperProviders)
            {
                if (!string.IsNullOrEmpty(input.Filter) &&
                    !provider.Name.Contains(input.Filter, StringComparison.OrdinalIgnoreCase) &&
                    !provider.Description.Contains(input.Filter, StringComparison.OrdinalIgnoreCase))
                    continue;

                var textProvider = provider as ITextPacketDumperProvider;
                // Story teller dumpers technically produce a document, but that document is a plain text
                // story which packet_dump knows how to extract, so they are marked as producing text.
                bool producesText = textProvider != null ||
                                    provider is StoryTellerDumperProvider ||
                                    provider is PerGuidStoryTellerDumperProvider;
                result.Add(new PacketDumperInfo
                {
                    Name = provider.Name,
                    Description = provider.Description,
                    Kind = textProvider != null ? "text" : "document",
                    ProducesText = producesText,
                    RequiresSplitUpdateObject = provider.RequiresSplitUpdateObject,
                    Extension = textProvider?.Extension
                });
            }
            return Task.FromResult(result);
        }
    }

    public sealed class PacketSetFilterInput
    {
        [Description("Index of the packet viewer document as returned by packet_documents")]
        public required int DocumentIndex { get; init; }
        [Description("Packet filter expression, exactly as typed in the filter box. Empty string clears the filter. Fields: packet.opcode (string), packet.id (number), packet.original_id, packet.entry, packet.text (full packet text). Operators: == != < <= > >= && || ! 'in' (substring test), strings in single or double quotes. Examples: \"packet.opcode == 'SMSG_SPELL_GO'\", \"'QUEST' in packet.opcode || packet.entry == 12345\", \"'Hogger' in packet.text\"")]
        public required string Filter { get; init; }
    }

    public sealed class PacketSetFilterOutput
    {
        [Description("Number of packets matching the new filter")]
        public required int FilteredPackets { get; init; }
        public required int TotalPackets { get; init; }
    }

    [AutoRegister]
    [SingleInstance]
    public class PacketSetFilterTool : McpTool<PacketSetFilterInput, PacketSetFilterOutput>
    {
        private readonly IDocumentManager documentManager;

        public PacketSetFilterTool(IDocumentManager documentManager)
        {
            this.documentManager = documentManager;
        }

        public override string Name => "packet_set_filter";
        public override string Description => "Sets and applies the packet filter of an open packet viewer document, exactly like the user typing the filter and pressing apply. Returns the resulting filtered packet count. Note: an invalid filter shows an error dialog to the USER which must be dismissed before this returns.";
        public override bool Mutating => true;

        protected override async Task<PacketSetFilterOutput> Execute(PacketSetFilterInput input, CancellationToken token)
        {
            var document = PacketMcpHelpers.GetPacketDocument(documentManager, input.DocumentIndex);
            var filtered = await PacketMcpHelpers.SetFilterAndApply(document, input.Filter, token);
            return new PacketSetFilterOutput
            {
                FilteredPackets = filtered,
                TotalPackets = document.TotalPacketCount
            };
        }
    }

    public sealed class PacketDumpInput
    {
        [Description("Index of the packet viewer document as returned by packet_documents")]
        public required int DocumentIndex { get; init; }
        [Description("Dumper name exactly as returned by packet_dumpers, i.e. 'Story teller'")]
        public required string Dumper { get; init; }
        [Description("Optional: set and apply this packet filter first (same syntax and behavior as packet_set_filter). Omit to use the document's current filter, pass empty string to clear the filter")]
        public string? Filter { get; init; }
        [Description("First line of the output to return (0-based), for paging")]
        public int OffsetLines { get; init; } = 0;
        [Description("Maximum number of lines to return (default 500, max 5000)")]
        public int LimitLines { get; init; } = 500;
    }

    public sealed class PacketDumpOutput
    {
        [Description("Total number of lines the dumper produced; page through them with offsetLines/limitLines")]
        public required int TotalLines { get; init; }
        [Description("Total size of the whole output in characters")]
        public required int TotalCharacters { get; init; }
        [Description("Offset of the first returned line")]
        public required int Offset { get; init; }
        public required List<string> Lines { get; init; }
        [Description("True if there are more lines after this page")]
        public required bool Truncated { get; init; }
    }

    [AutoRegister]
    [SingleInstance]
    public class PacketDumpTool : McpTool<PacketDumpInput, PacketDumpOutput>
    {
        private readonly IDocumentManager documentManager;
        private readonly IEnumerable<IPacketDumperProvider> dumperProviders;

        public PacketDumpTool(IDocumentManager documentManager,
            IEnumerable<IPacketDumperProvider> dumperProviders)
        {
            this.documentManager = documentManager;
            this.dumperProviders = dumperProviders;
        }

        public override string Name => "packet_dump";
        public override string Description => "Runs a packet dumper/processor (see packet_dumpers) over the current (filtered) packets of an open packet viewer document and returns its text output, paged by lines. Equivalent to the UI's 'run processors' flow, but headless: no result document is opened. May change document state: applies 'filter' when given and auto-enables 'split UPDATE_OBJECT' when the dumper requires it. Start with the 'Story teller' dumper to understand a sniff.";
        // Not strictly read-only: the optional filter and the automatic split-update enable change the open document.
        public override bool Mutating => true;

        protected override async Task<PacketDumpOutput> Execute(PacketDumpInput input, CancellationToken token)
        {
            var document = PacketMcpHelpers.GetPacketDocument(documentManager, input.DocumentIndex);
            await PacketMcpHelpers.WaitUntilIdle(document, token);

            var dumperName = input.Dumper.Trim();
            var processor = document.Processors.FirstOrDefault(p => string.Equals(p.Name, dumperName, StringComparison.OrdinalIgnoreCase));
            if (processor == null)
                throw new McpToolException($"Unknown dumper '{input.Dumper}'. Available dumpers: {string.Join(", ", document.Processors.Select(p => p.Name))}");

            if (input.Filter != null)
                await PacketMcpHelpers.SetFilterAndApply(document, input.Filter, token);

            var provider = dumperProviders.FirstOrDefault(p => string.Equals(p.Name, processor.Name, StringComparison.OrdinalIgnoreCase));
            if (provider is {RequiresSplitUpdateObject: true} && !document.SplitUpdate)
            {
                // Enable 'split UPDATE_OBJECT' exactly like ticking the checkbox: this splits the
                // packets and re-applies the current filter (otherwise the run would pop a dialog).
                document.SplitUpdate = true;
                await PacketMcpHelpers.WaitUntilIdle(document, token);
            }

            var single = new List<ProcessorViewModel> {processor};
            string text;
            if (processor.IsTextDumper)
            {
                text = await document.RunProcessorsThreaded(single, token)
                       ?? throw new McpToolException("The dumper run was cancelled");
            }
            else
            {
                var generatedDocuments = await document.RunDocumentProcessorsThreaded(single, token)
                                         ?? throw new McpToolException("The dumper run was cancelled");
                if (generatedDocuments.Count == 0)
                    throw new McpToolException("The dumper produced no output");
                var generated = generatedDocuments[0];
                try
                {
                    if (generated is StoryTellerDocumentViewModel story)
                        text = story.Document.ToString();
                    else
                        throw new McpToolException($"Dumper '{processor.Name}' produces an interactive document ({generated.GetType().Name}) without a plain text form, it can only be run from the UI");
                }
                finally
                {
                    // the generated document is never opened in the UI, dispose it right away
                    (generated as IDisposable)?.Dispose();
                }
            }

            var lines = text.ReplaceLineEndings("\n").Split('\n');
            int offset = Math.Max(0, input.OffsetLines);
            int limit = Math.Clamp(input.LimitLines, 1, 5000);
            var page = lines.Skip(offset).Take(limit).ToList();
            return new PacketDumpOutput
            {
                TotalLines = lines.Length,
                TotalCharacters = text.Length,
                Offset = offset,
                Lines = page,
                Truncated = offset + page.Count < lines.Length
            };
        }
    }

    public sealed class PacketOpenInput
    {
        [Description("Absolute path to the sniff file to open (.pkt/.bin raw sniff or .dat parsed packets)")]
        public required string Path { get; init; }
    }

    public sealed class PacketOpenOutput
    {
        [Description("Title of the opened packet viewer document")]
        public required string Title { get; init; }
        [Description("Index of the new document in the shared open-documents index space, if it is already listed")]
        public int? DocumentIndex { get; init; }
        public required string Message { get; init; }
    }

    [AutoRegister]
    [SingleInstance]
    public class PacketOpenTool : McpTool<PacketOpenInput, PacketOpenOutput>
    {
        private readonly IDocumentManager documentManager;
        private readonly IEventAggregator eventAggregator;
        private readonly PacketDocumentSolutionNameProvider solutionNameProvider;

        public PacketOpenTool(IDocumentManager documentManager,
            IEventAggregator eventAggregator,
            PacketDocumentSolutionNameProvider solutionNameProvider)
        {
            this.documentManager = documentManager;
            this.eventAggregator = eventAggregator;
            this.solutionNameProvider = solutionNameProvider;
        }

        public override string Name => "packet_open";
        public override string Description => "Opens a sniff file (.pkt/.bin raw sniff or .dat parsed packets) in the packet viewer, exactly like File -> Open. Parsing and loading continue asynchronously after this returns; call packet_documents to see loading state and packet counts.";
        public override bool Mutating => true;

        protected override Task<PacketOpenOutput> Execute(PacketOpenInput input, CancellationToken token)
        {
            if (!File.Exists(input.Path))
                throw new McpToolException($"File '{input.Path}' does not exist");

            var solutionItem = new PacketDocumentSolutionItem(input.Path);
            // the same event the File -> Open flow publishes to open a solution item as a document
            eventAggregator.GetEvent<EventRequestOpenItem>().Publish(solutionItem);

            int? index = null;
            for (int i = 0; i < documentManager.OpenedDocuments.Count; ++i)
            {
                if (documentManager.OpenedDocuments[i] is PacketDocumentViewModel packetDocument &&
                    ReferenceEquals(packetDocument.SolutionItem, solutionItem))
                {
                    index = i;
                    break;
                }
            }

            return Task.FromResult(new PacketOpenOutput
            {
                Title = solutionNameProvider.GetName(solutionItem),
                DocumentIndex = index,
                Message = "Sniff opened, loading started (parsing can take a while). Use packet_documents to check whether the document is still busy and how many packets are loaded."
            });
        }
    }

    public sealed class PacketListInput
    {
        [Description("Index of the packet viewer document as returned by packet_documents")]
        public required int DocumentIndex { get; init; }
        [Description("First packet row to return (0-based index into the visible packet list), for paging")]
        public int Offset { get; init; } = 0;
        [Description("Maximum number of packets to return (default 100, max 1000)")]
        public int Limit { get; init; } = 100;
    }

    public sealed class PacketListItem
    {
        [Description("Packet number/id, pass it to packet_get")]
        public required int Id { get; init; }
        public required string Opcode { get; init; }
        [Description("Packet timestamp (yyyy-MM-dd HH:mm:ss.fff)")]
        public required string Time { get; init; }
        [Description("Related entry (creature/gameobject/quest...), omitted when none")]
        public uint? Entry { get; init; }
        [Description("Name of the main object of the packet, if known")]
        public string? ObjectName { get; init; }
        [Description("First line of the packet text, truncated to 120 characters")]
        public required string Preview { get; init; }
    }

    public sealed class PacketListOutput
    {
        [Description("Total number of packets in the visible (filtered) list")]
        public required int TotalCount { get; init; }
        public required int Offset { get; init; }
        public required List<PacketListItem> Packets { get; init; }
        [Description("True if there are more packets after this page")]
        public required bool Truncated { get; init; }
    }

    [AutoRegister]
    [SingleInstance]
    public class PacketListTool : McpTool<PacketListInput, PacketListOutput>
    {
        private readonly IDocumentManager documentManager;

        public PacketListTool(IDocumentManager documentManager)
        {
            this.documentManager = documentManager;
        }

        public override string Name => "packet_list";
        public override string Description => "Pages through the packet list of an open packet viewer document as the user currently sees it in the grid (the filtered list, minus player-move packets when that hiding option is enabled). Returns per packet: id, opcode, time, entry, object name and a one-line preview. Use packet_get for the full packet text and packet_set_filter to change what is listed.";

        protected override async Task<PacketListOutput> Execute(PacketListInput input, CancellationToken token)
        {
            var document = PacketMcpHelpers.GetPacketDocument(documentManager, input.DocumentIndex);
            await PacketMcpHelpers.WaitUntilIdle(document, token);

            int offset = Math.Max(0, input.Offset);
            int limit = Math.Clamp(input.Limit, 1, 1000);
            var visible = document.VisiblePackets;
            var packets = new List<PacketListItem>();
            for (int i = offset; i < visible.Count && packets.Count < limit; ++i)
            {
                token.ThrowIfCancellationRequested();
                var packet = visible[i];
                var text = await document.GetPacketTextAsync(packet);
                var newLine = text.IndexOf('\n');
                var preview = (newLine >= 0 ? text[..newLine] : text).TrimEnd('\r').Trim();
                if (preview.Length > 120)
                    preview = preview[..120];
                packets.Add(new PacketListItem
                {
                    Id = packet.Id,
                    Opcode = packet.Opcode,
                    Time = packet.Time.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    Entry = packet.Entry == 0 ? null : packet.Entry,
                    ObjectName = packet.ObjectName,
                    Preview = preview
                });
            }

            return new PacketListOutput
            {
                TotalCount = visible.Count,
                Offset = offset,
                Packets = packets,
                Truncated = offset + packets.Count < visible.Count
            };
        }
    }

    public sealed class PacketGetInput
    {
        [Description("Index of the packet viewer document as returned by packet_documents")]
        public required int DocumentIndex { get; init; }
        [Description("Packet number/id as returned by packet_list")]
        public required int PacketId { get; init; }
        [Description("First character of the packet text to return (0-based), for paging huge packets")]
        public int OffsetChars { get; init; } = 0;
        [Description("Maximum number of characters to return (default 20000)")]
        public int LimitChars { get; init; } = 20000;
    }

    public sealed class PacketGetOutput
    {
        public required int Id { get; init; }
        public required string Opcode { get; init; }
        [Description("Packet timestamp (yyyy-MM-dd HH:mm:ss.fff)")]
        public required string Time { get; init; }
        [Description("Related entry (creature/gameobject/quest...), omitted when none")]
        public uint? Entry { get; init; }
        [Description("Total length of the full packet text in characters")]
        public required int TotalCharacters { get; init; }
        [Description("Offset of the first returned character")]
        public required int Offset { get; init; }
        [Description("The packet text (the detail view content), possibly a page of it")]
        public required string Text { get; init; }
        [Description("True if there is more text after this page")]
        public required bool Truncated { get; init; }
    }

    [AutoRegister]
    [SingleInstance]
    public class PacketGetTool : McpTool<PacketGetInput, PacketGetOutput>
    {
        private readonly IDocumentManager documentManager;

        public PacketGetTool(IDocumentManager documentManager)
        {
            this.documentManager = documentManager;
        }

        public override string Name => "packet_get";
        public override string Description => "Returns the full parsed text of a single packet of an open packet viewer document (what the user sees in the packet detail view), paged by characters for huge packets. The packet must be in the current filtered list (see packet_list; clear or adjust the filter with packet_set_filter if it is not).";

        protected override async Task<PacketGetOutput> Execute(PacketGetInput input, CancellationToken token)
        {
            var document = PacketMcpHelpers.GetPacketDocument(documentManager, input.DocumentIndex);
            await PacketMcpHelpers.WaitUntilIdle(document, token);

            PacketViewModel? packet = null;
            foreach (var candidate in document.FilteredPackets)
            {
                if (candidate.Id == input.PacketId)
                {
                    packet = candidate;
                    break;
                }
            }

            if (packet == null)
                throw new McpToolException($"Packet {input.PacketId} is not in the current filtered packet list of '{document.Title}'; use packet_list to see available packets or packet_set_filter to change/clear the filter");

            var text = await document.GetPacketTextAsync(packet);
            int offset = Math.Clamp(input.OffsetChars, 0, text.Length);
            int limit = Math.Max(1, input.LimitChars);
            var page = text.Substring(offset, Math.Min(limit, text.Length - offset));
            return new PacketGetOutput
            {
                Id = packet.Id,
                Opcode = packet.Opcode,
                Time = packet.Time.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Entry = packet.Entry == 0 ? null : packet.Entry,
                TotalCharacters = text.Length,
                Offset = offset,
                Text = page,
                Truncated = offset + page.Length < text.Length
            };
        }
    }
}
