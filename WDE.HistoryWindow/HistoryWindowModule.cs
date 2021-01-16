using Prism.Events;
using Prism.Services.Dialogs;
using WDE.Common.Managers;
using WDE.Common.Windows;
using WDE.HistoryWindow.ViewModels;
using WDE.HistoryWindow.Views;
using WDE.Module;
using WDE.Module.Attributes;

namespace WDE.HistoryWindow
{
    [AutoRegister]
    [SingleInstance]
    public class HistoryWindowModule : ModuleBase
    {
        public HistoryWindowModule()
        {
        }
    }
}