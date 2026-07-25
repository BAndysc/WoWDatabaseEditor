using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.Services.Mcp;
using WDE.Common.Sessions;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

namespace WDE.Mcp.Tools.Documents;

public sealed class DocumentInfo
{
    public required int Index { get; init; }
    public required string Title { get; init; }
    public required bool IsModified { get; init; }
    public required bool IsActive { get; init; }
    public required string Type { get; init; }
    public required bool CanGenerateSql { get; init; }
    public required bool CanReportProblems { get; init; }
}

[AutoRegister]
[SingleInstance]
public class DocumentsListTool : McpTool<EmptyInput, List<DocumentInfo>>
{
    private readonly IDocumentManager documentManager;

    public DocumentsListTool(IDocumentManager documentManager)
    {
        this.documentManager = documentManager;
    }

    public override string Name => "documents_list";
    public override string Description => "Lists documents currently open in the editor. The returned index is used by document_get_problems, document_generate_sql and document_save.";

    protected override Task<List<DocumentInfo>> Execute(EmptyInput input, CancellationToken token)
    {
        var result = new List<DocumentInfo>();
        for (int i = 0; i < documentManager.OpenedDocuments.Count; ++i)
        {
            var document = documentManager.OpenedDocuments[i];
            result.Add(new DocumentInfo
            {
                Index = i,
                Title = document.Title,
                IsModified = document.IsModified,
                IsActive = ReferenceEquals(document, documentManager.ActiveDocument),
                Type = document.GetType().Name,
                CanGenerateSql = document is ISolutionItemDocument,
                CanReportProblems = document is IProblemSourceDocument
            });
        }
        return Task.FromResult(result);
    }
}

public sealed class DocumentIndexInput
{
    [Description("Document index as returned by documents_list")]
    public required int Index { get; init; }
}

public abstract class DocumentToolBase<TOutput> : McpTool<DocumentIndexInput, TOutput>
{
    protected readonly IDocumentManager DocumentManager;

    protected DocumentToolBase(IDocumentManager documentManager)
    {
        DocumentManager = documentManager;
    }

    protected IDocument GetDocument(DocumentIndexInput input)
    {
        if (input.Index < 0 || input.Index >= DocumentManager.OpenedDocuments.Count)
            throw new McpToolException($"No document with index {input.Index}; call documents_list first");
        return DocumentManager.OpenedDocuments[input.Index];
    }
}

public sealed class ProblemInfo
{
    public required string Severity { get; init; }
    public required string Message { get; init; }
    public required int Line { get; init; }
}

[AutoRegister]
[SingleInstance]
public class DocumentGetProblemsTool : DocumentToolBase<List<ProblemInfo>>
{
    public DocumentGetProblemsTool(IDocumentManager documentManager) : base(documentManager) { }

    public override string Name => "document_get_problems";
    public override string Description => "Returns the current validation problems (inspections/diagnostics) of an open document, i.e. a smart script editor's warnings.";

    protected override Task<List<ProblemInfo>> Execute(DocumentIndexInput input, CancellationToken token)
    {
        var document = GetDocument(input);
        if (document is not IProblemSourceDocument problemSource)
            throw new McpToolException($"Document '{document.Title}' does not report problems");

        IReadOnlyList<IInspectionResult>? current = null;
        using (problemSource.Problems.SubscribeAction(problems => current = problems))
        {
        }

        var result = new List<ProblemInfo>();
        if (current != null)
        {
            foreach (var problem in current)
                result.Add(new ProblemInfo
                {
                    Severity = problem.Severity.ToString(),
                    Message = problem.Message,
                    Line = problem.Line
                });
        }
        return Task.FromResult(result);
    }
}

[AutoRegister]
[SingleInstance]
public class DocumentGenerateSqlTool : DocumentToolBase<string>
{
    public DocumentGenerateSqlTool(IDocumentManager documentManager) : base(documentManager) { }

    public override string Name => "document_generate_sql";
    public override string Description => "Generates the SQL that saving the given open document would execute (without executing anything).";

    protected override async Task<string> Execute(DocumentIndexInput input, CancellationToken token)
    {
        var document = GetDocument(input);
        if (document is not ISolutionItemDocument solutionItemDocument)
            throw new McpToolException($"Document '{document.Title}' cannot generate SQL");
        var query = await solutionItemDocument.GenerateQuery();
        return query.QueryString;
    }
}

[AutoRegister]
[SingleInstance]
public class DocumentActivateTool : DocumentToolBase<string>
{
    public DocumentActivateTool(IDocumentManager documentManager) : base(documentManager) { }

    public override string Name => "document_activate";
    public override string Description => "Brings an open document to front (focuses its tab), like clicking it.";
    public override bool Mutating => true;

    protected override Task<string> Execute(DocumentIndexInput input, CancellationToken token)
    {
        var document = GetDocument(input);
        DocumentManager.ActiveDocument = document;
        return Task.FromResult($"Activated '{document.Title}'");
    }
}

[AutoRegister]
[SingleInstance]
public class DocumentCloseTool : DocumentToolBase<string>
{
    public DocumentCloseTool(IDocumentManager documentManager) : base(documentManager) { }

    public override string Name => "document_close";
    public override string Description => "Closes an open document like clicking the tab's X. If the document has unsaved changes, the editor asks the USER whether to save them (the user may also cancel closing).";
    public override bool Mutating => true;

    protected override async Task<string> Execute(DocumentIndexInput input, CancellationToken token)
    {
        var document = GetDocument(input);
        var title = document.Title;
        if (document.CloseCommand == null)
            throw new McpToolException($"Document '{title}' cannot be closed");
        await document.CloseCommand.ExecuteAsync();
        var stillOpen = DocumentManager.OpenedDocuments.Contains(document);
        return stillOpen
            ? $"'{title}' was not closed (the user cancelled or saving was prevented)"
            : $"Closed '{title}'";
    }
}

public sealed class DocumentUndoRedoInput
{
    [Description("Document index as returned by documents_list")]
    public required int Index { get; init; }

    [Description("How many undo/redo steps to perform (default 1)")]
    public int Count { get; init; } = 1;
}

[AutoRegister]
[SingleInstance]
public class DocumentUndoTool : McpTool<DocumentUndoRedoInput, string>
{
    private readonly IDocumentManager documentManager;

    public DocumentUndoTool(IDocumentManager documentManager)
    {
        this.documentManager = documentManager;
    }

    public override string Name => "document_undo";
    public override string Description => "Undoes the last change(s) in an open document, like Ctrl+Z.";
    public override bool Mutating => true;

    protected override Task<string> Execute(DocumentUndoRedoInput input, CancellationToken token)
        => Task.FromResult(UndoRedoHelper.Perform(documentManager, input, undo: true));
}

[AutoRegister]
[SingleInstance]
public class DocumentRedoTool : McpTool<DocumentUndoRedoInput, string>
{
    private readonly IDocumentManager documentManager;

    public DocumentRedoTool(IDocumentManager documentManager)
    {
        this.documentManager = documentManager;
    }

    public override string Name => "document_redo";
    public override string Description => "Redoes previously undone change(s) in an open document, like Ctrl+Y.";
    public override bool Mutating => true;

    protected override Task<string> Execute(DocumentUndoRedoInput input, CancellationToken token)
        => Task.FromResult(UndoRedoHelper.Perform(documentManager, input, undo: false));
}

internal static class UndoRedoHelper
{
    public static string Perform(IDocumentManager documentManager, DocumentUndoRedoInput input, bool undo)
    {
        if (input.Index < 0 || input.Index >= documentManager.OpenedDocuments.Count)
            throw new McpToolException($"No document with index {input.Index}; call documents_list first");
        var document = documentManager.OpenedDocuments[input.Index];
        var command = undo ? document.Undo : document.Redo;
        int performed = 0;
        for (int i = 0; i < Math.Max(1, input.Count); ++i)
        {
            if (!command.CanExecute(null))
                break;
            command.Execute(null);
            performed++;
        }
        var operation = undo ? "undo" : "redo";
        return performed == 0
            ? $"Nothing to {operation} in '{document.Title}'"
            : $"Performed {performed} {operation} step(s) in '{document.Title}'";
    }
}

[AutoRegister]
[SingleInstance]
public class DocumentSaveTool : DocumentToolBase<string>
{
    private readonly ISessionService sessionService;

    public DocumentSaveTool(IDocumentManager documentManager, ISessionService sessionService) : base(documentManager)
    {
        this.sessionService = sessionService;
    }

    public override string Name => "document_save";
    public override string Description => "Saves an open document exactly like pressing its Save button (executes its SQL against the database and records it in the active session, if any).";
    public override bool Mutating => true;

    protected override async Task<string> Execute(DocumentIndexInput input, CancellationToken token)
    {
        var document = GetDocument(input);
        await document.Save.ExecuteAsync();
        if (document is ISolutionItemDocument solutionItemDocument)
            await sessionService.UpdateQuery(solutionItemDocument);
        return $"Saved '{document.Title}'";
    }
}
