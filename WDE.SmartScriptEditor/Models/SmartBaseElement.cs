using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Injection;
using WDE.Common.Parameters;
using WDE.Conditions.Shared;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Data;
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

        public abstract SmartScriptBase? Script { get; }

        public abstract SmartType SmartType { get; }

        /// <summary>
        /// a line id, used only within the editor (doesn't match exported EventId). Unique within a script,
        /// even when using begin inline actionlist
        /// </summary>
        public virtual int VirtualLineId { get; set; }
        
        /// <summary>
        /// the destination event id (smart_script.id). Can be null when the action/event is not exportable.
        /// Can also repeat within a single script when using inline timed action lists (because they generate few output scripts)
        /// </summary>
        public virtual int? DestinationEventId { get; set; }

        /// <summary>
        /// currently saved database event id (smart_script.id). Can be null when the action/event is not yet in the database
        /// </summary>
        public virtual int? SavedDatabaseEventId { get; set; }

        /// <summary>
        /// For 'inline timed actionlist' actions, this will keep the destination actionlist id
        /// </summary>
        public virtual int? DestinationTimedActionListId { get; set; }

        /// <summary>
        /// For 'inline timed actionlist' actions, this will keep the saved destination actionlist id
        /// </summary>
        public virtual int? SavedTimedActionListId { get; set; }

        /// <summary>
        /// True for actions which will be exported into a timed actionlists.
        /// </summary>
        public bool IsInInlineActionList { get; set; }
        private readonly ParameterValueHolder<long>[] @params;
        private readonly ParameterValueHolder<float>[]? floatParams;
        private readonly ParameterValueHolder<string>[]? stringParams;

        private string? readableHint = "";
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
        
        public IList<object?> Context { get; }
        public List<DescriptionRule>? DescriptionRules { get; set; }
        private bool readableDirty = true;
        private string readableCache = null!;
        protected abstract string ReadableImpl { get; }
        public string Readable
        {
            get
            {
                if (readableDirty)
                {
                    readableCache = ReadableImpl;
                    readableDirty = false;
                }
                return readableCache;
            }
        }
        
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

            Context = @params.Select((p, index) => (object?)new ParameterWithContext(p, index, this)).ToList();
            OnChanged += (_, _) => InvalidateReadable();
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

        protected virtual void CallOnChanged(object? sender)
        {
            OnChanged(this, null!);
            if (sender is IParameterValueHolder<long> paramHolder)
            {
                if (paramHolder.Parameter is IAffectsOtherParametersParameter affectsOther)
                {
                    foreach (var index in affectsOther.AffectedParameters())
                        if (index < @params.Length)
                            @params[index].RefreshStringText();
                }

                foreach (var p in @params)
                {
                    if (p.Parameter is IAffectedByOtherParametersParameter affectedByOther)
                        foreach (var index in affectedByOther.AffectedByParameters())
                            if (@params[index] == paramHolder)
                                p.RefreshStringText();
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
            readableDirty = true;
            OnPropertyChanged(nameof(Readable));
        }

        public virtual void InvalidateAllParameters()
        {
            foreach (var p in @params)
                p.RefreshStringText();
            if (floatParams != null)
            {
                foreach (var p in floatParams)
                    p.RefreshStringText();
            }
            if (stringParams != null)
            {
                foreach (var p in stringParams)
                    p.RefreshStringText();
            }
            InvalidateReadable();
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
        public ParameterWithContext(ParameterValueHolder<long> parameter, int? parameterIndex, SmartBaseElement context)
        {
            Parameter = parameter;
            ParameterIndex = parameterIndex;
            Context = context;
        }

        public ParameterValueHolder<long> Parameter { get; }
        public int? ParameterIndex { get; }
        public SmartBaseElement Context { get; }
    }
}