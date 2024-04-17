using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.VisualTree;
using WDE.Common.Avalonia.Utils;
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
        private static Dictionary<IDocument, Control> documents = new();
        private static Dictionary<ITool, Control> tools = new();

        public bool Match(object? data)
        {
            return data is AvaloniaDocumentDockWrapper || data is AvaloniaToolDockWrapper;
        }

        public Control Build(object? data, Control? existing)
        {
            Bind();
            if (data is AvaloniaDocumentDockWrapper documentDockWrapper)
            {
                if (documents.TryGetValue(documentDockWrapper.ViewModel, out var view))
                {
                    var parent = view.GetVisualParent();
                    if (parent?.GetVisualChildren() is AvaloniaList<Visual> children)
                    {
                        Console.WriteLine("Bandy: avalonia 11 fishy thing, to investigate [571XXA]");
                        children.Remove(view);
                    }
                    ((ISetLogicalParent)(view)).SetParent(null);
                    return view;
                }
                
                if (ViewBind.TryResolve(documentDockWrapper.ViewModel, out var documentView) && documentView is Control control)
                {
                    documents[documentDockWrapper.ViewModel] = control;
                    control.Classes.Add("documentView");
                    control.DataContext = documentDockWrapper.ViewModel;
                    return control;
                }
            }
            else if (data is AvaloniaToolDockWrapper toolDockWrapper)
            {
                if (tools.TryGetValue(toolDockWrapper.ViewModel, out var view))
                {
                    var parent = view.GetVisualParent();
                    if (parent?.GetVisualChildren() is AvaloniaList<Visual> children)
                    {
                        Console.WriteLine("Bandy: avalonia 11 fishy thing, to investigate [571XXA]");
                        children.Remove(view);
                    }
                    return view;
                }

                if (ViewBind.TryResolve(toolDockWrapper.ViewModel, out var documentView) && documentView is Control control)
                {
                    tools[toolDockWrapper.ViewModel] = control;
                    tools[toolDockWrapper.ViewModel].DataContext = toolDockWrapper.ViewModel;
                    return control;
                }
            }

            return new Panel();
        }

        public Control Build(object? data)
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
                        if (documents.TryGetValue(item.Item, out var view))
                            view.DataContext = null; // reset datacontext to prevent Avalonia leaks
                        //RemoveContextMenus(documents[item.Item]);
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
        //
        // // This might fix https://github.com/AvaloniaUI/Avalonia/issues/8214 in some cases, but dunno
        // private void RemoveContextMenus(Control item)
        // {
        //     Queue<(Control, int)> queue = new();
        //     queue.Enqueue((item, 0));
        //     while (queue.Count > 0)
        //     {
        //         var (elem, level) = queue.Dequeue();
        //         if (elem is ContentControl cc && cc.Content is Control ic2)
        //         {
        //             queue.Enqueue((ic2, level));
        //         }
        //         else if (elem is ContentPresenter cp && cp.Content is Control ic3)
        //         {
        //             queue.Enqueue((ic3, level));
        //         }
        //         if (elem is Control c)
        //         {
        //             if (c.ContextMenu != null)
        //                 c.ContextMenu = null;
        //             if (c.ContextFlyout != null)
        //                 c.ContextFlyout = null;
        //         }
        //
        //         foreach (var child in elem.GetVisualChildren())
        //         {
        //             if (child is Control ic)
        //             {
        //                 queue.Enqueue((ic, level + 1));
        //             }
        //         }
        //     }
        // }
    }
}