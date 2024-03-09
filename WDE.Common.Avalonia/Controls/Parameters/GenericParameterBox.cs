using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using WDE.Common.Avalonia.Utils;
using WDE.Common.CoreVersion;
using WDE.Common.Parameters;
using WDE.Common.Utils;

// ReSharper disable once CheckNamespace
namespace WDE.Common.Avalonia.Controls;

public class GenericParameterBox : BaseParameterBox
{
    private ParameterTextBox? textBox;
    private bool canPickItems;
    private Button? pickButton;
    private Button? specialCommandButton;

    public static readonly StyledProperty<string> ParameterValueAsStringProperty = AvaloniaProperty.Register<GenericParameterBox, string>(nameof(ParameterValueAsString));

    public static readonly StyledProperty<long> NumberToMultiplyProperty = AvaloniaProperty.Register<GenericParameterBox, long>(nameof(NumberToMultiply));
    public static readonly DirectProperty<GenericParameterBox, bool> CanPickItemsProperty = AvaloniaProperty.RegisterDirect<GenericParameterBox, bool>(nameof(CanPickItems), o => o.CanPickItems, (o, v) => o.CanPickItems = v);

    private static bool? cachedSupportsSpecialCommand;
    public bool SupportsSpecialCommand { get; }

    public GenericParameterBox()
    {
        parameterFactory = ViewBind.ResolveViewModel<IParameterFactory>();

        if (!cachedSupportsSpecialCommand.HasValue)
            cachedSupportsSpecialCommand = ViewBind.ResolveViewModel<ICurrentCoreVersion>().Current.SupportsSpecialCommands;

        SupportsSpecialCommand = cachedSupportsSpecialCommand.Value;
    }

    public long NumberToMultiply
    {
        get => (long)GetValue(NumberToMultiplyProperty);
        set => SetValue(NumberToMultiplyProperty, value);
    }

    public string ParameterValueAsString
    {
        get => (string)GetValue(ParameterValueAsStringProperty);
        set => SetValue(ParameterValueAsStringProperty, value);
    }

    public bool CanPickItems
    {
        get => canPickItems;
        set => SetAndRaise(CanPickItemsProperty, ref canPickItems, value);
    }

    private bool inChange = false;
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ParameterValueProperty)
        {
            if (inChange)
                return;

            inChange = true;
            SetCurrentValue(ParameterValueAsStringProperty, ParameterValue?.ToString() ?? "");
            inChange = false;
        }
        else if (change.Property == ParameterProperty)
        {
            SetCurrentValue(CanPickItemsProperty, (Parameter?.HasItems ?? false) || (Parameter is ICustomPickerParameter<long> or ICustomPickerParameter<string> or ICustomPickerContextualParameter<long> or ICustomPickerContextualParameter<long>));
        }
        else if (change.Property == ParameterValueAsStringProperty)
        {
            if (inChange)
                return;

            inChange = true;
            if (Parameter is IParameter<long>)
            {
                if (long.TryParse(ParameterValueAsString, out var l))
                    SetCurrentValue(ParameterValueProperty, l);
                else
                    SetCurrentValue(ParameterValueAsStringProperty, ParameterValue?.ToString() ?? "");
            }
            else if (Parameter is IParameter<string>)
            {
                SetCurrentValue(ParameterValueProperty, ParameterValueAsString);
            }
            else if (Parameter is IParameter<float>)
            {
                if (float.TryParse(ParameterValueAsString, out var f))
                    SetCurrentValue(ParameterValueProperty, f);
                else
                    SetCurrentValue(ParameterValueAsStringProperty, ParameterValue?.ToString() ?? "");
            }
            else if (Parameter != null)
            {
                throw new Exception("Unsupported parameter type!");
            }
            inChange = false;
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        textBox = e.NameScope.Get<ParameterTextBox>("PART_TextBox");
        pickButton = e.NameScope.Get<Button>("PART_Pick");
        specialCommandButton = e.NameScope.Get<Button>("PART_SpecialCommandButton");
        pickButton.Click += OnPickButton;
        specialCommandButton.Click += OnSpecialCommandButton;
    }

    private void OnPickButton(object? sender, RoutedEventArgs e)
    {
        var parameterPicker = ViewBind.ResolveViewModel<IParameterPickerService>();
        if (Parameter is IParameter<long> longParameter)
        {
            long? value = 0;
            if (ParameterValue is long l)
                value = l;
            if (value.HasValue || ParameterValue is null)
            {
                async Task PickAndUpdate()
                {
                    var result = await parameterPicker.PickParameter(longParameter, value ?? 0, Context);
                    if (result.ok)
                        SetCurrentValue(ParameterValueProperty, result.value);
                }

                PickAndUpdate().ListenErrors();
            }
            else if (ParameterValue != null)
                throw new Exception("Parameter type mismatch: expected long, got " + ParameterValue.GetType());
        }
        else if (Parameter is IParameter<string> stringParameter)
        {
            string? value = ParameterValue as string;
            if (value != null || ParameterValue == null)
            {
                async Task PickAndUpdate()
                {
                    var result = await parameterPicker.PickParameter(stringParameter, value ?? "", Context);
                    if (result.ok)
                        SetCurrentValue(ParameterValueProperty, result.value!);
                }

                PickAndUpdate().ListenErrors();
            }
            else if (ParameterValue != null)
                throw new Exception("Parameter type mismatch: expected string, got " + ParameterValue.GetType());
        }
        else
            throw new Exception("Parameter " + Parameter?.GetType() + " is neither IParameter<long> nor IParameter<string>!");
    }

    private void OnSpecialCommandButton(object? sender, RoutedEventArgs e)
    {
        async Task Action()
        {
            if (Parameter?.SpecialCommand is not { } command)
                return;

            var result = await command();

            if (result == null)
                return;

            SetCurrentValue(ParameterValueProperty, result);
        }

        Action().ListenErrors();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
    }
}