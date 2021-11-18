using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.VisualTree;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Disposables;
using WDE.Common.Managers;
using WDE.Common.Windows;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Avalonia.Docking
{
    public class PersistentDockDataTemplate : IRecyclingDataTemplate
    {
        public static IDocumentManager? DocumentManager;
        private bool bound = false;
        private Dictionary<IDocument, IControl> documents = new();
        private Dictionary<ITool, IControl> tools = new();

        public bool Match(object data)
        {
            return data is AvaloniaDocumentDockWrapper || data is AvaloniaToolDockWrapper;
        }

        public IControl Build(object data, IControl? existing)
        {
            Bind();
            if (data is AvaloniaDocumentDockWrapper documentDockWrapper)
            {
                if (documents.TryGetValue(documentDockWrapper.ViewModel, out var view))
                {
                    var parent = view.VisualParent;
                    if (parent?.VisualChildren is AvaloniaList<IVisual> children)
                        children.Remove(view);
                    ((ISetLogicalParent)(view)).SetParent(null);
                    return view;
                }
                
                if (ViewBind.TryResolve(documentDockWrapper.ViewModel, out var documentView) && documentView is IControl control)
                {
                    documents[documentDockWrapper.ViewModel] = control;
                    documents[documentDockWrapper.ViewModel].Classes.Add("documentView");
                    documents[documentDockWrapper.ViewModel].DataContext = documentDockWrapper.ViewModel;
                    return control;
                }
            }
            else if (data is AvaloniaToolDockWrapper toolDockWrapper)
            {
                if (tools.TryGetValue(toolDockWrapper.ViewModel, out var view))
                {
                    var parent = view.VisualParent;
                    if (parent?.VisualChildren is AvaloniaList<IVisual> children)
                        children.Remove(view);
                    return view;
                }

                if (ViewBind.TryResolve(toolDockWrapper.ViewModel, out var documentView) && documentView is IControl control)
                {
                    tools[toolDockWrapper.ViewModel] = control;
                    tools[toolDockWrapper.ViewModel].DataContext = toolDockWrapper.ViewModel;
                    return control;
                }
            }

            return new Panel();
        }

        public IControl Build(object data)
        {
            Bind();
            return Build(data, null);
        }

        private void Bind()
        {
            if (!bound && DocumentManager != null)
            {
                bound = true;
                DocumentManager.OpenedDocuments.ToStream(false).Where(e => e.Type == CollectionEventType.Remove)
                    .SubscribeAction(item =>
                    {
                        documents.Remove(item.Item);
                    });
                foreach (var tool in DocumentManager.AllTools)
                {
                    tool.ToObservable(i => i.Visibility)
                        .SubscribeAction(toolVisibility =>
                        {
                            if (!toolVisibility)
                                tools.Remove(tool);
                        });
                }
            }
        }
    }
}