using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Annotations;
using WDE.Common.Parameters;

namespace WDE.Parameters.Models;

public interface IBulkEditSource
{
    IDisposable BulkEdit(string name);
}

public partial class MultiPropertyValueHolder<T, R> : IParameterValueHolder<T>, IDisposable where T : notnull where R : INotifyPropertyChanged
{
    private bool wasTouched;
    private readonly T defaultValue;
    private readonly IReadOnlyList<R> values;
    private readonly IReadOnlyList<R> originals;
    private readonly Func<R, T> getter;
    private readonly Action<R, bool, T> setter;
    private readonly IBulkEditSource? bulkEditSource;

    public MultiPropertyValueHolder(T defaultValue, 
        IReadOnlyList<R> values,
        IReadOnlyList<R> originals,
        Func<R, T> getter,
        Action<R, bool, T> setter,
        IBulkEditSource? bulkEditSource)
    {
        this.defaultValue = defaultValue;
        this.values = values;
        this.originals = originals;
        this.getter = getter;
        this.setter = setter;
        this.bulkEditSource = bulkEditSource;
        Debug.Assert(values.Count > 0);
        foreach (var v in values)
        {
            v.PropertyChanged += OnSourceChanged;
        }

        value = defaultValue;
        RefreshSource();
        wasTouched = false;
    }

    private void OnSourceChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshSource();
    }

    private void RefreshSource()
    {
        if (suspendNotifications)
            return;
        
        bool isSameValue = true;
        var value = getter(values[0]);
        
        for (int i = 1; i < values.Count; ++i)
        {
            if (!EqualityComparer<T>.Default.Equals(value, getter(values[i])))
            {
                isSameValue = false;
            }
        }
        
        if (isSameValue)
            this.value = value;
        else
            this.value = defaultValue;
        OnPropertyChanged(nameof(Value));
        
        HoldsMultipleValues = !isSameValue;
        
        OnPropertyChanged(nameof(String));
    }

    [Notify] private bool holdsMultipleValues;
    
    public bool IsModified
    {
        get
        {
            for (int i = 0; i < values.Count; ++i)
                if (!EqualityComparer<T>.Default.Equals(this.value, getter(values[i])))
                    return true;
            return false;
        }
    }
    
    private T value;

    public T Value
    {
        get => value;
        set
        {
            if (Comparer<T>.Default.Compare(value, Value) == 0)
                return;

            wasTouched = true;
            var old = this.value;
            this.value = value;

            var dispose = bulkEditSource?.BulkEdit("Edit multiple");
            Apply();
            dispose?.Dispose();
            
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsModified));
            OnPropertyChanged(nameof(String));
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName]  string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool suspendNotifications = false;
    
    private void Apply()
    {
        if (wasTouched)
        {
            suspendNotifications = true;
            for (int i = 0; i < values.Count; ++i)
            {
                try
                {
                    setter(values[i], false, value);
                }
                catch (Exception e)
                {
                    LOG.LogError(e);
                }
            }
            suspendNotifications = false;
            RefreshSource();
        }
    }
    
    public void ApplyToOriginals()
    {
        if (wasTouched)
        {
            suspendNotifications = true;
            for (int i = 0; i < originals.Count; ++i)
            {
                try
                {
                    setter(originals[i], true, value);
                }
                catch (Exception e)
                {
                    LOG.LogError(e);
                }
            }
            suspendNotifications = false;
            RefreshSource();
        }
    }

    public void Dispose()
    {
        foreach (var v in values)
        {
            v.PropertyChanged -= OnSourceChanged;
        }
    }

    public bool IsUsed => true;
    public bool ForceHidden => false;
    public string Name => "";
    public string String => holdsMultipleValues ? "-" : Value.ToString() ?? "-";
    public IParameter GenericParameter => Parameter;

    public IParameter<T> Parameter => typeof(T) == typeof(int) ? 
        (IParameter<T>)IntParameter.Instance :
        throw new NotImplementedException();
}