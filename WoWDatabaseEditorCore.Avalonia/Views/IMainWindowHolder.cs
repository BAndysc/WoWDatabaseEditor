using Avalonia.Controls;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    public interface IMainWindowHolder
    {
        Window Window { get; set; }
    }

    [AutoRegister]
    [SingleInstance]
    public class MainWindowHolderHolder : IMainWindowHolder
    {
        public Window Window { get; set;  }
    }
}