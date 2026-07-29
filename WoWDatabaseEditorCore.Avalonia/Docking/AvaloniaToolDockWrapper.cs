using System;
using Dock.Model.Mvvm.Controls;
using WDE.Common.Managers;
using WDE.Common.Windows;
using ITool = WDE.Common.Windows.ITool;

namespace WoWDatabaseEditorCore.Avalonia.Docking
{
    public class AvaloniaToolDockWrapper : Tool, IDockableFocusable
    {
        private readonly IDocumentManager documentManager;
        public ITool ViewModel { get; }
        public bool IsClosed { get; private set; }
        
        public AvaloniaToolDockWrapper(IDocumentManager documentManager, ITool tool)
        {
            this.documentManager = documentManager;
            Id = tool.UniqueId;
            Title = tool.Title;
            ViewModel = tool;
            CanFloat = true;
            CanPin = true;
            CanClose = true;
        }

        public override bool OnClose()
        {
            if (!ViewModel.CanClose())
                return false;
            
            IsClosed = true;
            ViewModel.Visibility = false;
            return true;
        }

        public override void OnSelected()
        {
            if (ViewModel is IFocusableTool focusable)
                documentManager.ActiveTool = focusable;
            documentManager.SelectedTool = ViewModel;
        }

        public void OnFocus()
        {
            OnSelected();
        }
    }
}