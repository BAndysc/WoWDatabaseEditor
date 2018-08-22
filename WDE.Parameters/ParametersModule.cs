using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

using Prism.Modularity;
using WDE.Common;
using WDE.Common.Parameters;
using WDE.Parameters.ViewModels;
using WDE.Parameters.Views;
using Prism.Ioc;
using WDE.Common.Database;

namespace WDE.Parameters
{
    public class ParametersModule : IModule, IConfigurable
    {
        public static ParameterFactory FactoryInstance { get; private set; }

        public ParametersModule()
        {
        }

        public KeyValuePair<ContentControl, Action> GetConfigurationView()
        {
            var view = new ParametersView();
            var viewModel = new ParametersViewModel(FactoryInstance);
            view.DataContext = viewModel;
            return new KeyValuePair<ContentControl, Action>(view, viewModel.SaveAction);
        }

        public string GetName()
        {
            return "Parameters browser";
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            FactoryInstance = new ParameterFactory();
            containerRegistry.RegisterInstance<IParameterFactory>(FactoryInstance);


            containerRegistry.Register<IConfigurable, ParametersModule>("Parameters configuration");
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            new ParameterLoader(containerProvider.Resolve<IDatabaseProvider>()).Load(FactoryInstance);
        }
    }
}