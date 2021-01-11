using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WDE.Common.Parameters;

namespace WDE.SmartScriptEditor.Models
{
    public abstract class SmartBaseElement : INotifyPropertyChanged
    {
        private readonly Parameter[] @params;
        public string ReadableHint;

        protected SmartBaseElement(int parametersCount, int id)
        {
            Id = id;
            @params = new Parameter[parametersCount];
            for (var i = 0; i < parametersCount; ++i)
                SetParameterObject(i, new NullParameter());

            OnChanged += (sender, args) => OnPropertyChanged("Readable");
        }

        public List<DescriptionRule> DescriptionRules { get; set; }
        public int Id { get; }

        public abstract string Readable { get; }
        public abstract int ParametersCount { get; }
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action BulkEditingStarted = delegate { };
        public event Action<string> BulkEditingFinished = delegate { };
        public event EventHandler OnChanged = delegate { };

        public void SetParameterObject(int index, Parameter parameter)
        {
            if (@params[index] != null)
                @params[index].OnValueChanged -= SmartBaseElement_OnValueChanged;
            @params[index] = parameter;
            parameter.OnValueChanged += SmartBaseElement_OnValueChanged;
        }

        public void SetParameter(int index, int value) { @params[index].SetValue(value); }

        public Parameter GetParameter(int index) { return @params[index]; }

        private void SmartBaseElement_OnValueChanged(object sender, ParameterChangedValue<int> e) { CallOnChanged(); }

        protected void CallOnChanged(SmartBaseElement smartEvent = null, ParameterChangedValue<int> e = null)
        {
            OnChanged(this, null);
        }

        public IDisposable BulkEdit(string name) { return new BulkEditing(this, name); }

        protected virtual void OnPropertyChanged([CallerMemberName]
            string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class BulkEditing : IDisposable
        {
            private readonly string name;
            private readonly SmartBaseElement smartBaseElement;

            public BulkEditing(SmartBaseElement smartBaseElement, string name)
            {
                this.smartBaseElement = smartBaseElement;
                this.name = name;
                this.smartBaseElement.BulkEditingStarted.Invoke();
            }

            public void Dispose() { smartBaseElement.BulkEditingFinished.Invoke(name); }
        }
    }
}