using System;
using Dock.Model.Controls;
using Dock.Model.Core;

namespace WoWDatabaseEditorCore.Avalonia.Docking.Serialization
{
    public class DockSerializer
    {
        private readonly IFactory dockFactory;
        private readonly IToolWrapperModelResolver layoutViewModelResolver;

        public DockSerializer(IFactory factory, IToolWrapperModelResolver layoutViewModelResolver)
        {
            this.dockFactory = factory;
            this.layoutViewModelResolver = layoutViewModelResolver;
        }

        private bool alreadySerializedDocument;
        public SerializedDock Serialize(IDock dock)
        {
            alreadySerializedDocument = false;
            return SerializeDockable(dock);
        }

        public IRootDock Deserialize(SerializedDock serializedDock)
        {
            if (serializedDock.DockableType != SerializedDockableType.RootDock)
                return null;
            
            return DeserializeDockable(serializedDock) as IRootDock;
        }
        
        public IDockable DeserializeDockable(SerializedDock serializedDock)
        {
            switch (serializedDock.DockableType)
            {
                case SerializedDockableType.ProportionalDock:
                    // simplify if need
                    if (serializedDock.Children.Count == 1 && serializedDock.Children[0].DockableType ==
                        SerializedDockableType.ProportionalDock)
                        return DeserializeDockable(serializedDock.Children[0]);
                    IProportionalDock proportionalDock = dockFactory.CreateProportionalDock();
                    proportionalDock.Orientation =
                        serializedDock.Horizontal ? Orientation.Horizontal : Orientation.Vertical;
                    proportionalDock.Proportion = serializedDock.Proportion;
                    DeserializeChildren(proportionalDock, serializedDock);
                    return proportionalDock;
                case SerializedDockableType.DocumentDock:
                    IDocumentDock documentDock = dockFactory.CreateDocumentDock();
                    documentDock.Proportion = serializedDock.Proportion;
                    documentDock.IsCollapsable = serializedDock.IsCollapsable;
                    return documentDock;
                case SerializedDockableType.Splitter:
                    return dockFactory.CreateSplitterDockable();
                case SerializedDockableType.ToolDock:
                    if (serializedDock.Children.Count == 0)
                        return null;
                    
                    IToolDock toolDock = dockFactory.CreateToolDock();
                    toolDock.Proportion = serializedDock.Proportion;
                    DeserializeChildren(toolDock, serializedDock);
                    return toolDock;
                case SerializedDockableType.Tool:
                    var tool = layoutViewModelResolver.ResolveTool(serializedDock.UniqueId);
                    return tool;
                case SerializedDockableType.RootDock:
                    IRootDock rootDock = dockFactory.CreateRootDock();
                    rootDock.Proportion = serializedDock.Proportion;
                    DeserializeChildren(rootDock, serializedDock);
                    if (rootDock.VisibleDockables.Count > 0)
                    {
                        rootDock.ActiveDockable = rootDock.VisibleDockables[0];
                        rootDock.DefaultDockable = rootDock.VisibleDockables[0];
                    }
                    return rootDock;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DeserializeChildren(IDock dock, SerializedDock serializedDock)
        {
            dock.VisibleDockables = dockFactory.CreateList<IDockable>();

            if (serializedDock.Children == null)
                return;
            
            foreach (var dockable in serializedDock.Children)
            {
                var deserialized = DeserializeDockable(dockable);
                if (deserialized != null)
                    dock.VisibleDockables.Add(deserialized);
            }
        }

        private SerializedDock SerializeDockable(IDockable dockable)
        {
            SerializedDock serialized = new();
            if (dockable is IProportionalDock proportinal)
            {
                serialized.Proportion = proportinal.Proportion;
                serialized.Horizontal = proportinal.Orientation == Orientation.Horizontal;
                serialized.DockableType = SerializedDockableType.ProportionalDock;
            } 
            else if (dockable is IDocumentDock documentDock)
            {
                if (!alreadySerializedDocument)
                {
                    serialized.DockableType = SerializedDockableType.DocumentDock;
                    serialized.Proportion = documentDock.Proportion;
                    serialized.IsCollapsable = false;
                }
                alreadySerializedDocument = true;
            }
            else if (dockable is IToolDock toolDock)
            {
                serialized.DockableType = SerializedDockableType.ToolDock;
                serialized.Proportion = toolDock.Proportion;
            }
            else if (dockable is ISplitterDockable splitterDockable)
            {
                serialized.DockableType = SerializedDockableType.Splitter;
            }
            else if (dockable is ITool tool)
            {
                serialized.DockableType = SerializedDockableType.Tool;
                serialized.UniqueId = tool.Id;
            }
            else if (dockable is IRootDock root)
            {
                serialized.DockableType = SerializedDockableType.RootDock;
            }
            else if (dockable is IDocument document)
            {
                throw new Exception("documents not expected here!");
            }
            else
                throw new Exception("Unexpected dockable type: " + dockable.GetType());

            if (dockable is IDock dock && serialized.DockableType != SerializedDockableType.DocumentDock)
            {
                foreach (var d in dock.VisibleDockables)
                {
                    if (d is not IDocument || d is ITool)
                    {
                        var s = SerializeDockable(d);
                        if (s != null)
                            serialized.Children.Add(s);
                    }
                }
            }

            if (serialized.DockableType == SerializedDockableType.ProportionalDock)
            {
                while (serialized.Children.Count > 0)
                {
                    if (serialized.Children[^1].DockableType == SerializedDockableType.Splitter)
                        serialized.Children.RemoveAt(serialized.Children.Count - 1);
                    else
                        break;
                }
                
                while (serialized.Children.Count > 0)
                {
                    if (serialized.Children[0].DockableType == SerializedDockableType.Splitter)
                        serialized.Children.RemoveAt(0);
                    else
                        break;
                }

                for (int i = serialized.Children.Count - 2; i >= 0; --i)
                {
                    if (serialized.Children[i].DockableType == SerializedDockableType.Splitter &&
                        serialized.Children[i+1].DockableType == SerializedDockableType.Splitter)
                        serialized.Children.RemoveAt(i + 1);
                }

                if (serialized.Children.Count == 1)
                    return serialized.Children[0];
                if (serialized.Children.Count == 0)
                    return null;
            }
            else if (serialized.DockableType == SerializedDockableType.RootDock)
            {
                if (serialized.Children.Count != 1 ||
                    serialized.Children[0].DockableType != SerializedDockableType.ProportionalDock)
                {
                    var proportional = new SerializedDock()
                    {
                        DockableType = SerializedDockableType.ProportionalDock,
                        Proportion = 1,
                        Children = serialized.Children
                    };
                    serialized.Children = new();
                    serialized.Children.Add(proportional);
                }
            }

            return serialized;
        }
    }
}