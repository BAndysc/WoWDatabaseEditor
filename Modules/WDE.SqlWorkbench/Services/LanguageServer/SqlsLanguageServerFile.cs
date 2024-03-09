using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using WDE.Common;
using WDE.MVVM.Observable;

namespace WDE.SqlWorkbench.Services.LanguageServer;

internal class SqlsLanguageServerFile : ISqlLanguageServerFile
{
    private readonly SqlsLanguageServer sqls;
    private readonly LanguageServerConnectionId connectionId;
    private readonly string fileUri;
    private LanguageClient client;
    private readonly ReactiveProperty<bool> serverAlive = new(true);

    public IReadOnlyReactiveProperty<bool> ServerAlive => serverAlive;

    public LanguageServerConnectionId ConnectionId => connectionId;
    
    public SqlsLanguageServerFile(SqlsLanguageServer sqls, LanguageServerConnectionId connectionId,
        LanguageClient client, long fileId)
    {
        this.sqls = sqls;
        this.client = client;
        this.connectionId = connectionId;
        fileUri = fileId.ToString();

        Reconnect(client);
    }
    
    public void Reconnect(LanguageClient newClient)
    {
        client = newClient;
        client.DidOpenTextDocument(new DidOpenTextDocumentParams()
        {
            TextDocument = new TextDocumentItem()
            {
                LanguageId = "sql",
                Text = "",
                Uri = fileUri
            }
        });
    }
    
    public void Dispose()
    {
        try
        {
            sqls.UnregisterFile(this);
            client.DidCloseTextDocument(new DidCloseTextDocumentParams()
            {
                TextDocument = fileUri
            });
        }
        catch (Exception e)
        {
            LOG.LogWarning(e);
        }
    }

    public async Task<Hover?> GetHoverAsync(int line, int column)
    {
        try
        {
            var hover = await client.RequestHover(new HoverParams()
            {
                TextDocument = fileUri,
                Position = new Position(line - 1, column - 1)
            });
            return hover;
        }
        catch (Exception e)
        {
            LOG.LogWarning(e);
            return null;
        }
    }
    
    public async Task<IReadOnlyList<CompletionItem>> GetCompletionsAsync(int line, int column)
    {
        try
        {
            var completions = await client.RequestCompletion(new CompletionParams
            {
                TextDocument = fileUri,
                Position = new Position(line - 1, column - 1),
                Context = null,
                WorkDoneToken = null,
                PartialResultToken = null
            });
            return completions.Items.ToList();
        }
        catch (Exception e)
        {
            LOG.LogWarning(e);
            return new List<CompletionItem>();
        }
    }

    public async Task<SignatureHelp?> GetSignatureHelpAsync(int line, int column)
    {
        try
        {
            return await client.RequestSignatureHelp(new SignatureHelpParams()
            {
                TextDocument = fileUri,
                Context = new SignatureHelpContext() { TriggerCharacter = "," },
                Position = new Position(line - 1, column - 1)
            });
        }
        catch (Exception e)
        {
            LOG.LogWarning(e);
            return null;
        }
    }
    
    public async Task<IReadOnlyList<TextEdit>> FormatAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await client.RequestDocumentFormatting(new DocumentFormattingParams()
            {
                TextDocument = fileUri,
                Options = new FormattingOptions()
                {
                    InsertSpaces = true,
                    TabSize = 4
                }
            }, cancellationToken: cancellationToken).WaitAsync(cancellationToken);
            return (IReadOnlyList<TextEdit>?)result?.Select(x => new TextEdit()
                {
                    NewText = x.NewText,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                        x.Range.Start.Line + 1, x.Range.Start.Character + 1,
                        x.Range.End.Line + 1, x.Range.End.Character + 1)
                }
            ).ToList() ?? Array.Empty<TextEdit>();
        }
        catch (TaskCanceledException)
        {
            // sqls might hang on the format request and it doesn't support cancellation,
            // the only way to cancel it is to kill the process and let it restart
            sqls.KillProcess(connectionId);
        }
        catch (Exception e)
        {
            LOG.LogWarning(e);
        }
        

        return Array.Empty<TextEdit>();
    }

    public async Task RestartLanguageServerAsync()
    {
        await sqls.RestartLanguageServerAsync(connectionId);
    }

    public async Task ChangeDatabaseAsync(string databaseName)
    {
        await client.ExecuteCommand(new Command() { Name = "switchDatabase", Arguments = new JArray() { new JValue(databaseName) } });
    }

    public async Task<IReadOnlyList<TextEdit>> FormatRangeAsync(int startLine, int startColumn, int endLine, int endColumn, CancellationToken cancellationToken)
    {
        try
        {
            var result = await client.RequestDocumentRangeFormatting(new DocumentRangeFormattingParams()
            {
                TextDocument = fileUri,
                Options = new FormattingOptions()
                {
                    InsertSpaces = true,
                    TabSize = 4
                },
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range()
                {
                    Start = new Position(startLine - 1, startColumn - 1),
                    End = new Position(endLine - 1, endColumn - 1)
                }
            }, cancellationToken: cancellationToken).WaitAsync(cancellationToken);
            return (IReadOnlyList<TextEdit>?)result?.Select(x => new TextEdit(){
                NewText = x.NewText, 
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                    x.Range.Start.Line + 1, x.Range.Start.Character + 1,
                    x.Range.End.Line + 1, x.Range.End.Character + 1)}
            ).ToList() ?? Array.Empty<TextEdit>();
        }
        catch (TaskCanceledException)
        {
            // sqls might hang on the format request and it doesn't support cancellation,
            // the only way to cancel it is to kill the process and let it restart
            sqls.KillProcess(connectionId);
        }
        catch (Exception e)
        {
            LOG.LogWarning(e);
        }

        return Array.Empty<TextEdit>(); 
    }
    
    public void UpdateText(string text)
    {
        try
        {
            client.DidChangeTextDocument(new DidChangeTextDocumentParams()
            {
                TextDocument = new OptionalVersionedTextDocumentIdentifier()
                {
                    Uri = fileUri,
                    Version = 1
                },
                ContentChanges = new Container<TextDocumentContentChangeEvent>(new TextDocumentContentChangeEvent()
                {
                    Text = text
                })
            });
        }
        catch (Exception e)
        {
            LOG.LogWarning(e);
        }
    }

    public void NotifyLanguageServerDied()
    {
        serverAlive.Value = false;
    }
    
    public void NotifyLanguageServerAlive()
    {
        serverAlive.Value = true;
    }
}