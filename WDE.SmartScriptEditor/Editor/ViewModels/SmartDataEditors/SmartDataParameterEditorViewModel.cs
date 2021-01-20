using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.Commands;
using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Data;
using WDE.Common.ViewHelpers;
using System.Collections.ObjectModel;
using System.Configuration;
using WDE.Common.Managers;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    class SmartDataParameterEditorViewModel: BindableBase, IDialog
    {
        // TEMP until better solution comes to my mind ~Pelekon
        private static readonly Dictionary<string, bool> NotStoredParameters = new Dictionary<string, bool>
        {
            { "Parameter", false },
            { "PercentageParameter", false },
            { "SwitchParameter", true },
            { "FlagParameter", true },
        };
        public SmartDataParameterEditorViewModel(IParameterFactory parameterFactory, in SmartParameterJsonData source)
        {
            // Params loaded from DBC etc + basic parameters defined my module itself e.g.CreatureParameter
            var primitiveParameters = parameterFactory.GetKeys().Where(x => !NotStoredParameters.ContainsKey(x)).ToList();
            ParameterKeys = new List<SmartDataParamterData>(NotStoredParameters.Count + primitiveParameters.Count);
            foreach (var param in NotStoredParameters)
                ParameterKeys.Add(new SmartDataParamterData(param.Key, param.Value));
            foreach (var param in primitiveParameters)
                ParameterKeys.Add(new SmartDataParamterData(param, false));
            ParameterKeys.Sort();
            Source = new SmartDataParameterEditorData(in source);
            areValuesVisible = false;
            AssignCurrentParamKey();
            AddValue = new DelegateCommand(AddValueToParam);
            DeleteValue = new DelegateCommand(DeleteValueFromParam);
            SaveValue = new DelegateCommand(() => CloseOk?.Invoke());
        }

        public SmartDataParameterEditorData Source { get; }
        private bool areValuesVisible;

        public bool AreValuesVisible
        {
            get => areValuesVisible;
            set => SetProperty(ref areValuesVisible, value);
        }

        private SmartDataParamterData selectedKey;
        public SmartDataParamterData SelectedParameterKey { get => selectedKey;
            set
            {
                selectedKey = value;
                Source.Type = value.Key;
                AreValuesVisible = value.UseValues;
                Source.Values.Clear();
            }
        }
        public List<SmartDataParamterData> ParameterKeys { get; }
        public BindableTuple<int, SelectOption>? SeletectedParamValue { get; set; }

        public DelegateCommand AddValue { get; }
        public DelegateCommand DeleteValue { get; }
        public DelegateCommand SaveValue { get; }

        private void AssignCurrentParamKey()
        {
            var index = ParameterKeys.FindIndex(x => x.Key == Source.Type);
            if (index != -1)
            {
                selectedKey = ParameterKeys[index];
                AreValuesVisible = ParameterKeys[index].UseValues;
            }
        }

        private void AddValueToParam() => Source.Values.Add(new BindableTuple<int, SelectOption>(0, new SelectOption()));
        private void DeleteValueFromParam()
        {
            if (SeletectedParamValue != null)
                Source.Values.Remove(SeletectedParamValue);
        }
        
        public int DesiredWidth { get; } = 453;
        public int DesiredHeight { get; } = 666;
        public string Title { get; } = "Parameter Editor";
        public bool Resizeable { get; } = false;
        public event Action CloseCancel;
        public event Action CloseOk;
    }

    public class SmartDataParamterData: IComparable<SmartDataParamterData>
    {
        public string Key { get; set; }
        public bool UseValues { get; set; }
        internal SmartDataParamterData(string Key, bool UseValues)
        {
            this.Key = Key;
            this.UseValues = UseValues;
        }

        public override string ToString() => Key;

        public int CompareTo(SmartDataParamterData other)
        {
            if (other == null)
                return 1;
            return Key.CompareTo(other.Key);
        }
    }

    class SmartDataParameterEditorData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
        public int DefaultVal { get; set; }
        public ObservableCollection<BindableTuple<int, SelectOption>> Values { get; set; }

        public SmartDataParameterEditorData(in SmartParameterJsonData source)
        {
            Name = source.Name;
            Description = source.Description;
            Type = source.Type;
            Required = source.Required;
            DefaultVal = source.DefaultVal;
            Values = new ObservableCollection<BindableTuple<int, SelectOption>>();
            if (source.Values != null)
                foreach (var pair in source.Values)
                    Values.Add(new BindableTuple<int, SelectOption>(pair.Key, pair.Value));
        }

        public SmartParameterJsonData ToSmartParameterJsonData()
        {
            var obj = new SmartParameterJsonData();
            obj.Name = Name;
            obj.Description = Description;
            obj.Type = Type;
            obj.Required = Required;
            obj.DefaultVal = DefaultVal;
            if (Values.Count > 0)
            {
                obj.Values = new Dictionary<int, SelectOption>();
                foreach (var pair in Values)
                    obj.Values.TryAdd(pair.Item1, pair.Item2);
            }
            return obj;
        }

        public bool IsEmpty() => string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Type);
    }
}
