using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WDE.Common.Parameters;

namespace WDE.SmartScriptEditor.Models
{
    public abstract class SmartBaseElement : INotifyPropertyChanged
    {
        private readonly ParameterValueHolder<int>[] @params;
        public string ReadableHint;
        
        public int ParametersCount { get; }

        protected SmartBaseElement(int parametersCount, int id)
        {
            ParametersCount = parametersCount;
            Id = id;
            @params = new ParameterValueHolder<int>[parametersCount];
            for (int i = 0; i < parametersCount; ++i)
            {
                @params[i] = new ParameterValueHolder<int>("empty", Parameter.Instance);
                @params[i].PropertyChanged += (_, _) => CallOnChanged();
            }
            OnChanged += (sender, args) => OnPropertyChanged(nameof(Readable));
        }

        public List<DescriptionRule> DescriptionRules { get; set; }
        public int Id { get; }
        public abstract string Readable { get; }
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action BulkEditingStarted = delegate { };
        public event Action<string> BulkEditingFinished = delegate { };
        public event EventHandler OnChanged = delegate { };
        
        public ParameterValueHolder<int> GetParameter(int index)
        {
            return @params[index];
        }

        protected void CallOnChanged()
        {
            OnChanged(this, null);
        }

        public IDisposable BulkEdit(string name)
        {
            return new BulkEditing(this, name);
        }

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

            public void Dispose()
            {
                smartBaseElement.BulkEditingFinished.Invoke(name);
            }
        }
    }
}