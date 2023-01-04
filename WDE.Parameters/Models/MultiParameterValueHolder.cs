using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PropertyChanged.SourceGenerator;
using WDE.Common.Annotations;
using WDE.Common.Parameters;

namespace WDE.Parameters.Models;

public partial class MultiParameterValueHolder<T> : IParameterValueHolder<T>, System.IDisposable where T : notnull
{
    private bool wasTouched;
    private string cachedStringValue = null!;
    private bool hasCachedStringValue;
    private readonly IParameter<T> defaultParameter;
    private readonly T defaultValue;
    private readonly IReadOnlyList<IParameterValueHolder<T>> values;
    private readonly IReadOnlyList<IParameterValueHolder<T>> originals;
    private readonly IBulkEditSource? bulkEditSource;
    private readonly object? context;

    public MultiParameterValueHolder(IParameter<T> defaultParameter, 
        T defaultValue, 
        IReadOnlyList<IParameterValueHolder<T>> values,
        IReadOnlyList<IParameterValueHolder<T>> originals,
        IBulkEditSource? bulkEditSource,
        object? context)
    {
        this.defaultParameter = defaultParameter;
        this.defaultValue = defaultValue;
        this.values = values;
        this.originals = originals;
        this.bulkEditSource = bulkEditSource;
        this.context = context;
        Debug.Assert(values.Count > 0);
        foreach (var v in values)
        {
            v.PropertyChanged += OnSourceChanged;
        }

        parameter = defaultParameter;
        value = defaultValue;
        name = "";
        RefreshSource();
        wasTouched = false;
    }

    public object? Context => context;

    private void OnSourceChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshSource();
    }

    private void RefreshSource()
    {
        if (suspendNotifications)
            return;
        
        bool isSameValue = true;
        bool isSameParameter = true;
        bool isSameName = true;
        var name = values[0].Name;
        var parameter = values[0].Parameter;
        var value = values[0].Value;
        isUsed = values[0].IsUsed;
        forceHidden = values[0].ForceHidden;
        
        for (int i = 1; i < values.Count; ++i)
        {
            if (!EqualityComparer<T>.Default.Equals(value, values[i].Value))
            {
                isSameValue = false;
            }

            if (name != values[i].Name)
            {
                isSameName = false;
            }
            
            if (parameter != values[i].Parameter)
            {
                isSameParameter = false;
            }

            isUsed |= values[i].IsUsed;
            forceHidden &= values[i].ForceHidden;
        }

        if (isSameParameter)
            this.parameter = parameter;
        else
            this.parameter = defaultParameter;

        if (isSameValue)
            this.value = value;
        else
            this.value = defaultValue;

        if (isSameName)
            this.name = name;
        else
            this.name = "(multiple types)";

        HoldsMultipleValues = !isSameValue;
        hasCachedStringValue = false;
        OnPropertyChanged(nameof(Parameter));
        OnPropertyChanged(nameof(GenericParameter));
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(String));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(IsUsed));
        OnPropertyChanged(nameof(ForceHidden));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(Items));
    }

    private IParameter<T> parameter;
    public IParameter<T> Parameter
    {
        get => parameter;
        set
        {
            if (parameter == value)
                return;

            hasCachedStringValue = false;
            parameter = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(String));
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(GenericParameter));
        }
    }
    
    private string name;
    public string Name
    {
        get => name; 
        set
        {
            name = value;
            OnPropertyChanged();
        }
    }

    [Notify] private bool holdsMultipleValues;
    
    public bool IsModified
    {
        get
        {
            for (int i = 0; i < values.Count; ++i)
                if (!EqualityComparer<T>.Default.Equals(this.value, values[i].Value))
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
            hasCachedStringValue = false;

            var dispose = bulkEditSource?.BulkEdit("Edit " + name);
            Apply();
            dispose?.Dispose();
            
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsModified));
            OnPropertyChanged(nameof(String));
        }
    }
    
    [Notify] private bool isUsed;

    [Notify] private bool forceHidden;
    
    public virtual Dictionary<T, SelectOption>? Items => Parameter.Items;
    
    public virtual bool HasItems => Parameter.HasItems;
    
    public string String => ToString();

    public IParameter GenericParameter => Parameter;

    public void ForceRefresh()
    {
        hasCachedStringValue = false;
        OnPropertyChanged(nameof(String));
    }
    
    public override string ToString()
    {
        if (hasCachedStringValue)
            return cachedStringValue;
        // if (parameter is IAsyncParameter<T> asyncParameter)
        // {
        //     if (!AsyncInProgress) 
        //         CalculateStringAsync(value, asyncParameter).ListenErrors();
        //     return parameter.ToString(value);
        // }
        
        hasCachedStringValue = true;

        if (HoldsMultipleValues)
            cachedStringValue = "---";
        else
            cachedStringValue = values[0].String;// parameter.ToString(value);
    
        return cachedStringValue;
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
                values[i].Value = value;
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
                originals[i].Value = value;
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
}