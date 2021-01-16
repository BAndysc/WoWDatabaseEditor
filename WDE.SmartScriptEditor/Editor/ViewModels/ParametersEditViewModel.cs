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

        public List<object> Parameters { get; } = new();

        public ParametersEditViewModel(IItemFromListProvider itemFromListProvider,
            SmartBaseElement element,
            IEnumerable<(Parameter parameter, string name)> parameters,
            IEnumerable<(FloatParameter parameter, string name)> floatParameters = null,
            IEnumerable<(StringParameter parameter, string name)> stringParameters = null)
        {
            this.element = element;
            Readable = element.Readable;
            this.element.OnChanged += ElementOnOnChanged;

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    Parameters.Add(new ParameterEditorViewModel<int>(parameter.parameter, parameter.name, itemFromListProvider));
                }
            }

            if (floatParameters != null)
            {
                foreach (var parameter in floatParameters)
                {
                    Parameters.Add(new ParameterEditorViewModel<float>(parameter.parameter, parameter.name, itemFromListProvider));
                }   
            }

            if (stringParameters != null)
            {
                foreach (var parameter in stringParameters)
                {
                    Parameters.Add(new ParameterEditorViewModel<string>(parameter.parameter, parameter.name, itemFromListProvider));
                }
            }

            items = new CollectionViewSource {Source = Parameters};
            items.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
        }

        public string Readable
        {
            get => readable;
            set => SetProperty(ref readable, value);
        }

        public ICollectionView AllItems => items.View;

        public void Dispose()
        {
            element.OnChanged -= ElementOnOnChanged;
        }

        private void ElementOnOnChanged(object? sender, EventArgs e)
        {
            Readable = element.Readable;
        }
    }
}