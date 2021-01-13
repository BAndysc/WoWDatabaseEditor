using Prism.Events;
using WDE.Common.Managers;
using WDE.Common.Windows;
using WDE.HistoryWindow.ViewModels;
using WDE.Module;
using WDE.Module.Attributes;

namespace WDE.HistoryWindow
{
    [AutoRegister]
    [SingleInstance]
    public class HistoryWindowModule : ModuleBase
    {
        private readonly IEventAggregator eventAggregator;

        public HistoryWindowModule(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }
    }
}