using System;
using System.Collections.Generic;
using Avalonia.Data;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.ReactiveUI;
using Dock.Model.ReactiveUI.Controls;

namespace WoWDatabaseEditorCore.Avalonia.Docking
{
    public class DockFactory : Factory
    {
        public override IDocumentDock CreateDocumentDock() => new FocusAwareDocumentDock() {CanFloat = false};
        public override IToolDock CreateToolDock() => new FocusAwareToolDock() {CanFloat = false};

        public void AddDocument(IDock layout, AvaloniaDocumentDockWrapper document)
        {
            var documentsDock = FindDockable(layout, dockable => dockable is IDocumentDock) as IDocumentDock;
            if (documentsDock == null)
            {
                documentsDock = CreateDocumentDock();
                documentsDock.Proportion = 1;
                AddDockable(layout, documentsDock);
            }
            AddDockable(documentsDock, document);
        }
        
        public void AddTool(IDock layout, AvaloniaToolDockWrapper toolWrapper)
        {
            var toolDock = FindDockable(layout, dockable => dockable is ToolDock) as ToolDock;
            if (toolDock == null)
            {
                toolDock = new ToolDock
                {
                    Id = "tool",
                    Title = "Tools",
                    Proportion = 0.2f,
                    CanFloat = false
                };
                AddDockable(layout, CreateProportionalDockSplitter());
                AddDockable(layout, toolDock);
            }
            
            AddDockable(toolDock, toolWrapper);
        }
        
        public override IRootDock CreateLayout()
        {
            var documents = CreateDocumentDock();
            documents.Proportion = 0.8f;
            documents.IsCollapsable = false;
            var mainLayout = new ProportionalDock
            {
                Id = "MainLayout",
                Title = "MainLayout",
                Proportion = 1,
                IsCollapsable = false,
                CanFloat = false,
                Orientation = Orientation.Horizontal,
                ActiveDockable = null,
                VisibleDockables = CreateList<IDockable>
                (
                    documents
                )
            };

            var root = CreateRootDock();
            root.ActiveDockable = mainLayout;
            root.DefaultDockable = mainLayout;
            root.VisibleDockables = CreateList<IDockable>(mainLayout);

            return root;
        }

        public override void InitLayout(IDockable layout)
        {
            this.ContextLocator = new Dictionary<string, Func<object>>
            {
            };

            this.HostWindowLocator = new Dictionary<string, Func<IHostWindow>>
            {
                [nameof(IDockWindow)] = () =>
                {
                    var hostWindow = new HostWindow()
                    {
                        [!HostWindow.TitleProperty] = new Binding("ActiveDockable.Title")
                    };
                    return hostWindow;
                }
            };
            
            base.InitLayout(layout);
        }
    }
}