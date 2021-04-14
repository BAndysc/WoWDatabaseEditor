using Dock.Model.Controls;
using Dock.Model.ReactiveUI.Controls;
using ITool = WDE.Common.Windows.ITool;

namespace WoWDatabaseEditorCore.Avalonia.Docking
{
    public class AvaloniaToolDockWrapper : Tool
    {
        public ITool ViewModel { get; }
        public bool IsClosed { get; private set; }
        
        public AvaloniaToolDockWrapper(ITool tool)
        {
            Id = tool.UniqueId;
            Title = tool.Title;
            ViewModel = tool;
            CanFloat = false;
            CanPin = false;
            CanClose = true;
        }

        public override bool OnClose()
        {
            IsClosed = true;
            ViewModel.Visibility = false;
            return true;
        }
    }
}