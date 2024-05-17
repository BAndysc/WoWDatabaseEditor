using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Controls;

public abstract class BaseParameterBox : TemplatedControl
{
    protected IParameterFactory parameterFactory;
    private string stringValue = "";
    private IParameter? currentParameter;
    public static readonly StyledProperty<string?> ParameterKeyProperty = AvaloniaProperty.Register<BaseParameterBox, string?>(nameof(ParameterKey));
    public static readonly DirectProperty<BaseParameterBox, bool> IsFloatParameterProperty = AvaloniaProperty.RegisterDirect<BaseParameterBox, bool>(nameof(IsFloatParameter), o => o.IsFloatParameter);
    public static readonly DirectProperty<BaseParameterBox, bool> IsLongParameterProperty = AvaloniaProperty.RegisterDirect<BaseParameterBox, bool>(nameof(IsLongParameter), o => o.IsLongParameter);
    public static readonly DirectProperty<BaseParameterBox, bool> IsStringParameterProperty = AvaloniaProperty.RegisterDirect<BaseParameterBox, bool>(nameof(IsStringParameter), o => o.IsStringParameter);
    public static readonly DirectProperty<BaseParameterBox, string> ParameterStringProperty = AvaloniaProperty.RegisterDirect<BaseParameterBox, string>(nameof(ParameterString), o => o.ParameterString, (o, v) => o.ParameterString = v);
    public static readonly DirectProperty<BaseParameterBox, IParameter?> ParameterProperty = AvaloniaProperty.RegisterDirect<BaseParameterBox, IParameter?>(nameof(Parameter), o => o.Parameter, (o, v) => o.Parameter = v);
    public static readonly StyledProperty<object?> ParameterValueProperty = AvaloniaProperty.Register<BaseParameterBox, object?>(nameof(ParameterValue), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<object?> ContextProperty = AvaloniaProperty.Register<BaseParameterBox, object?>(nameof(Context));

    public string? ParameterKey
    {
        get => (string?)GetValue(ParameterKeyProperty);
        set => SetValue(ParameterKeyProperty, value);
    }

    public string ParameterString
    {
        get => stringValue;
        set => SetAndRaise(ParameterStringProperty, ref stringValue, value);
    }

    public IParameter? Parameter
    {
        get => currentParameter!;
        set => SetAndRaise(ParameterProperty, ref currentParameter, value);
    }

    public object? ParameterValue
    {
        get => (object?)GetValue(ParameterValueProperty);
        set => SetValue(ParameterValueProperty, value);
    }

    public object? Context
    {
        get => (object?)GetValue(ContextProperty);
        set => SetValue(ContextProperty, value);
    }

    public bool IsFloatParameter => currentParameter is IParameter<float>;
    public bool IsLongParameter => currentParameter is IParameter<long>;
    public bool IsStringParameter => currentParameter is IParameter<string>;

    public BaseParameterBox()
    {
        parameterFactory = DI.Resolve<IParameterFactory>();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ParameterKeyProperty)
        {
            var name = ParameterKey;

            if (parameterFactory.IsRegisteredLong(name))
            {
                var parameter = parameterFactory.Factory(name);
                SetCurrentValue(ParameterProperty, parameter);
            }
            else if (parameterFactory.IsRegisteredString(name))
            {
                var parameter = parameterFactory.FactoryString(name);
                SetCurrentValue(ParameterProperty, parameter);
            }
            else if (parameterFactory.IsRegisteredFloat(name))
            {
                var parameter = parameterFactory.FactoryFloat(name);
                SetCurrentValue(ParameterProperty, parameter);
            }
            else
                SetCurrentValue(ParameterProperty, null);
        }
        else if (change.Property == ParameterProperty)
        {
            RaisePropertyChanged(IsFloatParameterProperty, !IsFloatParameter, IsFloatParameter);
            RaisePropertyChanged(IsLongParameterProperty, !IsLongParameter, IsLongParameter);
            RaisePropertyChanged(IsStringParameterProperty, !IsStringParameter, IsStringParameter);
            UpdateString();
        }
        else if (change.Property == ParameterValueProperty)
        {
            UpdateString();
        }
        else if (change.Property == ContextProperty)
        {
            UpdateString();
            UnbindFromContext();
            BindToContext();
        }
    }

    private CancellationTokenSource? refreshStringInProgress;

    private void UpdateString()
    {
        {
            if (currentParameter is IParameter<long> longParameter && ParameterValue is long l)
            {
                SetCurrentValue(ParameterStringProperty, longParameter.ToString(l));
            }
            else if (currentParameter is IParameter<string> stringParameter && ParameterValue is string s)
            {
                SetCurrentValue(ParameterStringProperty, stringParameter.ToString(s));
            }
            else if (currentParameter is IParameter<float> floatParameter && ParameterValue is float f)
            {
                SetCurrentValue(ParameterStringProperty, floatParameter.ToString(f));
            }
            else
            {
                SetCurrentValue(ParameterStringProperty, "");
            }
        }

        {
            if (Context is { } context)
            {
                if (currentParameter is IContextualParameter<long> longParameter && ParameterValue is long l)
                {
                    SetCurrentValue(ParameterStringProperty, longParameter.ToString(l, context));
                }
                else if (currentParameter is IContextualParameter<string> stringParameter && ParameterValue is string s)
                {
                    SetCurrentValue(ParameterStringProperty, stringParameter.ToString(s, context));
                }
                else if (currentParameter is IContextualParameter<float> floatParameter && ParameterValue is float f)
                {
                    SetCurrentValue(ParameterStringProperty, floatParameter.ToString(f, context));
                }
            }
        }

        {
            if (Context is { } context && currentParameter is IAsyncContextualParameter<long> contextualLong && ParameterValue is long l2)
            {
                refreshStringInProgress?.Cancel();
                refreshStringInProgress = new();

                async Task UpdateLong(CancellationToken token)
                {
                    SetCurrentValue(ParameterStringProperty, await contextualLong.ToStringAsync(l2, token, context));
                }

                UpdateLong(refreshStringInProgress.Token).ListenErrors();
            }
            else if (currentParameter is IAsyncParameter<long> longParameter && ParameterValue is long l)
            {
                refreshStringInProgress?.Cancel();
                refreshStringInProgress = new();

                async Task UpdateLong(CancellationToken token)
                {
                    SetCurrentValue(ParameterStringProperty, await longParameter.ToStringAsync(l, token));
                }

                UpdateLong(refreshStringInProgress.Token).ListenErrors();
            }
            else if (Context is { } context2 && currentParameter is IAsyncContextualParameter<string> contextualString && ParameterValue is string s2)
            {
                refreshStringInProgress?.Cancel();
                refreshStringInProgress = new();

                async Task UpdateString(CancellationToken token)
                {
                    SetCurrentValue(ParameterStringProperty, await contextualString.ToStringAsync(s2, token, context2));
                }

                UpdateString(refreshStringInProgress.Token).ListenErrors();
            }
            else if (currentParameter is IAsyncParameter<string> stringParameter && ParameterValue is string s)
            {
                refreshStringInProgress?.Cancel();
                refreshStringInProgress = new();

                async Task UpdateString(CancellationToken token)
                {
                    SetCurrentValue(ParameterStringProperty, await stringParameter.ToStringAsync(s, token));
                }

                UpdateString(refreshStringInProgress.Token).ListenErrors();
            }
            else if (currentParameter is IAsyncParameter<float> floatParameter && ParameterValue is float f)
            {
                refreshStringInProgress?.Cancel();
                refreshStringInProgress = new();

                async Task UpdateFloat(CancellationToken token)
                {
                    SetCurrentValue(ParameterStringProperty, await floatParameter.ToStringAsync(f, token));
                }

                UpdateFloat(refreshStringInProgress.Token).ListenErrors();
            }
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UnbindFromContext();
        BindToContext();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        UnbindFromContext();
    }

    private INotifyPropertyChanged? boundContext;

    private void BindToContext()
    {
        if (Context is INotifyPropertyChanged)
        {
            boundContext = (INotifyPropertyChanged)Context;
            boundContext.PropertyChanged += ContextOnPropertyChanged;
        }
    }

    private void UnbindFromContext()
    {
        if (boundContext != null)
        {
            boundContext.PropertyChanged -= ContextOnPropertyChanged;
            boundContext = null;
        }
    }

    private void ContextOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateString();
    }
}