using Microsoft.Practices.Unity;
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

namespace WDE.HistoryWindow
{
    public class HistoryWindowModule : IModule, IWindowProvider
    {
        private readonly IUnityContainer _container;

        public HistoryWindowModule(IUnityContainer _container)
        {
            this._container = _container;
        }
        
        public void Initialize()
        {
            _container.RegisterType<IWindowProvider, HistoryWindowModule>("HistoryWindow");
        }

        public ContentControl GetView()
        {
            var view = new HistoryView();
            view.DataContext = _container.Resolve<HistoryViewModel>();
            return view;
        }

        public bool AllowMultiple => false;

        public string Name => "History view";
    }
}
