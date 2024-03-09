using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaStyles.Controls;
using AvaloniaStyles.Controls.FastTableView;
using AvaloniaStyles.Converters;
using FuzzySharp;
using WDE.Common.Parameters;
using WDE.MVVM.Observable;

namespace WDE.Common.Avalonia.Controls
{
    public class FlagComboBox : CompletionComboBox
    {
        private AvaloniaList<Option> options = new();
            
        private long selectedValue;
        public static readonly DirectProperty<FlagComboBox, long> SelectedValueProperty = AvaloniaProperty.RegisterDirect<FlagComboBox, long>("SelectedValue", o => o.SelectedValue, (o, v) => o.SelectedValue = v, 0, BindingMode.TwoWay);
        
        private Dictionary<long, SelectOption>? flags;
        
        public static readonly DirectProperty<FlagComboBox, Dictionary<long, SelectOption>?> FlagsProperty = AvaloniaProperty.RegisterDirect<FlagComboBox, Dictionary<long, SelectOption>?>("Flags", o => o.Flags, (o, v) => o.Flags = v);
        protected override Type StyleKeyOverride => typeof(CompletionComboBox);

        public long SelectedValue
        {
            get => selectedValue;
            set => SetAndRaise(SelectedValueProperty, ref selectedValue, value);
        }

        public Dictionary<long, SelectOption>? Flags
        {
            get => flags;
            set => SetAndRaise(FlagsProperty, ref flags, value);
        }

        static FlagComboBox()
        {
            FlagsProperty.Changed.AddClassHandler<FlagComboBox>((box, args) =>
            {
                if (args.NewValue is not Dictionary<long, SelectOption> optionsDict)
                    return;

                box.options.Clear();
                foreach (var pair in optionsDict)
                    box.options.Add(new Option(box, pair.Key, pair.Value.Name, pair.Value.Description));

                box.Items = box.options;
                box.AsyncPopulator = async (items, str, _) =>
                {
                    if (string.IsNullOrEmpty(str))
                        return box.options;
                    if (long.TryParse(str, out var longValue))
                        return box.options.Where(o => (o.OptionValue & longValue) != 0);
                    return Process.ExtractSorted(str, box.options.Select(s => s.TextWithNumber), cutoff: 51)
                        .Select(s => box.options[s.Index]);
                };
                box.SelectedItemExtractor = delegate(object? o)
                {
                    if (o is Option opt)
                    {
                        box.SelectedValue = opt.OptionValue;
                        return box.SelectedItem;
                    }

                    return null;
                };
                box.OnEnterPressed += (sender, pressedArgs) =>
                {
                    // ReSharper disable once VariableHidesOuterVariable
                    var combo = (sender as FlagComboBox)!;
                    if (pressedArgs.SelectedItem == null)
                    {
                        if (long.TryParse(pressedArgs.SearchText, out var num))
                            combo.SelectedValue = num;   
                    }
                    pressedArgs.Handled = true;
                };
                box.Classes.Add("NoPadding");
                box.ButtonItemTemplate = new FuncDataTemplate(_ => true, (_, _) => new TextBlock() { [!TextBlock.TextProperty] = new Binding(".") }, true);
                box.ItemTemplate = new FuncDataTemplate(_ => true, (_, _) =>
                {
                    var panel = new DockPanel() { Margin = new Thickness(3,1,3,1)};
                    var idText = new TextBlock()
                    {
                        Width = 80, Padding = new Thickness(0, 0, 3, 0),
                        [!TextBlock.TextProperty] = new Binding("OptionValue")
                    };
                    var textText = new TextBlock() { [!TextBlock.TextProperty] = new Binding("Text"), VerticalAlignment = VerticalAlignment.Center};
                    DockPanel.SetDock(idText, global::Avalonia.Controls.Dock.Left);
                    DockPanel.SetDock(textText, global::Avalonia.Controls.Dock.Top);
                    panel.Children.Add(idText);
                    panel.Children.Add(textText);
                    panel.Children.Add(new TextBlock() { [!TextBlock.TextProperty] = new Binding("Description"), TextWrapping = TextWrapping.WrapWithOverflow, Opacity = 0.7, [!IsVisibleProperty] = new Binding("HasDescription") });
                    var checkBox = new CheckBox
                    { 
                        Content = panel,
                        Margin = new Thickness(7,0,0,0),
                        Padding = new Thickness(7,5,7,5),
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        [~ToggleButton.IsCheckedProperty] = new Binding("IsChecked", BindingMode.TwoWay),
                        [!ToolTip.TipProperty] = new Binding("OptionValue") {Converter = IntToHexStringConverter.Instance}
                    };

                    return checkBox;
                }, true);
            });
        }
        
        internal sealed class Option : INotifyPropertyChanged
        {
            private readonly WeakReference<FlagComboBox> completionBoxReference;
            private IDisposable? disposable;

            public Option(FlagComboBox combo, long optionValue, string text, string? description)
            {
                Text = text;
                OptionValue = optionValue;
                Description = description;
                completionBoxReference = new WeakReference<FlagComboBox>(combo);

                TextWithNumber = $"{Text} {OptionValue}";
                subscribed = 0;
            }

            public string TextWithNumber { get; }
            
            public string Text { get; }
            
            public long OptionValue { get; }
            
            public string? Description { get; }

            public bool HasDescription => Description != null;
            
            public bool IsChecked
            {
                get
                {
                    if (completionBoxReference.TryGetTarget(out var completionBox))
                    {
                        if (OptionValue == 0)
                            return completionBox.SelectedValue == 0;
                        
                        return (completionBox.SelectedValue & OptionValue) == OptionValue;
                    }

                    return false;
                }
                set
                {
                    if (completionBoxReference.TryGetTarget(out var completionBox))
                    {
                        var current = completionBox.SelectedValue;
                        if (value)
                        {
                            if (OptionValue == 0)
                                completionBox.SelectedValue = 0;
                            else
                                completionBox.SelectedValue = current | OptionValue;
                        }
                        else
                        {
                            completionBox.SelectedValue = current & ~OptionValue;
                        }
                    }
                }
            }

            private event PropertyChangedEventHandler? PropertyChanged;

            private int subscribed;
            
            event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
            {
                add
                {
                    if (!completionBoxReference.TryGetTarget(out var completionBox))
                        return;
                    
                    subscribed++;
                    PropertyChanged += value;
                    if (subscribed > 0)
                    {
                        if (disposable != null)
                            throw new Exception();
                        disposable = completionBox.GetObservable(SelectedValueProperty).Skip(1).SubscribeAction(_ =>
                        {
                            OnPropertyChanged(nameof(IsChecked));
                        });
                    }
                }
                remove
                {
                    if (!completionBoxReference.TryGetTarget(out _))
                        return;
                    
                    PropertyChanged -= value;
                    subscribed--;
                    if (subscribed == 0)
                    {
                        disposable?.Dispose();
                        disposable = null;
                    }
                }
            }

            private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    public abstract class BasePhantomFlagsComboBox : PhantomControlBase<FlagComboBox>
    {
        protected void Spawn(Visual parent, Rect position, string? initialText, Dictionary<long, SelectOption>? flags, long value)
        {
            var flagsComboBox = new FlagComboBox();
            flagsComboBox.Flags = flags;
            flagsComboBox.SelectedValue = value;
            flagsComboBox.HideButton = true;
            flagsComboBox.IsLightDismissEnabled = false; // we are handling it ourselves, without doing .Handled = true so that as soon as user press outside of popup, the click is treated as actual click
            flagsComboBox.Closed += CompletionComboBoxOnClosed;

            if (!AttachAsAdorner(parent, position, flagsComboBox, true))
                return;

            DispatcherTimer.RunOnce(() =>
            {
                flagsComboBox.IsDropDownOpen = true;
                flagsComboBox.SearchText = initialText ?? "";
            }, TimeSpan.FromMilliseconds(1));
        }

        private void CompletionComboBoxOnClosed()
        {
            Despawn(true);
        }

        protected override void Cleanup(FlagComboBox element)
        {
            element.Closed -= CompletionComboBoxOnClosed;
        }
    }
}