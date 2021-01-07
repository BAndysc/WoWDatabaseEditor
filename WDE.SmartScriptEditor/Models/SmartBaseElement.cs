using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Parameters;

namespace WDE.SmartScriptEditor.Models
{
    public abstract class SmartBaseElement : INotifyPropertyChanged
    {
        public event Action BulkEditingStarted = delegate {  };
        public event Action<string> BulkEditingFinished = delegate {  };
        public event EventHandler OnChanged = delegate { };
        public List<DescriptionRule> DescriptionRules { get; set; }
        private readonly Parameter[] _params;
        public string ReadableHint;
        public int Id { get; }

        protected SmartBaseElement(int parametersCount, int id)
        {
            Id = id;
            _params = new Parameter[parametersCount];
            for (int i = 0; i < parametersCount; ++i)
                SetParameterObject(i, new NullParameter());

            OnChanged += (sender, args) => OnPropertyChanged("Readable");
        }

        public void SetParameterObject(int index, Parameter parameter)
        {
            if (_params[index] != null)
                _params[index].OnValueChanged -= SmartBaseElement_OnValueChanged;
            _params[index] = parameter;
            parameter.OnValueChanged += SmartBaseElement_OnValueChanged;
        }

        public void SetParameter(int index, int value)
        {
            _params[index].SetValue(value);
        }

        public Parameter GetParameter(int index)
        {
            return _params[index];
        }

        private void SmartBaseElement_OnValueChanged(object sender, ParameterChangedValue<int> e)
        {
            CallOnChanged();
        }

        protected void CallOnChanged(SmartBaseElement smartEvent = null, ParameterChangedValue<int> e = null)
        {
            OnChanged(this, null);
        }

        public System.IDisposable BulkEdit(string name)
        {
            return new BulkEditing(this, name);
        }

        private class BulkEditing : System.IDisposable
        {
            private readonly SmartBaseElement _smartBaseElement;
            private readonly string _name;

            public BulkEditing(SmartBaseElement smartBaseElement, string name)
            {
                _smartBaseElement = smartBaseElement;
                _name = name;
                _smartBaseElement.BulkEditingStarted.Invoke();
            }

            public void Dispose()
            {
                _smartBaseElement.BulkEditingFinished.Invoke(_name);
            }
        }
        
        public abstract string Readable { get; }
        public abstract int ParametersCount { get;}
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
