using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using WDE.MVVM.Observable;

namespace WDE.SqlWorkbench.Services.LanguageServer;

internal interface ISqlLanguageServerFile : System.IDisposable
{
    void UpdateText(string text);
    Task<IReadOnlyList<CompletionItem>> GetCompletionsAsync(int line, int column);
    Task<Hover?> GetHoverAsync(int line, int column);
    Task<SignatureHelp?> GetSignatureHelpAsync(int line, int column);
    Task<IReadOnlyList<TextEdit>> FormatRangeAsync(int startLine, int startColumn, int endLine, int endColumn, CancellationToken cancellationToken);
    Task<IReadOnlyList<TextEdit>> FormatAsync(CancellationToken cancellationToken);
    Task RestartLanguageServerAsync();
    Task ChangeDatabaseAsync(string databaseName);
    IReadOnlyReactiveProperty<bool> ServerAlive { get; }
}