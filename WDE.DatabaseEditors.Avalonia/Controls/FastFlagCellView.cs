using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaStyles.Controls;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Parameters;

namespace WDE.DatabaseEditors.Avalonia.Controls
{
    public class FastFlagCellView : OpenableFastCellViewBase
    {
        public static readonly DirectProperty<FastFlagCellView, object?> SelectedItemProperty =
            AvaloniaProperty.RegisterDirect<FastFlagCellView, object?>(
                nameof(SelectedItem),
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v,
                defaultBindingMode: BindingMode.TwoWay);
        
        private long selectedValue;
        public static readonly DirectProperty<FastFlagCellView, long> SelectedValueProperty = AvaloniaProperty.RegisterDirect<FastFlagCellView, long>("SelectedValue", o => o.SelectedValue, (o, v) => o.SelectedValue = v, 0, BindingMode.TwoWay);
        
        private Dictionary<long, SelectOption>? flags;
        public static readonly DirectProperty<FastFlagCellView, Dictionary<long, SelectOption>?> FlagsProperty = AvaloniaProperty.RegisterDirect<FastFlagCellView, Dictionary<long, SelectOption>?>("Flags", o => o.Flags, (o, v) => o.Flags = v);
        
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
        
        private object? selectedItem;
        public object? SelectedItem
        {
            get => selectedItem;
            set => SetAndRaise(SelectedItemProperty, ref selectedItem, value);
        }

        protected override bool DismissOnWindowFocusLost => true;

        protected override Type StyleKeyOverride => typeof(FastCellView);
        
        private FlagComboBox? flagsComboBox;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            partPanel = e.NameScope.Find<Panel>("PART_Panel");
            partText = e.NameScope.Find<TextBlock>("PART_text");
        }
        
        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            if (!OpenForEditing())
                return;
            e.Handled = true;
            if (e.Text == "\n" || e.Text == " " || e.Text == "\r")
                return;
            DispatcherTimer.RunOnce(() =>
            {
                var args = new TextInputEventArgs();
                args.Handled = false;
                args.Text = e.Text;
                args.Route = e.Route;
                args.RoutedEvent = e.RoutedEvent;
                args.Source = flagsComboBox;
                flagsComboBox!.RaiseEvent(args);
            }, TimeSpan.FromMilliseconds(2));
        }

        private void CompletionComboBoxOnClosed()
        {
            EndEditing(true);
            Focus(NavigationMethod.Tab);
        }

        protected override Control CreateEditingControl()
        {
 
            flagsComboBox = new FlagComboBox();
            flagsComboBox.Flags = flags;
            flagsComboBox.SelectedItem = selectedItem;
            flagsComboBox.SelectedValue = selectedValue;
            flagsComboBox.HideButton = true;
            flagsComboBox.IsLightDismissEnabled = false; // we are handling it ourselves, without doing .Handled = true so that as soon as user press outside of popup, the click is treated as actual click
            flagsComboBox.Margin = new Thickness(partText?.Margin.Left ?? 0,partText?.Margin.Top ?? 0, 0, 0);
            flagsComboBox.Padding = new Thickness(partText?.Padding.Left ?? 0,partText?.Padding.Top ?? 0, 0, 0);
            flagsComboBox.Closed += CompletionComboBoxOnClosed;
            DispatcherTimer.RunOnce(() =>
            {
                if (flagsComboBox != null)
                    flagsComboBox.IsDropDownOpen = true;
            }, TimeSpan.FromMilliseconds(1));
            return flagsComboBox;
        }

        protected override void EndEditingInternal(bool commit)
        {
            if (flagsComboBox == null)
                return;
            
            if (commit)
                SelectedValue = flagsComboBox.SelectedValue;
            flagsComboBox.Closed -= CompletionComboBoxOnClosed;
            flagsComboBox = null;
        }
        
        protected override void PasteImpl(string text)
        {
            if (long.TryParse(text, out var l))
                SelectedValue = l;
        }

        public override void DoCopy()
        {
            TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(SelectedValue.ToString()!);
        }
    }
}
