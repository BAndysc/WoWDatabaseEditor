using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.DbScriptsEditor.Models;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;

namespace WDE.DbScriptsEditor.Editor.ViewModels.Editing
{
    // Copy-adapted from WDE.EventAiEditor's ParametersEditViewModel: the SmartScript-style
    // "edit action" dialog. Edits a detached copy of a step live (each row is a proper editor —
    // completion combo for items, flags combo, checkbox, value+picker); saveAction applies the
    // copy back to the original on Accept.
    public class DbScriptParametersEditViewModel : ObservableBase, IDialog
    {
        public DbScriptParametersEditViewModel(
            IParameterPickerService parameterPickerService,
            EditableDbScriptStep step,
            bool focusFirst,
            IEnumerable<(ParameterValueHolder<long> parameter, string name)>? parameters,
            IEnumerable<(ParameterValueHolder<float> parameter, string name)>? floatParameters = null,
            IEnumerable<(ParameterValueHolder<string> parameter, string name)>? stringParameters = null,
            IEnumerable<EditableActionData>? actionParameters = null,
            System.Action? saveAction = null)
        {
            HashSet<IEditableParameterViewModel> visible = new();
            SourceList<IEditableParameterViewModel> visibleParameters = new();
            List<IEditableParameterViewModel> allParameters = new();
            Link(step, s => s.FormattedReadable, () => Readable);

            if (actionParameters != null)
                foreach (EditableActionData act in actionParameters)
                    allParameters.Add(new EditableParameterActionViewModel(act));

            bool first = focusFirst;
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    var focusThis = first && parameter.parameter.IsUsed;
                    allParameters.Add(AutoDispose(new EditableParameterViewModel<long>(parameter.parameter, parameter.name, parameterPickerService, step) { FocusFirst = focusThis }));
                    if (focusThis)
                        first = false;
                }
            }

            if (floatParameters != null)
                foreach (var parameter in floatParameters)
                {
                    allParameters.Add(AutoDispose(new EditableParameterViewModel<float>(parameter.parameter, parameter.name, parameterPickerService) { FocusFirst = first }));
                    first = false;
                }

            if (stringParameters != null)
                foreach (var parameter in stringParameters)
                    allParameters.Add(AutoDispose(new EditableParameterViewModel<string>(parameter.parameter, parameter.name, parameterPickerService) { FocusFirst = focusFirst }));

            // Rows hide/unhide live while editing and DynamicData's GroupOn appends new items
            // regardless of the source position, so the authored build order is captured here and
            // both the group headers and the rows inside each group re-sort by it.
            var groupOrder = new Dictionary<string, int>();
            for (var i = 0; i < allParameters.Count; i++)
            {
                allParameters[i].Order = i;
                groupOrder.TryAdd(allParameters[i].Group, groupOrder.Count);
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

            Accept = new DelegateCommand(() =>
            {
                saveAction?.Invoke();
                CloseOk?.Invoke();
            });
            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());

            ReadOnlyObservableCollection<Grouping<string, IEditableParameterViewModel>> l;
            visibleParameters
                .Connect()
                .GroupOn(t => t.Group)
                .Transform(group => new Grouping<string, IEditableParameterViewModel>(group,
                    Comparer<IEditableParameterViewModel>.Create((x, y) => x.Order.CompareTo(y.Order))))
                .Sort(Comparer<Grouping<string, IEditableParameterViewModel>>.Create(
                    (x, y) => groupOrder[x.Key].CompareTo(groupOrder[y.Key])))
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
        public string Title => "Edit action";
        public bool Resizeable => true;

        public event Action? CloseCancel;
        public event Action? CloseOk;
    }

    public class Grouping<TKey, TVal> : ObservableCollectionExtended<TVal>, IGrouping<TKey, TVal>, IDisposable where TVal : notnull
    {
        private readonly IDisposable disposable;

        public Grouping(IGroup<TVal, TKey> group, IComparer<TVal>? comparer = null)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            Key = group.GroupKey;
            var connection = group.List.Connect();
            if (comparer != null)
                connection = connection.Sort(comparer);
            disposable = connection
                .Bind(this)
                .Subscribe();
        }

        public TKey Key { get; private set; }
        public void Dispose() => disposable.Dispose();
    }
}
