using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Mvvm;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.ViewHelpers;
using WDE.Conditions.Data;

namespace WDE.Conditions.ViewModels
{
    public class ConditionsParameterEditorViewModel: BindableBase, IDialog
    {
        // TODO: find better solution  ~Pelekon
        private static readonly Dictionary<string, bool> NotStoredParameters = new()
        {
            { "Parameter", false },
            { "PercentageParameter", false },
            { "SwitchParameter", true },
            { "FlagParameter", true },
        };
        
        public ConditionsParameterEditorViewModel(in ConditionParameterJsonData source, IParameterFactory parameterFactory)
        {
            MakeParamsList(parameterFactory);
            Source = new ConditionParameterEditorData(in source);
            areValuesVisible = false;
            AssignCurrentParamKey();
            AddValue = new DelegateCommand(AddValueToParam);
            DeleteValue = new DelegateCommand(DeleteValueFromParam);
            SaveValue = new DelegateCommand(() => CloseOk?.Invoke());
        }

        public ConditionsParameterEditorViewModel(in ConditionSourceParamsJsonData source, IParameterFactory parameterFactory)
        {
            MakeParamsList(parameterFactory, true);
            Source = new ConditionParameterEditorData(in source);
            areValuesVisible = false;
            AssignCurrentParamKey();
            AddValue = new DelegateCommand(AddValueToParam);
            DeleteValue = new DelegateCommand(DeleteValueFromParam);
            SaveValue = new DelegateCommand(() => CloseOk?.Invoke());
        }
        
        public ConditionParameterEditorData Source { get; }
        private bool areValuesVisible;

        public bool AreValuesVisible
        {
            get => areValuesVisible;
            set => SetProperty(ref areValuesVisible, value);
        }

        private ConditionParameterData? selectedKey;
        public ConditionParameterData? SelectedParameterKey {
            
            get => selectedKey;
            set
            {
                selectedKey = value;
                Source.Type = value!.Key;
                AreValuesVisible = value.UseValues;
                Source.Values.Clear();
            }
        }
        
        public List<ConditionParameterData>? ParameterKeys { get; private set; }
        public BindableTuple<long, SelectOption>? SeletectedParamValue { get; set; }

        public DelegateCommand AddValue { get; }
        public DelegateCommand DeleteValue { get; }
        public DelegateCommand SaveValue { get; }

        private void AssignCurrentParamKey()
        {
            var index = ParameterKeys!.FindIndex(x => x.Key == Source.Type);
            if (index != -1)
            {
                selectedKey = ParameterKeys[index];
                AreValuesVisible = ParameterKeys[index].UseValues;
            }
        }

        private void AddValueToParam() => Source.Values.Add(new BindableTuple<long, SelectOption>(0, new SelectOption()));
        private void DeleteValueFromParam()
        {
            if (SeletectedParamValue != null)
                Source.Values.Remove(SeletectedParamValue);
        }

        public bool IsEmpty() => Source.IsEmpty();

        private void MakeParamsList(IParameterFactory parameterFactory, bool filterValueParams = false)
        {
            // Params loaded from DBC etc + basic parameters defined my module itself e.g.CreatureParameter
            var primitiveParameters = parameterFactory.GetKeys().Where(x => !NotStoredParameters.ContainsKey(x)).ToList();
            ParameterKeys = new List<ConditionParameterData>(NotStoredParameters.Count + primitiveParameters.Count);
            foreach (var param in NotStoredParameters)
                ParameterKeys.Add(new ConditionParameterData(param.Key, param.Value));
            foreach (var param in primitiveParameters)
                ParameterKeys.Add(new ConditionParameterData(param, false));
            if (filterValueParams)
                ParameterKeys = ParameterKeys.Where(x => !x.UseValues).ToList();
            ParameterKeys.Sort();
        }

        public int DesiredWidth { get; } = 473;
        public int DesiredHeight { get; } = 666;
        public string Title { get; } = "Parameter Editor";
        public bool Resizeable { get; } = false;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }
    
    public class ConditionParameterData: IComparable<ConditionParameterData>
    {
        public string Key { get; set; }
        public bool UseValues { get; set; }
        internal ConditionParameterData(string Key, bool UseValues)
        {
            this.Key = Key;
            this.UseValues = UseValues;
        }

        public override string ToString() => Key;

        public int CompareTo(ConditionParameterData? other)
        {
            if (other == null)
                return 1;
            return Key.CompareTo(other.Key);
        }
    }

    public class ConditionParameterEditorData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public ObservableCollection<BindableTuple<long, SelectOption>> Values { get; set; }

        public ConditionParameterEditorData(in ConditionParameterJsonData source)
        {
            Name = source.Name;
            Description = source.Description;
            Type = source.Type;
            Values = new ObservableCollection<BindableTuple<long, SelectOption>>();
            if (source.Values != null)
                foreach (var pair in source.Values)
                    Values.Add(new BindableTuple<long, SelectOption>(pair.Key, pair.Value));
        }

        public ConditionParameterEditorData(in ConditionSourceParamsJsonData source)
        {
            Name = source.Name;
            Description = source.Description;
            Type = source.Type;
            Values = new ObservableCollection<BindableTuple<long, SelectOption>>();
        }

        public ConditionParameterJsonData ToConditionParameterJsonData()
        {
            var obj = new ConditionParameterJsonData();
            obj.Name = Name;
            obj.Description = Description;
            obj.Type = Type;
            if (Values.Count > 0)
            {
                obj.Values = new Dictionary<long, SelectOption>();
                foreach (var pair in Values)
                    obj.Values.TryAdd(pair.Item1, pair.Item2);
            }
            return obj;
        }

        public ConditionSourceParamsJsonData ToConditionSourceParamsJsonData()
        {
            var obj = new ConditionSourceParamsJsonData();
            obj.Name = Name;
            obj.Description = Description;
            obj.Type = Type;
            return obj;
        }

        public bool IsEmpty() => string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Type);
    }
}