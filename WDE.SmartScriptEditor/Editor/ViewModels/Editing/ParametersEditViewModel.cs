using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels.Editing
{
    public class ParametersEditViewModel : ObservableBase, IDialog
    {
        private readonly IParameterPickerService parameterPickerService;

        public List<CommandKeyBinding> KeyBindings { get; } = new List<CommandKeyBinding>();
        
        public long SpecialCopyValue { get; }
        
        public ParametersEditViewModel(IItemFromListProvider itemFromListProvider,
            ICurrentCoreVersion currentCoreVersion,
            IParameterPickerService parameterPickerService,
            IMainThread mainThread,
            long specialCopyValue,
            string title,
            SmartBaseElement? element,
            bool focusFirst,
            SmartEditableGroup editableGroup,
            System.Action? saveAction = null,
            string? focusFirstGroup = null)
        {
            Title = title;
            SpecialCopyValue = specialCopyValue;
            this.parameterPickerService = parameterPickerService;
            HashSet<IEditableParameterViewModel> visible = new();
            SourceList<IEditableParameterViewModel> visibleParameters = new();
            List<IEditableParameterViewModel> allParameters = new();
            if (element != null)
                Link(element, e => e.Readable, () => Readable);
            else
                Readable = "(multiple)";

            foreach (EditableActionData act in editableGroup.Actions)
            {
                if (act.Value != null)
                    allParameters.Add(new NumberedEditableParameterActionViewModel(act));
                else
                    allParameters.Add(new EditableParameterActionViewModel(act));
            }

            bool first = focusFirst;
            foreach (var parameter in editableGroup.Parameters)
            {
                var context = parameter.parameter.Context;
                var canFocusThis = first && (focusFirstGroup == null || focusFirstGroup == parameter.name);
                var focusThis = canFocusThis && parameter.parameter.IsUsed;
                allParameters.Add(AutoDispose(new EditableParameterViewModel<long>(parameter.parameter, parameter.name, itemFromListProvider, currentCoreVersion, parameterPickerService, context){FocusFirst = focusThis, IsFirstParameter = focusThis}));
                if (focusThis)
                    first = false;
            }

            foreach (var parameter in editableGroup.FloatParameters)
            {
                allParameters.Add(AutoDispose(new EditableParameterViewModel<float>(parameter.parameter, parameter.name, itemFromListProvider, currentCoreVersion, parameterPickerService){FocusFirst = first, IsFirstParameter = first}));
                first = false;
            }

            foreach (var parameter in editableGroup.StringParameters)
            {
                allParameters.Add(AutoDispose(new EditableParameterViewModel<string>(parameter.parameter, parameter.name, itemFromListProvider, currentCoreVersion, parameterPickerService){FocusFirst=first, IsFirstParameter = first}));
                first = false;
            }

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

            // so that it doesn't get focused anymore
            mainThread.Delay(() =>
            {
                foreach (var p in allParameters)
                    p.FocusFirst = false;
            }, TimeSpan.FromMilliseconds(1));

            Accept = new DelegateCommand(() =>
            {
                BeforeAccept?.Invoke();
                saveAction?.Invoke();
                CloseOk?.Invoke();
            });
            AcceptOpenNext = new DelegateCommand(() =>
            {
                RequestOpenNextEdit = true;
                Accept.Execute(null);
            });
            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());

            AutoDispose(editableGroup);
            
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
        
        public ICommand AcceptOpenNext { get; }
        public ICommand Accept { get; }
        public ICommand Cancel { get; }
        public int DesiredWidth => 545;
        public int DesiredHeight => 625;
        public string Title { get; }
        public bool Resizeable => true;
        public bool RequestOpenNextEdit { get; private set; }

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