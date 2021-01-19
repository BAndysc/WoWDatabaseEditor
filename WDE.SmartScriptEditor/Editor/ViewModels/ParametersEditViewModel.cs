using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class ParametersEditViewModel : BindableBase, IDisposable, IDialog
    {
        private readonly SmartBaseElement element;
        private readonly CollectionViewSource items;
        private string readable;

        public List<object> Parameters { get; } = new();

        public ParametersEditViewModel(IItemFromListProvider itemFromListProvider,
            SmartBaseElement element,
            IEnumerable<(ParameterValueHolder<int> parameter, string name)> parameters,
            IEnumerable<(ParameterValueHolder<float> parameter, string name)> floatParameters = null,
            IEnumerable<(ParameterValueHolder<string> parameter, string name)> stringParameters = null)
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

            Accept = new DelegateCommand(() => CloseOk?.Invoke());
            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
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

        public DelegateCommand Accept { get; }
        public DelegateCommand Cancel { get; }
        public int DesiredWidth => 455;
        public int DesiredHeight => 485;
        public string Title => "Edit";
        public bool Resizeable => true;
        public event Action CloseCancel;
        public event Action CloseOk;
    }
}