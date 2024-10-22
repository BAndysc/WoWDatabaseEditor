using System;
using System.Collections.Generic;
using System.Linq;
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
        public SerializedDock? Serialize(IDock dock)
        {
            alreadySerializedDocument = false;
            tempPinnedDockables.Clear();
            var result = SerializeDockable(dock);
            foreach (var x in tempPinnedDockables) // pin it back in case this method is called again
                dockFactory.PinDockable(x);
            tempPinnedDockables.Clear();
            return result;
        }

        private SerializedDock? Simplify(SerializedDock dock)
        {
            for (var index = dock.Children.Count - 1; index >= 0; index--)
            {
                var children = dock.Children[index];
                if (Simplify(children) is { } child)
                {
                    dock.Children[index] = child;
                }
                else
                {
                    dock.Children.RemoveAt(index);
                }
            }

            var allChildrenAreSplitters = dock.Children.Count > 0 &&
                                          dock.Children.All(c => c.DockableType == SerializedDockableType.Splitter);
            if (allChildrenAreSplitters)
                return null;

            var nextCanBeSplitter = false;
            for (var index = 0; index < dock.Children.Count; index++)
            {
                var children = dock.Children[index];
                if (!nextCanBeSplitter && children.DockableType == SerializedDockableType.Splitter)
                {
                    dock.Children.RemoveAt(index);
                    index--;
                }
                else
                {
                    nextCanBeSplitter = children.DockableType != SerializedDockableType.Splitter;
                }
            }

            return dock;
        }

        public IRootDock? Deserialize(SerializedDock serializedDock, out IReadOnlyList<IDockable>? dockablesToPin)
        {
            serializedDock = Simplify(serializedDock) ?? new SerializedDock() { DockableType = SerializedDockableType.RootDock };
            dockablesToPin = null;
            if (serializedDock.DockableType != SerializedDockableType.RootDock)
                return null;
            
            tempPinnedDockables.Clear();
            var root = DeserializeDockable(serializedDock) as IRootDock;
            if (tempPinnedDockables.Count > 0)
                dockablesToPin = tempPinnedDockables.ToList();
            tempPinnedDockables.Clear();
            return root;
        }
        
        private IDockable? DeserializeDockable(SerializedDock serializedDock)
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
                    proportionalDock.VisibleDockables = dockFactory.CreateList<IDockable>();
                    DeserializeChildren(proportionalDock.VisibleDockables, serializedDock.Children, true);
                    return proportionalDock;
                case SerializedDockableType.DocumentDock:
                    IDocumentDock documentDock = dockFactory.CreateDocumentDock();
                    documentDock.Proportion = serializedDock.Proportion;
                    documentDock.IsCollapsable = serializedDock.IsCollapsable;
                    return documentDock;
                case SerializedDockableType.Splitter:
                    return dockFactory.CreateProportionalDockSplitter();
                case SerializedDockableType.ToolDock:
                    if (serializedDock.Children.Count == 0)
                        return null;
                    
                    IToolDock toolDock = dockFactory.CreateToolDock();
                    toolDock.Alignment = serializedDock.ToolAlignment;
                    toolDock.Proportion = serializedDock.Proportion;
                    toolDock.VisibleDockables = dockFactory.CreateList<IDockable>();
                    DeserializeChildren(toolDock.VisibleDockables, serializedDock.Children, false);
                    return toolDock;
                case SerializedDockableType.Tool:
                    var tool = layoutViewModelResolver.ResolveTool(serializedDock.UniqueId);
                    return tool;
                case SerializedDockableType.RootDock:
                    IRootDock rootDock = dockFactory.CreateRootDock();
                    rootDock.Proportion = serializedDock.Proportion;
                    rootDock.VisibleDockables = dockFactory.CreateList<IDockable>();
                    DeserializeChildren(rootDock.VisibleDockables, serializedDock.Children, false);
                    if (rootDock.VisibleDockables?.Count > 0)
                    {
                        rootDock.ActiveDockable = rootDock.VisibleDockables[0];
                        rootDock.DefaultDockable = rootDock.VisibleDockables[0];
                    }
                    return rootDock;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DeserializeChildren(IList<IDockable> dockables, List<SerializedDock>? children, bool addSplitters)
        {
            if (children == null)
                return;

            double totalProportion = 0;
            int proportionalDockables = 0;
            bool nextMustBeSplitter = false;
            
            foreach (var dockable in children)
            {
                var deserialized = DeserializeDockable(dockable);

                if (deserialized is IDock d)
                {
                    totalProportion += d.Proportion;
                    proportionalDockables++;
                }

                if (deserialized != null)
                {
                    if (nextMustBeSplitter)
                    {
                        if (deserialized is not IProportionalDockSplitter)
                        {
                            dockables.Add(dockFactory.CreateProportionalDockSplitter());
                        }
                    }

                    dockables.Add(deserialized);

                    nextMustBeSplitter = deserialized is not IProportionalDockSplitter;

                    if (dockable.IsPinned)
                        tempPinnedDockables.Add(deserialized);
                }
            }

            if (proportionalDockables > 0)
            {
                // shrink too big docks
                if (totalProportion > 1)
                {
                    var surplusProportion = totalProportion - 1;
                    foreach (var visible in dockables)
                    {
                        if (visible is IDock d)
                        {
                            d.Proportion -= surplusProportion * d.Proportion / totalProportion;
                        }
                    }
                }
            
                // enlarge too small docks
                var minProportionPerDockable = Math.Min(0.025, 1.0 / proportionalDockables);
                double additionalProportionToRemove = 0;
                double docksBigEnoughSize = 0;
                int docksTooSmall = 0;
                int docksBigEnough = 0;
                foreach (var visible in dockables)
                {
                    if (visible is IDock d)
                    {
                        if (d.Proportion < minProportionPerDockable)
                        {
                            docksTooSmall++;
                            additionalProportionToRemove = minProportionPerDockable - d.Proportion;
                        }
                        else
                        {
                            docksBigEnoughSize += d.Proportion;
                            docksBigEnough++;
                        }
                    }
                }

                foreach (var visible in dockables)
                {
                    if (visible is IDock d)
                    {
                        if (d.Proportion < minProportionPerDockable)
                            d.Proportion = minProportionPerDockable;
                        else
                            d.Proportion -= additionalProportionToRemove * (d.Proportion / docksBigEnoughSize);
                    }
                }
            }
        }

        private HashSet<IDockable> tempPinnedDockables = new();

        private SerializedDock? SerializeDockable(IDockable dockable)
        {
            SerializedDock serialized = new();
            if (tempPinnedDockables.Contains(dockable))
                serialized.IsPinned = true;

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
                serialized.ToolAlignment = toolDock.Alignment;
            }
            else if (dockable is IProportionalDockSplitter splitterDockable)
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
                void ProcessPinnedDockables(IList<IDockable>? dockables)
                {
                    if (dockables != null)
                    {
                        foreach (var p in dockables.ToList())
                        {
                            dockFactory.PinDockable(p); // unpin to serialize
                            tempPinnedDockables.Add(p);
                        }
                    }
                }
                ProcessPinnedDockables(root.LeftPinnedDockables);
                ProcessPinnedDockables(root.RightPinnedDockables);
                ProcessPinnedDockables(root.TopPinnedDockables);
                ProcessPinnedDockables(root.BottomPinnedDockables);

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
                if (dock.VisibleDockables != null)
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
