using System;

namespace WoWDatabaseEditorCore.ViewModels
{
    public interface ICloseAwareViewModel
    {
        public event Action CloseRequest;
        public event Action ForceCloseRequest;
    }
}