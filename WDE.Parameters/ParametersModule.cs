using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using WDE.Common;
using WDE.Common.Parameters;
using WDE.Parameters.ViewModels;
using WDE.Parameters.Views;

namespace WDE.Parameters
{
    public class ParametersModule : IModule, IConfigurable
    {
        public static ParameterFactory FactoryInstance { get; private set; }
        private readonly IUnityContainer _container;

        public ParametersModule(IUnityContainer container)
        {
            _container = container;
        }

        public void Initialize()
        {
            FactoryInstance = new ParameterFactory();
            _container.RegisterInstance<IParameterFactory>(FactoryInstance, new ContainerControlledLifetimeManager());

            new ParameterLoader(_container).Load(FactoryInstance);

            _container.RegisterType<IConfigurable, ParametersModule>("Parameters configuration");
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
    }
}