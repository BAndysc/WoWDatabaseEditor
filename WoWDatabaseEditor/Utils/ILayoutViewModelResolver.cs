using WDE.Common.Windows;

namespace WoWDatabaseEditor.Utils
{
    public interface ILayoutViewModelResolver
    {
        ITool? ResolveViewModel(string id);
        void LoadDefault();
    }
}