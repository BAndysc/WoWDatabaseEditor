using System;
using System.IO;
using System.Linq;
using Prism.Ioc;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Solutions;

[AutoRegister]
[SingleInstance]
internal class QueryDocumentSolutionProviders : 
    ISolutionNameProvider<QueryDocumentSolutionItem>,
    ISolutionItemIconProvider<QueryDocumentSolutionItem>,
    ISolutionItemEditorProvider<QueryDocumentSolutionItem>,
    ISolutionItemSerializer<QueryDocumentSolutionItem>,
    ISolutionItemDeserializer<QueryDocumentSolutionItem>
{
    private readonly IContainerProvider containerProvider;
    private readonly Lazy<IConnectionsManager> connectionsManager;

    public QueryDocumentSolutionProviders(IContainerProvider containerProvider,
        Lazy<IConnectionsManager> connectionsManager)
    {
        this.containerProvider = containerProvider;
        this.connectionsManager = connectionsManager;
    }
    
    public string GetName(QueryDocumentSolutionItem item) => item.FileName;

    public ImageUri GetIcon(QueryDocumentSolutionItem icon) => new("Icons/document_sql.png");

    public IDocument GetEditor(QueryDocumentSolutionItem item)
    {
        var connection =
            connectionsManager.Value.StaticConnections.FirstOrDefault(x => x.ConnectionData.Id == item.ConnectionId)
            ?? connectionsManager.Value.DefaultConnection;
        
        if (connection == null)
            throw new Exception("No connection found");
        
        var document = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(IConnection), connection), (typeof(QueryDocumentSolutionItem), item));
        return document;
    }

    public ISmartScriptProjectItem? Serialize(QueryDocumentSolutionItem item, bool forMostRecentlyUsed)
    {
        return new AbstractSmartScriptProjectItem()
        {
            Type = 47,
            Value = item.IsTemporary ? 1 : 0,
            StringValue = item.FileName,
            Comment = item.ConnectionId.ToString()
        };
    }

    public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
    {
        solutionItem = null;
        if (projectItem.Type != 47)
            return false;
        
        if (!Guid.TryParse(projectItem.Comment, out var guid))
            return false;

        solutionItem = new QueryDocumentSolutionItem(projectItem.StringValue ?? Path.GetTempFileName(), guid, projectItem.Value == 1);
        return true;
    }
}