using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WDE.Common.Parameters;
using WDE.Parameters;

namespace WDE.Common.Avalonia.Controls;

public class ParameterBox : BaseParameterBox
{
    private Panel? panel;
    private CheckBox? boolBox;
    private GenericParameterBox? genericBox;
    private CompletionComboParameterBox? comboBox;
    private FlagsParameterBox? flagsBox;
    private bool inChange;

    public static readonly StyledProperty<long> NumberToMultiplyProperty = AvaloniaProperty.Register<ParameterBox, long>(nameof(NumberToMultiply));
    public static readonly StyledProperty<bool> ParameterValueAsBoolProperty = AvaloniaProperty.Register<ParameterBox, bool>(nameof(ParameterValueAsBool), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<Template> BoolTemplateProperty = AvaloniaProperty.Register<ParameterBox, Template>(nameof(BoolTemplate));
    public static readonly StyledProperty<Template> GenericTemplateProperty = AvaloniaProperty.Register<ParameterBox, Template>(nameof(GenericTemplate));
    public static readonly StyledProperty<Template> ComboBoxTemplateProperty = AvaloniaProperty.Register<ParameterBox, Template>(nameof(ComboBoxTemplate));
    public static readonly StyledProperty<Template> FlagsTemplateProperty = AvaloniaProperty.Register<ParameterBox, Template>(nameof(FlagsTemplate));

    public long NumberToMultiply
    {
        get => (long)GetValue(NumberToMultiplyProperty);
        set => SetValue(NumberToMultiplyProperty, value);
    }

    public bool ParameterValueAsBool
    {
        get => (bool)GetValue(ParameterValueAsBoolProperty);
        set => SetValue(ParameterValueAsBoolProperty, value);
    }

    public Template BoolTemplate
    {
        get => GetValue(BoolTemplateProperty);
        set => SetValue(BoolTemplateProperty, value);
    }

    public Template GenericTemplate
    {
        get => GetValue(GenericTemplateProperty);
        set => SetValue(GenericTemplateProperty, value);
    }

    public Template ComboBoxTemplate
    {
        get => GetValue(ComboBoxTemplateProperty);
        set => SetValue(ComboBoxTemplateProperty, value);
    }

    public Template FlagsTemplate
    {
        get => GetValue(FlagsTemplateProperty);
        set => SetValue(FlagsTemplateProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ParameterProperty)
        {
            UpdateVisibility();
        }
        else if (change.Property == ParameterValueProperty)
        {
            if (inChange)
                return;

            inChange = true;
            if (ParameterValue is long l)
                SetCurrentValue(ParameterValueAsBoolProperty, l != 0);
            inChange = false;
        }
        else if (change.Property == ParameterValueAsBoolProperty)
        {
            if (inChange)
                return;

            inChange = true;
            SetCurrentValue(ParameterValueProperty, ParameterValueAsBool ? 1L : 0L);
            inChange = false;
        }
    }

    private void UpdateVisibility()
    {
        if (panel == null)
            return;

        if (this.GetVisualParent() == null)
            return;

        DisableAll();
        if (Parameter is BoolParameter)
            EnableCheckBox();
        else if (Parameter is FlagParameter)
            EnableFlagsBox();
        else if (Parameter is IParameter<long> l && l.Items != null && l.Items.Count < 2000 && !l.NeverUseComboBoxPicker && Parameter is not ICustomPickerParameter<long> && Parameter is not ICustomPickerContextualParameter<long>)
            EnableComboBox();
        else
            EnableGenericBox();
    }

    private void DisableAll()
    {
        if (boolBox != null)
            boolBox.IsVisible = false;

        if (genericBox != null)
            genericBox.IsVisible = false;

        if (comboBox != null)
            comboBox.IsVisible = false;

        if (flagsBox != null)
            flagsBox.IsVisible = false;
    }

    private void EnableCheckBox()
    {
        if (boolBox == null)
        {
            boolBox = (CheckBox?)BoolTemplate?.Build();
            if (boolBox != null)
                panel!.Children.Add(boolBox);
        }
        else
            boolBox.IsVisible = true;
    }

    private void EnableGenericBox()
    {
        if (genericBox == null)
        {
            genericBox = (GenericParameterBox?)GenericTemplate?.Build();
            if (genericBox != null)
                panel!.Children.Add(genericBox);
        }
        else
            genericBox.IsVisible = true;
    }

    private void EnableComboBox()
    {
        if (comboBox == null)
        {
            comboBox = (CompletionComboParameterBox?)ComboBoxTemplate?.Build();
            if (comboBox != null)
                panel!.Children.Add(comboBox);
        }
        else
            comboBox.IsVisible = true;
    }

    private void EnableFlagsBox()
    {
        if (flagsBox == null)
        {
            flagsBox = (FlagsParameterBox)FlagsTemplate.Build()!;
            panel!.Children.Add(flagsBox);
        }
        else
            flagsBox.IsVisible = true;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        panel = e.NameScope.Get<Panel>("PART_Panel");
        UpdateVisibility();
    }
}