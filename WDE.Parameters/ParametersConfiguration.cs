using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WDE.Common;
using WDE.Module.Attributes;
using WDE.Parameters.ViewModels;
using WDE.Parameters.Views;

namespace WDE.Parameters
{
    [AutoRegister, SingleInstance]
    public class ParametersConfiguration : IConfigurable
    {
        private readonly ParameterFactory factory;

        public ParametersConfiguration(ParameterFactory factory)
        {
            this.factory = factory;
        }

        public KeyValuePair<ContentControl, Action> GetConfigurationView()
        {
            var view = new ParametersView();
            var viewModel = new ParametersViewModel(factory);
            view.DataContext = viewModel;
            return new KeyValuePair<ContentControl, Action>(view, viewModel.SaveAction);
        }

        public string GetName()
        {
            return "Parameters browser";
        }
    }
}
