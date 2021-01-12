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
    public class HistoryWindowModule : ModuleBase, IToolProvider
    {
        private readonly IEventAggregator eventAggregator;

        public HistoryWindowModule(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        public bool AllowMultiple => false;

        public string Name => "History view";

        public ITool Provide()
        {
            return new HistoryViewModel(eventAggregator);
        }

        public bool CanOpenOnStart => false;
    }
}