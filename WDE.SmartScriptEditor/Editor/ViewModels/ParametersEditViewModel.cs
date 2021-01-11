using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Prism.Mvvm;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class ParametersEditViewModel : BindableBase, IDisposable
    {
        private readonly SmartBaseElement element;
        private readonly CollectionViewSource items;

        private string readable;

        public ParametersEditViewModel(IItemFromListProvider itemFromListProvider,
            SmartBaseElement element,
            IEnumerable<KeyValuePair<Parameter, string>> parameters)
        {
            this.element = element;
            Readable = element.Readable;
            this.element.OnChanged += ElementOnOnChanged;

            foreach (var parameter in parameters)
                Parameters.Add(new ParameterEditorViewModel(parameter.Key, parameter.Value, itemFromListProvider));

            items = new CollectionViewSource();
            items.Source = Parameters;
            items.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
        }

        public ObservableCollection<ParameterEditorViewModel> Parameters { get; } = new();

        public string Readable
        {
            get => readable;
            set => SetProperty(ref readable, value);
        }

        public ICollectionView AllItems => items.View;

        public void Dispose() { element.OnChanged -= ElementOnOnChanged; }

        private void ElementOnOnChanged(object? sender, EventArgs e) { Readable = element.Readable; }
    }
}