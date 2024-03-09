using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.CoreVersion;
using WDE.Common.Managers;
using WDE.MVVM;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Utils;
using WDE.Parameters.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels.Editing
{
    public abstract class EditableParameterViewModel : ObservableBase {}
    public class EditableParameterViewModel<T> : EditableParameterViewModel, IEditableParameterViewModel, IDialog where T : notnull
    {
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly IParameterPickerService parameterPickerService;
        private readonly object? context;

        public EditableParameterViewModel(MultiParameterValueHolder<T> parameter, 
            string group, 
            IItemFromListProvider itemFromListProvider,
            ICurrentCoreVersion currentCoreVersion,
            IParameterPickerService parameterPickerService,
            object? context = null)
        {
            this.itemFromListProvider = itemFromListProvider;
            this.currentCoreVersion = currentCoreVersion;
            this.parameterPickerService = parameterPickerService;
            this.context = context;
            Group = group;
            Parameter = parameter;

            Accept = new DelegateCommand(() => CloseOk?.Invoke());
            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
            
            Watch(parameter, p => p.IsUsed, nameof(IsHidden));
            Watch(parameter, p => p.ForceHidden, nameof(IsHidden));
            Watch(parameter, p => p.Parameter, nameof(GenericParameter));
            Watch(parameter, p => p.Name, nameof(Name));
            Watch(parameter, p => p.Value, nameof(Value));
            Watch(parameter, p => p.HoldsMultipleValues, nameof(HoldsMultipleValues));
        }

        public MultiParameterValueHolder<T> Parameter { get; set; }

        public bool HoldsMultipleValues => Parameter.HoldsMultipleValues;

        public T Value
        {
            get => Parameter.Value;
            set => Parameter.Value = value;
        }

        public IParameter? GenericParameter => Parameter.Parameter;

        public object? Context => context;

        public string Group { get; }

        public string Name => Parameter.Name;

        public bool FocusFirst { get; set; }
        
        public bool IsFirstParameter { get; set; }

        private bool IsValueNonEmpty => Parameter is MultiParameterValueHolder<long> p && p.Value != 0 ||
                                        Parameter is MultiParameterValueHolder<float> f && f.Value != 0 ||
                                        Parameter is MultiParameterValueHolder<string> s && !string.IsNullOrEmpty(s.Value);
        
        public bool IsHidden => Parameter.ForceHidden || !(Parameter.IsUsed || IsValueNonEmpty);

        public int DesiredWidth { get; } = 300;
        public int DesiredHeight { get; } = 50;
        public string Title { get; } = "Editing";
        public bool Resizeable { get; } = false;
        public ICommand Accept { get; }
        public ICommand Cancel { get; }
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }

    public class ParameterOption
    {
        public ParameterOption(long value, string name)
        {
            Value = value;
            Name = name;
        }

        private string? searchText;
        public long Value { get; }
        public string Name { get; }

        public override string ToString()
        {
            searchText ??= $"{Name} ({Value})";
            return searchText;
        }
    }
}