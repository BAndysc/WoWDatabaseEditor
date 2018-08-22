
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using WDE.Parameters.Models;
using Prism.Ioc;

namespace WDE.Parameters.ViewModels
{
    public class ParametersViewModel : BindableBase
    {
        private ParameterSpecModel _selected;
        private bool _hasSelected = true;

        public ObservableCollection<ParameterSpecModel> Parameters { get; } = new ObservableCollection<ParameterSpecModel>();

        public ParameterSpecModel Selected
        {
            get { return _selected; }
            set { SetProperty(ref _selected, value); }
        }

        public bool HasSelected
        {
            get { return _hasSelected; }
            set { SetProperty(ref _hasSelected, value); }
        }

        public Action SaveAction { get; set; }

        public ParametersViewModel(ParameterFactory factory)
        {
            SaveAction = Save;
            
            foreach (var key in factory.GetKeys())
            {
                Parameters.Add(factory.GetDefinition(key));
            }
            Selected = Parameters[0];
        }

        private void Save()
        {
            throw new NotImplementedException();
        }
    }
}
