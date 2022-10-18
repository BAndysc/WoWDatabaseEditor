using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Injection;
using WDE.Common.Parameters;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Parameters;

namespace WDE.SmartScriptEditor.Models
{
    public abstract class SmartBaseElement : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action BulkEditingStarted = delegate { };
        public event Action<string> BulkEditingFinished = delegate { };
        public event EventHandler OnChanged = delegate { };
        public event Action<SmartBaseElement, int, int> OnIdChanged = delegate { };

        public virtual int LineId { get; set; }
        private readonly ParameterValueHolder<long>[] @params;
        private readonly ParameterValueHolder<float>[]? floatParams;
        private readonly ParameterValueHolder<string>[]? stringParams;

        private string? readableHint;
        public string? ReadableHint
        {
            get => readableHint;
            set
            {
                readableHint = value;
                CallOnChanged(null);
            }
        }
        
        private int id;
        public int Id
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
        public readonly int ParametersCount;
        public readonly int FloatParametersCount;
        public readonly int StringParametersCount;
        
        protected SmartBaseElement(int id, 
            ParametersCount parametersCount,
            Func<SmartBaseElement, ParameterValueHolder<long>> paramCreator)
        {
            Id = id;
            ParametersCount = parametersCount.IntCount;
            @params = new ParameterValueHolder<long>[parametersCount.IntCount];
            for (int i = 0; i < parametersCount.IntCount; ++i)
            {
                @params[i] = paramCreator(this);
                @params[i].PropertyChanged += (p, _) => CallOnChanged(p);
            }

            if (parametersCount.FloatCount > 0)
            {
                FloatParametersCount = parametersCount.FloatCount;
                floatParams = new ParameterValueHolder<float>[parametersCount.FloatCount];
                for (int i = 0; i < parametersCount.FloatCount; ++i)
                {
                    floatParams[i] = new ParameterValueHolder<float>(FloatParameter.Instance, 0);
                    floatParams[i].PropertyChanged += (p, _) => CallOnChanged(p);
                }
            }
            
            if (parametersCount.StringCount > 0)
            {
                StringParametersCount = parametersCount.StringCount;
                stringParams = new ParameterValueHolder<string>[parametersCount.StringCount];
                for (int i = 0; i < parametersCount.StringCount; ++i)
                {
                    stringParams[i] = new ParameterValueHolder<string>(StringParameter.Instance, "");
                    stringParams[i].PropertyChanged += (sender, _) => CallOnChanged(sender);
                }
            }

            Context = @params.Select(p => (object)new ParameterWithContext(p, this)).ToList();
            OnChanged += (sender, args) => OnPropertyChanged(nameof(Readable));
        }

        public ParameterValueHolder<long> GetParameter(int index)
        {
            return @params[index];
        }
        
        public ParameterValueHolder<float> GetFloatParameter(int index)
        {
            return floatParams![index];
        }
        
        public ParameterValueHolder<string> GetStringParameter(int index)
        {
            return stringParams![index];
        }

        protected void CallOnChanged(object? sender)
        {
            OnChanged(this, null!);
            if (sender is ParameterValueHolder<long> paramHolder)
            {
                if (paramHolder.Parameter is IAffectsOtherParametersParameter affectsOther)
                {
                    foreach (var index in affectsOther.AffectedParameters())
                        @params[index].RefreshStringText();
                }
            }
        }
        
        protected void CopyParameters(SmartBaseElement source)
        {
            for (var i = 0; i < ParametersCount; ++i)
                GetParameter(i).Copy(source.GetParameter(i));

            for (var i = 0; i < FloatParametersCount; ++i)
                GetFloatParameter(i).Copy(source.GetFloatParameter(i));
            
            for (var i = 0; i < StringParametersCount; ++i)
                GetStringParameter(i).Copy(source.GetStringParameter(i));
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

    public class ParameterWithContext
    {
        public ParameterWithContext(ParameterValueHolder<long> parameter, SmartBaseElement context)
        {
            Parameter = parameter;
            Context = context;
        }

        public ParameterValueHolder<long> Parameter { get; }
        public SmartBaseElement Context { get; }
    }
}