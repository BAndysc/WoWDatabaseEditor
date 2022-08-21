using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using WDE.Parameters.Models;

namespace WDE.EventAiEditor.Models
{
    public abstract class EventAiBaseElement : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action BulkEditingStarted = delegate { };
        public event Action<string> BulkEditingFinished = delegate { };
        public event EventHandler OnChanged = delegate { };
        public event Action<EventAiBaseElement, uint, uint> OnIdChanged = delegate { };

        public virtual int LineId { get; set; }
        private readonly ParameterValueHolder<long>[] @params;

        private string? readableHint;
        public string? ReadableHint
        {
            get => readableHint;
            set
            {
                readableHint = value;
                CallOnChanged();
            }
        }
        
        private uint id;
        public uint Id
        {
            get => id;
            set
            {
                var old = id;
                id = value;
                OnIdChanged?.Invoke(this, old, id);
                OnPropertyChanged();
            }
        }
        
        public IList<object> Context { get; }
        public List<DescriptionRule>? DescriptionRules { get; set; }
        public abstract string Readable { get; }
        public int ParametersCount { get; }
        
        protected EventAiBaseElement(int parametersCount, uint id, Func<EventAiBaseElement, ParameterValueHolder<long>> paramCreator)
        {
            Id = id;
            ParametersCount = parametersCount;
            @params = new ParameterValueHolder<long>[parametersCount];
            for (int i = 0; i < parametersCount; ++i)
            {
                @params[i] = paramCreator(this);
                @params[i].PropertyChanged += (_, _) => CallOnChanged();
            }

            Context = @params.Select(p => (object)new ParameterWithContext(p, this)).ToList();
            OnChanged += (sender, args) => OnPropertyChanged(nameof(Readable));
        }

        public ParameterValueHolder<long> GetParameter(int index)
        {
            return @params[index];
        }

        protected void CallOnChanged()
        {
            OnChanged(this, null!);
        }

        public IDisposable BulkEdit(string name)
        {
            return new BulkEditing(this, name);
        }

        public virtual void InvalidateReadable()
        {
            OnPropertyChanged(nameof(Readable));
        }

        protected void OnPropertyChanged([CallerMemberName]
            string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class BulkEditing : IDisposable
        {
            private readonly string name;
            private readonly EventAiBaseElement eventAiBaseElement;

            public BulkEditing(EventAiBaseElement eventAiBaseElement, string name)
            {
                this.eventAiBaseElement = eventAiBaseElement;
                this.name = name;
                this.eventAiBaseElement.BulkEditingStarted.Invoke();
            }

            public void Dispose()
            {
                eventAiBaseElement.BulkEditingFinished.Invoke(name);
            }
        }
    }

    public class ParameterWithContext
    {
        public ParameterWithContext(ParameterValueHolder<long> parameter, EventAiBaseElement context)
        {
            Parameter = parameter;
            Context = context;
        }

        public ParameterValueHolder<long> Parameter { get; }
        public EventAiBaseElement Context { get; }
    }
}