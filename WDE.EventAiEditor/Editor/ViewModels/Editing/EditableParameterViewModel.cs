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

namespace WDE.EventAiEditor.Editor.ViewModels.Editing
{
    public abstract class EditableParameterViewModel : ObservableBase {}
    public class EditableParameterViewModel<T> : EditableParameterViewModel, IEditableParameterViewModel, IDialog where T : notnull
    {
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly IParameterPickerService parameterPickerService;
        private readonly object? context;

        public EditableParameterViewModel(ParameterValueHolder<T> parameter, 
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
            SelectItemAction = new AsyncAutoCommand(SelectItem);
            SpecialCopying = typeof(T) == typeof(float);

            Accept = new DelegateCommand(() => CloseOk?.Invoke());
            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
            
            Watch(parameter, p => p.IsUsed, nameof(IsHidden));
            Watch(parameter, p => p.ForceHidden, nameof(IsHidden));
            Watch(parameter, p => p.Parameter, nameof(HasItems));
            Watch(parameter, p => p.Parameter, nameof(Items));
            Watch(parameter, p => p.Parameter, nameof(OptionValue));
            Watch(parameter, p => p.Parameter, nameof(UseModernPicker));
            Watch(parameter, p => p.Parameter, nameof(SpecialCommand));
            Watch(parameter, p => p.Name, nameof(Name));
            Watch(parameter, p => p.Value, nameof(OptionValue));
        }

        public ParameterValueHolder<T> Parameter { get; set; }

        public string Group { get; }
        
        public bool SpecialCopying { get; }
        
        public ICommand SelectItemAction { get; set; }

        public string Name => Parameter.Name;

        public IList<object>? Items
        {
            get
            {
                if (Parameter is not ParameterValueHolder<long> p)
                    return null;
                return HasItems
                    ? p.Items!.Select(pair => (object)new ParameterOption(pair.Key, pair.Value.Name)).ToList()
                    : null;
            }
        }

        public bool FocusFirst { get; set; }

        public Func<Task<object?>>? SpecialCommand => currentCoreVersion.Current.SupportsSpecialCommands ? Parameter.Parameter.SpecialCommand : null;
        
        public bool IsHidden => Parameter.ForceHidden || !Parameter.IsUsed && (Parameter is not ParameterValueHolder<long> p || p.Value == 0);

        public bool UseModernPicker => HasItems && Parameter.Parameter.Items != null && !Parameter.Parameter.NeverUseComboBoxPicker && Parameter is ParameterValueHolder<long> && Parameter.Parameter.Items!.Count < 1000;

        public ParameterOption? OptionValue
        {
            get
            {
                if (Parameter is ParameterValueHolder<long> p)
                {
                    if (p.Items != null && p.Items.TryGetValue(p.Value, out var option))
                        return new ParameterOption(p.Value, option.Name);
                    return new ParameterOption(p.Value, "Unknown");
                }

                return null;
            }
            set
            {
                if (value != null && value.Value is T t)
                    Parameter.Value = t;
            }
        }
        
        public bool HasItems => Parameter.Parameter.HasItems;
        
        public bool BoolIsChecked
        {
            get
            {
                if (Parameter is ParameterValueHolder<long> p)
                    return p.Value == 1;
                return false;
            }
            set
            {
                if (Parameter is ParameterValueHolder<long> p)
                    p.Value = value ? 1 : 0;
                RaisePropertyChanged();
            }
        }
        
        private async Task SelectItem()
        {
            if (Parameter is ParameterValueHolder<long> p)
            {
                (long? val, bool ok) = await parameterPickerService.PickParameter(p.Parameter, p.Value, context);
                if (ok)
                {
                    p.Value = val.Value;
                    if (p.Parameter is ICustomPickerContextualParameter<long>)
                        p.ForceRefresh();
                }
            }
            else if (Parameter is ParameterValueHolder<string> s)
            {
                (string? val, bool ok) = await parameterPickerService.PickParameter(s.Parameter, s.Value, context);
                if (ok)
                {
                    s.Value = val ?? "";
                    if (s.Parameter is ICustomPickerContextualParameter<string>)
                        s.ForceRefresh();
                }
            }
        }

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

        public long Value { get; }
        public string Name { get; }

        public override string ToString()
        {
            return $"{Name} ({Value})";
        }
    }
}