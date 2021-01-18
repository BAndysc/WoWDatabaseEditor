using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Module.Attributes;
using WDE.Parameters.Models;

namespace WDE.Parameters.ViewModels
{
    [AutoRegister]
    public class ParametersViewModel : BindableBase, IConfigurable
    {
        private bool hasSelected = true;
        private ParameterSpecModel selected;

        public ParametersViewModel(ParameterFactory factory)
        {
            foreach (string key in factory.GetKeys())
                Parameters.Add(factory.GetDefinition(key));
            if (Parameters.Count > 0)
                Selected = Parameters[0];
            
            Save = new DelegateCommand(() => { });
        }

        public ObservableCollection<ParameterSpecModel> Parameters { get; } = new();

        public ParameterSpecModel Selected
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }

        public bool HasSelected
        {
            get => hasSelected;
            set => SetProperty(ref hasSelected, value);
        }

        public ICommand Save { get; }

        public string Name => "Parameters browser";
        
        public bool IsModified => false;
    }
}