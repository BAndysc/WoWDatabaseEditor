using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using AvaloniaStyles.Converters;
using FuzzySharp;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace AvaloniaStyles.Controls
{
    internal class EnumToIntConverter : IValueConverter
    {
        public static EnumToIntConverter Instance { get; } = new();
        
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var val = System.Convert.ToUInt32(value);
            return $"{value} ({val})";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class Extensions
    {
        public static readonly AttachedProperty<Type> EnumTypeProperty = AvaloniaProperty.RegisterAttached<CompletionComboBox, Type>("EnumType", typeof(Extensions));

        public static Type GetEnumType(CompletionComboBox obj)
        {
            return obj.GetValue(EnumTypeProperty);
        }

        public static void SetEnumType(CompletionComboBox obj, Type value)
        {
            obj.SetValue(EnumTypeProperty, value);
        }

        private static bool IsFlagEnum(Type type)
        {
            if (!type.IsEnum)
                return false;

            return type.GetCustomAttribute<FlagsAttribute>() != null;
        }

        internal sealed class Option : ObservableBase, INotifyPropertyChanged
        {
            private readonly Type type;
            private readonly WeakReference<CompletionComboBox> completionBoxReference;

            public Option(object enumValue, Type type, CompletionComboBox combo, string text)
            {
                Text = text;
                EnumValue = enumValue;
                this.type = type;
                completionBoxReference = new WeakReference<CompletionComboBox>(combo);
                EnumInteger = Convert.ToUInt32(enumValue);

                TextWithNumber = $"{Text} {EnumInteger}";
            }

            public string TextWithNumber { get; }
            
            public string Text { get; }
            
            public uint EnumInteger { get; }
            
            public object EnumValue { get; }

            public bool IsChecked
            {
                get
                {
                    if (completionBoxReference.TryGetTarget(out var completionBox))
                    {
                        var current = Convert.ToUInt32(completionBox.SelectedItem!);
                        return EnumInteger == 0 && current == 0 || EnumInteger > 0 && (current & EnumInteger) == EnumInteger;                        
                    }

                    return false;
                }
                set
                {
                    if (completionBoxReference.TryGetTarget(out var completionBox))
                    {
                        var current = Convert.ToUInt32(completionBox.SelectedItem!);
                        if (value)
                        {
                            if (EnumInteger == 0)
                                completionBox.SelectedItem = Enum.ToObject(type, 0);
                            else
                                completionBox.SelectedItem = Enum.ToObject(type, current | EnumInteger);
                        }
                        else
                            completionBox.SelectedItem = Enum.ToObject(type, current & ~EnumInteger);
                    }
                }
            }

            event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
            {
                add => PropertyChanged += value;
                remove => PropertyChanged -= value;
            }

            public void RaiseIsChecked(CompletionComboBox combo)
            {
                RaisePropertyChanged(nameof(IsChecked));
            }
        }
        
        static Extensions()
        {
            EnumTypeProperty.Changed.AddClassHandler<CompletionComboBox>((combo, args) =>
            {
                if (args.NewValue is not Type type)
                    return;

                var values = Enum.GetValuesAsUnderlyingType(type).Cast<object>().Zip(Enum.GetNames(type), (val, name) => new Option(val, type, combo, name)).ToList();
                combo.GetObservable(CompletionComboBox.SelectedItemProperty).SubscribeAction(_ =>
                {
                    foreach (var val in values)
                        val.RaiseIsChecked(combo);
                });
                combo.AsyncPopulator = async (items, str, _) =>
                {
                    if (string.IsNullOrEmpty(str))
                        return values;
                    return Process.ExtractSorted(str, values.Select(s => s.TextWithNumber), cutoff: 51)
                            .Select(s => values[s.Index]);
                };
                if (IsFlagEnum(type))
                {
                    combo.OnEnterPressed += (sender, pressedArgs) =>
                    {
                        // ReSharper disable once VariableHidesOuterVariable
                        var combo = (sender as CompletionComboBox)!;
                        if (pressedArgs.SelectedItem == null)
                        {
                            if (uint.TryParse(pressedArgs.SearchText, out var num))
                                combo.SelectedItem = Enum.ToObject(type, num);   
                        }
                        pressedArgs.Handled = true;
                    };
                    combo.Classes.Add("NoPadding");
                    combo.ButtonItemTemplate = new FuncDataTemplate(_ => true, (_, _) => new TextBlock() { [!TextBlock.TextProperty] = new Binding(".") }, true);
                    combo.ItemTemplate = new FuncDataTemplate(_ => true, (_, _) =>
                    {
                        var panel = new StackPanel()
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(7,5,7,5)
                        };
                        panel.Children.Add(new TextBlock(){Width = 80, 
                            Padding = new Thickness(0,0,3,0),
                            [!TextBlock.TextProperty] = new Binding("EnumInteger")});
                        panel.Children.Add(new TextBlock(){[!TextBlock.TextProperty] = new Binding("Text")});
                        var checkBox = new CheckBox
                        { 
                            Content = panel,
                            Margin = new Thickness(7,0,0,0),
                            Padding = new Thickness(3,1,3,1),
                            VerticalAlignment = VerticalAlignment.Stretch,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            [~ToggleButton.IsCheckedProperty] = new Binding("IsChecked", BindingMode.TwoWay),
                            [!ToolTip.TipProperty] = new Binding("EnumInteger") {Converter = IntToHexStringConverter.Instance}
                        };

                        return checkBox;
                    }, true);
                }
                else
                {
                    combo.SelectedItemExtractor = o =>
                    {
                        if (o is Option option)
                            return option.EnumValue;
                        return Enum.ToObject(type, 0);
                    };
                    combo.ButtonItemTemplate = new FuncDataTemplate(_ => true, (_, _) => new TextBlock() { [!TextBlock.TextProperty] = new Binding("."){Converter = EnumToIntConverter.Instance} }, true);
                    combo.ItemTemplate = new FuncDataTemplate(_ => true, (_, _) =>
                    {
                        var panel = new StackPanel() { Orientation = Orientation.Horizontal };
                        panel.Children.Add(new TextBlock(){Width = 40, [!TextBlock.TextProperty] = new Binding("EnumInteger")});
                        panel.Children.Add(new TextBlock(){[!TextBlock.TextProperty] = new Binding("Text")});
                        return panel;
                    }, true);
                }
            });
        }
    }
}
