using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Microsoft.Practices.Unity;
using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class ParametersEditViewModel : BindableBase
    {
        private readonly IUnityContainer _container;
        private readonly SmartBaseElement _element;
        private readonly CollectionViewSource _items;
        public ObservableCollection<ParameterEditorViewModel> Parameters { get; private set; } = new ObservableCollection<ParameterEditorViewModel>();

        public string Readable => _element.Readable;
        
        public ICollectionView AllItems => _items.View;

        public ParametersEditViewModel(IUnityContainer _container, SmartBaseElement element, IEnumerable<KeyValuePair<Parameter, string>> parameters)
        {
            this._container = _container;
            _element = element;

            foreach (var parameter in parameters)
            {
                Parameters.Add(new ParameterEditorViewModel(parameter.Key, parameter.Value, _container));
            }

            _items = new CollectionViewSource();
            _items.Source = Parameters;
            _items.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
        }
    }
}
