using System.Collections.Generic;
using Dock.Model.Controls;
using Dock.Model.Core;
using WDE.Common.Managers;
using WDE.Common.Windows;
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
        private IDock? currentLayout;
        private IRootDock? rootLayout;
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
        
        public AvaloniaToolDockWrapper? ResolveTool(string id)
        {
            var tool = layoutViewModelResolver.ResolveViewModel(id);
            if (tool == null)
                return null;
            
            var toolDockWrapper = new AvaloniaToolDockWrapper(documentManager, tool);
            tools[tool] = toolDockWrapper;
            tool.Visibility = true;
            return toolDockWrapper;
        }
        
        public SerializedDock? SerializeDock()
        {
            if (rootLayout == null)
                return null;
            return dockSerializer.Serialize(rootLayout);
        }
        
        public IRootDock? Initialize(SerializedDock? serializedDock)
        {
            IReadOnlyList<IDockable>? dockablesToPin = null;
            if (serializedDock != null)
            {
                rootLayout = dockSerializer.Deserialize(serializedDock, out dockablesToPin);
                if ((rootLayout?.VisibleDockables?.Count ?? 0) == 0)
                    rootLayout = null;
            }

            if (rootLayout == null)
            {
                rootLayout = factory.CreateLayout();
                layoutViewModelResolver.LoadDefault();
            }
            
            factory.InitLayout(rootLayout);

            if (dockablesToPin != null)
            {
                foreach (var x in dockablesToPin)
                    factory.PinDockable(x);
            }

            currentLayout = rootLayout!.VisibleDockables![0] as IDock;

            if (currentLayout == null)
                return null;
            
            documentManager.ToObservable(d => d.ActiveDocument).SubscribeAction(active =>
            {
                if (active != null && !documentManager.BackgroundMode)
                    ActivateDocument(active);
            });
            
            documentManager.OpenedDocuments.ToStream(false).SubscribeAction(e =>
            {
                if (e.Type == CollectionEventType.Add)
                {
                    var dockable = new AvaloniaDocumentDockWrapper(documentManager, e.Item);
                    documents.Add(e.Item, dockable);
                    factory.InitLayout(dockable);
                    factory.AddDocument(currentLayout!, dockable);
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
                        var dockable = new AvaloniaToolDockWrapper(documentManager, tool);
                        tools[tool] = dockable;
                        if (tool.PreferedPosition == ToolPreferedPosition.DocumentCenter)
                            factory.AddToolAsDocument(currentLayout!, dockable);
                        else
                            factory.AddTool(currentLayout!, dockable, tool.PreferedPosition);
                        factory.SetActiveDockable(dockable);
                    } else if (isVisible && tools.TryGetValue(tool, out var visibleTool))
                    {
                        factory.SetActiveDockable(visibleTool);
                    }
                    else if (!isVisible)
                    {
                        if (tools.TryGetValue(tool, out var dockable))
                        {
                            if (!dockable.IsClosed)
                            {
                                if (factory.IsDockablePinned(dockable, factory.FindRoot(dockable, _ => true)!))
                                {
                                    factory.PinDockable(dockable);
                                }
                                factory.CloseDockable(dockable);
                            }
                            tools.Remove(tool);
                        }
                    }
                });
            }

            return rootLayout;
        }
    }
}