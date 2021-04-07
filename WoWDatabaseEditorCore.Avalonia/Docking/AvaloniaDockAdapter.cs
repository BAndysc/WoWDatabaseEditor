using System.Collections.Generic;
using Dock.Model.Controls;
using Dock.Model.Core;
using WDE.Common.Managers;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WoWDatabaseEditorCore.Avalonia.Docking.Serialization;
using WoWDatabaseEditorCore.ViewModels;
using IDocument = WDE.Common.Managers.IDocument;
using ITool = WDE.Common.Windows.ITool;

namespace WoWDatabaseEditorCore.Avalonia.Docking
{
    public class AvaloniaDockAdapter : IToolWrapperModelResolver
    {
        private readonly IDocumentManager documentManager;
        private readonly ILayoutViewModelResolver layoutViewModelResolver;
        private Dictionary<IDocument, AvaloniaDocumentDockWrapper> documents = new();
        private Dictionary<ITool, AvaloniaToolDockWrapper> tools = new();
        private DockFactory factory;
        private IDock currentLayout;
        private IRootDock rootLayout;
        private DockSerializer dockSerializer;

        private void ActivateDocument(IDocument document)
        {
            if (documents.TryGetValue(document, out var dockable))
                factory.SetActiveDockable(dockable);
        }

        public AvaloniaDockAdapter(IDocumentManager documentManager, ILayoutViewModelResolver layoutViewModelResolver)
        {
            this.documentManager = documentManager;
            this.layoutViewModelResolver = layoutViewModelResolver;
            factory = new DockFactory();
            dockSerializer = new DockSerializer(factory, this);
        }
        
        public AvaloniaToolDockWrapper ResolveTool(string id)
        {
            var tool = layoutViewModelResolver.ResolveViewModel(id);
            if (tool == null)
                return null;
            
            var toolDockWrapper = new AvaloniaToolDockWrapper(tool);
            tools[tool] = toolDockWrapper;
            tool.Visibility = true;
            return toolDockWrapper;
        }
        
        public SerializedDock SerializeDock()
        {
            return dockSerializer.Serialize(rootLayout);
        }
        
        public IRootDock Initialize(SerializedDock serializedDock)
        {
            if (serializedDock != null)
            {
                rootLayout = dockSerializer.Deserialize(serializedDock);
                if ((rootLayout.VisibleDockables?.Count ?? 0) == 0)
                    rootLayout = null;
            }

            if (rootLayout == null)
            {
                rootLayout = factory.CreateLayout();
                layoutViewModelResolver.LoadDefault();
            }
            
            factory.InitLayout(rootLayout);

            currentLayout = rootLayout.VisibleDockables[0] as IDock;
            
            documentManager.ToObservable(d => d.ActiveDocument).SubscribeAction(active =>
            {
                if (active != null)
                    ActivateDocument(active);
            });
            
            documentManager.OpenedDocuments.ToStream().SubscribeAction(e =>
            {
                if (e.Type == CollectionEventType.Add)
                {
                    var dockable = new AvaloniaDocumentDockWrapper(documentManager, e.Item);
                    documents.Add(e.Item, dockable);
                    factory.InitLayout(dockable);
                    factory.AddDocument(currentLayout, dockable);
                }
                else if (e.Type == CollectionEventType.Remove)
                {
                    if (documents.TryGetValue(e.Item, out var dockable))
                    {
                        dockable.CanReallyClose = true;
                        factory.CloseDockable(dockable);
                        documents.Remove(e.Item);
                    }
                }
            });
            foreach (var tool in documentManager.AllTools)
            {
                tool.ToObservable(t => t.Visibility).SubscribeAction(isVisible =>
                {
                    if (isVisible && !tools.ContainsKey(tool))
                    {
                        var dockable = new AvaloniaToolDockWrapper(tool);
                        tools[tool] = dockable;
                        factory.AddTool(currentLayout, dockable);
                    }
                    else if (!isVisible)
                    {
                        if (tools.TryGetValue(tool, out var dockable))
                        {
                            if (!dockable.IsClosed)
                                factory.CloseDockable(dockable);
                            tools.Remove(tool);
                        }
                    }
                });
            }

            return rootLayout;
        }
    }
}