using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Models;
using Prism.Ioc;
using WDE.Common.Providers;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class ParametersEditViewModel : BindableBase, System.IDisposable
    {
        private readonly SmartBaseElement _element;
        private readonly CollectionViewSource _items;
        public ObservableCollection<ParameterEditorViewModel> Parameters { get; private set; } = new ObservableCollection<ParameterEditorViewModel>();

        private string readable;
        public string Readable
        {
            get => readable;
            set => SetProperty(ref readable, value);
        }
        
        public ICollectionView AllItems => _items.View;

        public ParametersEditViewModel(IItemFromListProvider itemFromListProvider, SmartBaseElement element, IEnumerable<KeyValuePair<Parameter, string>> parameters)
        {
            _element = element;
            Readable = element.Readable;
            _element.OnChanged += ElementOnOnChanged;

            foreach (var parameter in parameters)
            {
                Parameters.Add(new ParameterEditorViewModel(parameter.Key, parameter.Value, itemFromListProvider));
            }

            _items = new CollectionViewSource();
            _items.Source = Parameters;
            _items.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
        }

        private void ElementOnOnChanged(object? sender, EventArgs e)
        {
            Readable = _element.Readable;
        }

        public void Dispose()
        {
            _element.OnChanged -= ElementOnOnChanged;
        }
    }
}
