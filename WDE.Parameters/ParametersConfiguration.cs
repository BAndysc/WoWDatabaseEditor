using System;
using System.Collections.Generic;
using System.Windows.Controls;
using WDE.Common;
using WDE.Module.Attributes;
using WDE.Parameters.ViewModels;
using WDE.Parameters.Views;

namespace WDE.Parameters
{
    [AutoRegister]
    [SingleInstance]
    public class ParametersConfiguration : IConfigurable
    {
        private readonly ParameterFactory factory;

        public ParametersConfiguration(ParameterFactory factory)
        {
            this.factory = factory;
        }

        public KeyValuePair<ContentControl, Action> GetConfigurationView()
        {
            ParametersView view = new ParametersView();
            ParametersViewModel viewModel = new ParametersViewModel(factory);
            view.DataContext = viewModel;
            return new KeyValuePair<ContentControl, Action>(view, viewModel.SaveAction);
        }

        public string GetName()
        {
            return "Parameters browser";
        }
    }
}