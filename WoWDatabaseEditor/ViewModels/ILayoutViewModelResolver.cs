using WDE.Common.Windows;

namespace WoWDatabaseEditorCore.ViewModels
{
    public interface ILayoutViewModelResolver
    {
        ITool? ResolveViewModel(string id);
        void LoadDefault();
    }
}