
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WDE.Common;
using WDE.Common.Windows;
using WDE.HistoryWindow.ViewModels;
using WDE.HistoryWindow.Views;
using Prism.Ioc;
using Prism.Events;
using WDE.Module.Attributes;
using WDE.Module;

namespace WDE.HistoryWindow
{
    [AutoRegister, SingleInstance]
    public class HistoryWindowModule : ModuleBase, IToolProvider
    {
        private IEventAggregator eventAggregator;

        public HistoryWindowModule(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }
        
        public ContentControl GetView()
        {
            var view = new HistoryView();
            view.DataContext = new HistoryViewModel(eventAggregator);
            return view;
        }

        public bool AllowMultiple => false;

        public string Name => "History view";

        public bool CanOpenOnStart => false;
    }
}
