using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.MVVM.Observable;
using WDE.MVVM;
using WDE.Common.Providers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels.Editing
{
    public class ParametersEditViewModel : ObservableBase, IDialog
    {
        private readonly CollectionViewSource items;

        public ParametersEditViewModel(IItemFromListProvider itemFromListProvider,
            SmartBaseElement element,
            bool focusFirst,
            IEnumerable<(ParameterValueHolder<int> parameter, string name)> parameters,
            IEnumerable<(ParameterValueHolder<float> parameter, string name)> floatParameters = null,
            IEnumerable<(ParameterValueHolder<string> parameter, string name)> stringParameters = null,
            IEnumerable<EditableActionData> actionParameters = null,
            System.Action saveAction = null)
        {
            Link(element, e => e.Readable, () => Readable);

            FocusFirst = focusFirst;
            
            if (actionParameters != null)
                foreach (EditableActionData act in actionParameters)
                    Parameters.Add(new EditableParameterActionViewModel(act));

            if (parameters != null)
                foreach (var parameter in parameters)
                    Parameters.Add(AutoDispose(new EditableParameterViewModel<int>(parameter.parameter, parameter.name, itemFromListProvider)));

            if (floatParameters != null)
                foreach (var parameter in floatParameters)
                    Parameters.Add(AutoDispose(new EditableParameterViewModel<float>(parameter.parameter, parameter.name, itemFromListProvider)));

            if (stringParameters != null)
                foreach (var parameter in stringParameters)
                    Parameters.Add(AutoDispose(new EditableParameterViewModel<string>(parameter.parameter, parameter.name, itemFromListProvider)));

            foreach (IEditableParameterViewModel parameter in Parameters)
            {
                AutoDispose(parameter.Subscribe(p => p.IsHidden,
                    isHidden =>
                    {
                        if (isHidden)
                        {
                            if (FilteredParameters.Contains(parameter))
                                FilteredParameters.Remove(parameter);
                        }
                        else
                        {
                            if (!FilteredParameters.Contains(parameter))
                                FilteredParameters.Add(parameter);
                        }
                    }));
            }

            items = new CollectionViewSource {Source = FilteredParameters};
            items.GroupDescriptions.Add(new PropertyGroupDescription("Group"));

            Accept = new DelegateCommand(() =>
            {
                BeforeAccept?.Invoke();
                saveAction?.Invoke();
                CloseOk?.Invoke();
            });
            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
        }

        public List<IEditableParameterViewModel> Parameters { get; } = new();
        public ObservableCollection<IEditableParameterViewModel> FilteredParameters { get; } = new();
        public string Readable { get; private set; }
        public ICollectionView AllItems => items.View;
        public bool ShowCloseButtons { get; set; } = true;

        public DelegateCommand Accept { get; }
        public DelegateCommand Cancel { get; }
        public int DesiredWidth => 455;
        public int DesiredHeight => 485;
        public string Title => "Edit";
        public bool Resizeable => true;
        public bool FocusFirst { get; }

        public event Action CloseCancel;
        public event Action CloseOk;
        public event Action BeforeAccept;
    }
}