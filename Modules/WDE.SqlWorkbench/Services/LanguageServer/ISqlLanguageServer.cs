using System;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.LanguageServer;

[UniqueProvider]
internal interface ISqlLanguageServer
{
    Task<LanguageServerConnectionId> ConnectAsync(Guid credentialsId, DatabaseCredentials credentials);
    Task<ISqlLanguageServerFile> NewFileAsync(LanguageServerConnectionId connection);
}