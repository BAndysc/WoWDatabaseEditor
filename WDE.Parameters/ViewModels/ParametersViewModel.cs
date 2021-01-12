using System;
using System.Collections.ObjectModel;
using Prism.Mvvm;
using WDE.Parameters.Models;

namespace WDE.Parameters.ViewModels
{
    public class ParametersViewModel : BindableBase
    {
        private bool hasSelected = true;
        private ParameterSpecModel selected;

        public ParametersViewModel(ParameterFactory factory)
        {
            SaveAction = Save;

            foreach (string key in factory.GetKeys())
                Parameters.Add(factory.GetDefinition(key));
            if (Parameters.Count > 0)
                Selected = Parameters[0];
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

        public Action SaveAction { get; set; }

        private void Save()
        {
        }
    }
}