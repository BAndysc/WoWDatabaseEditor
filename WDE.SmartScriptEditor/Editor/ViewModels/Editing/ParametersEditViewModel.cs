using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DynamicData;
using DynamicData.Binding;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.MVVM.Observable;
using WDE.MVVM;
using WDE.Common.Providers;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels.Editing
{
    public class ParametersEditViewModel : ObservableBase, IDialog
    {
        public ParametersEditViewModel(IItemFromListProvider itemFromListProvider,
            SmartBaseElement element,
            bool focusFirst,
            IEnumerable<(ParameterValueHolder<long> parameter, string name)> parameters,
            IEnumerable<(ParameterValueHolder<float> parameter, string name)> floatParameters = null,
            IEnumerable<(ParameterValueHolder<string> parameter, string name)> stringParameters = null,
            IEnumerable<EditableActionData> actionParameters = null,
            System.Action saveAction = null)
        {
            HashSet<IEditableParameterViewModel> visible = new();
            SourceList<IEditableParameterViewModel> visibleParameters = new();
            List<IEditableParameterViewModel> allParameters = new();
            Link(element, e => e.Readable, () => Readable);

            FocusFirst = focusFirst;
            
            if (actionParameters != null)
                foreach (EditableActionData act in actionParameters)
                    allParameters.Add(new EditableParameterActionViewModel(act));

            if (parameters != null)
                foreach (var parameter in parameters)
                    allParameters.Add(AutoDispose(new EditableParameterViewModel<long>(parameter.parameter, parameter.name, itemFromListProvider)));

            if (floatParameters != null)
                foreach (var parameter in floatParameters)
                    allParameters.Add(AutoDispose(new EditableParameterViewModel<float>(parameter.parameter, parameter.name, itemFromListProvider)));

            if (stringParameters != null)
                foreach (var parameter in stringParameters)
                    allParameters.Add(AutoDispose(new EditableParameterViewModel<string>(parameter.parameter, parameter.name, itemFromListProvider)));

            foreach (IEditableParameterViewModel parameter in allParameters)
            {
                AutoDispose(parameter.Subscribe(p => p.IsHidden,
                    isHidden =>
                    {
                        if (isHidden)
                        {
                            if (visible.Remove(parameter))
                                visibleParameters.Remove(parameter);
                        }
                        else
                        {
                            if (visible.Add(parameter))
                                visibleParameters.Add(parameter);
                        }
                    }));
            }

            Accept = new DelegateCommand(() =>
            {
                BeforeAccept?.Invoke();
                saveAction?.Invoke();
                CloseOk?.Invoke();
            });
            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());

            ReadOnlyObservableCollection<Grouping<string, IEditableParameterViewModel>> l;
            visibleParameters
                .Connect()
                .GroupOn(t => t.Group)
                .Transform(group => new Grouping<string, IEditableParameterViewModel>(group))
                .DisposeMany()
                .Bind(out l)
                .Subscribe();
            FilteredParameters = l;
        }

        public ReadOnlyObservableCollection<Grouping<string, IEditableParameterViewModel>> FilteredParameters { get; }
        public string Readable { get; private set; }
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
    
    public class Grouping<TKey, TVal> : ObservableCollectionExtended<TVal>, IGrouping<TKey, TVal>, IDisposable
    {
        private readonly IDisposable disposable;
        
        public Grouping(IGroup<TVal, TKey> group) 
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            Key = group.GroupKey;
            disposable = group.List
                .Connect()
                .Bind(this)
                .Subscribe();
        }

        public TKey Key { get; private set; }
        public void Dispose() => disposable.Dispose();
    }
}