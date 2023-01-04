using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using Prism.Commands;
using WDE.Common.CoreVersion;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.MVVM.Observable;
using WDE.MVVM;
using WDE.Common.Providers;
using WDE.Parameters.Models;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Editor.ViewModels.Editing
{
    public class ParametersEditViewModel : ObservableBase, IDialog
    {
        private readonly IParameterPickerService parameterPickerService;

        public ParametersEditViewModel(IItemFromListProvider itemFromListProvider,
            ICurrentCoreVersion currentCoreVersion,
            IParameterPickerService parameterPickerService,
            EventAiBaseElement element,
            bool focusFirst,
            IEnumerable<(ParameterValueHolder<long> parameter, string name)>? parameters,
            IEnumerable<(ParameterValueHolder<float> parameter, string name)>? floatParameters = null,
            IEnumerable<(ParameterValueHolder<string> parameter, string name)>? stringParameters = null,
            IEnumerable<EditableActionData>? actionParameters = null,
            System.Action? saveAction = null,
            string? focusFirstGroup = null,
            object? context = null)
        {
            this.parameterPickerService = parameterPickerService;
            HashSet<IEditableParameterViewModel> visible = new();
            SourceList<IEditableParameterViewModel> visibleParameters = new();
            List<IEditableParameterViewModel> allParameters = new();
            Link(element, e => e.Readable, () => Readable);
            
            if (actionParameters != null)
                foreach (EditableActionData act in actionParameters)
                    allParameters.Add(new EditableParameterActionViewModel(act));

            bool first = focusFirst;
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    var canFocusThis = first && (focusFirstGroup == null || focusFirstGroup == parameter.name);
                    var focusThis = canFocusThis && parameter.parameter.IsUsed;
                    allParameters.Add(AutoDispose(new EditableParameterViewModel<long>(parameter.parameter, parameter.name, itemFromListProvider, currentCoreVersion, parameterPickerService, context){FocusFirst = focusThis}));
                    if (focusThis)
                        first = false;
                }
            }

            if (floatParameters != null)
                foreach (var parameter in floatParameters)
                {
                    allParameters.Add(AutoDispose(new EditableParameterViewModel<float>(parameter.parameter, parameter.name, itemFromListProvider, currentCoreVersion, parameterPickerService){FocusFirst = first}));
                    first = false;
                }

            if (stringParameters != null)
                foreach (var parameter in stringParameters)
                    allParameters.Add(AutoDispose(new EditableParameterViewModel<string>(parameter.parameter, parameter.name, itemFromListProvider, currentCoreVersion, parameterPickerService){FocusFirst=focusFirst}));

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
        public string Readable { get; private set; } = "";
        public bool ShowCloseButtons { get; set; } = true;

        public ICommand Accept { get; }
        public ICommand Cancel { get; }
        public int DesiredWidth => 545;
        public int DesiredHeight => 625;
        public string Title => "Edit";
        public bool Resizeable => true;

        public event Action? CloseCancel;
        public event Action? CloseOk;
        public event Action? BeforeAccept;
    }
    
    public class Grouping<TKey, TVal> : ObservableCollectionExtended<TVal>, IGrouping<TKey, TVal>, IDisposable where TVal : notnull
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